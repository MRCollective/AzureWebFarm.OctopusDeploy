using System;
using System.Diagnostics;
using System.IO;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Octopus.Client;
using Serilog;

namespace WebFarm
{
    public class WebRole : RoleEntryPoint
    {
        private readonly ILogger _log;
        private readonly IOctopusRepository _repository;
        private readonly string _octopusServer;
        private readonly string _octopusApiKey;
        private readonly string _tentacleEnvironment;
        private readonly string _tentacleRole;
        private readonly string _name;

        public WebRole()
        {
            _log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString")))
                .CreateLogger();
            // todo: enrich with the instance name

            _octopusServer = RoleEnvironment.GetConfigurationSettingValue("OctopusServer");
            _octopusApiKey = RoleEnvironment.GetConfigurationSettingValue("OctopusApiKey");
            _tentacleEnvironment = RoleEnvironment.GetConfigurationSettingValue("TentacleEnvironment");
            _tentacleRole = RoleEnvironment.GetConfigurationSettingValue("TentacleRole");

            _name = RoleEnvironment.IsEmulated
                ? Environment.MachineName
                : string.Format("{0}_{1}", RoleEnvironment.CurrentRoleInstance.Id, _tentacleEnvironment);

            // todo: request recycle for the above configuration settings being changed

            var endpoint = new OctopusServerEndpoint(_octopusServer, _octopusApiKey);
            _repository = new OctopusRepository(endpoint);
        }

        public override bool OnStart()
        {
            const string instanceArg = "--instance \"Tentacle\"";
            var octopusDeploymentsPath = RoleEnvironment.GetLocalResource("Deployments").RootPath;
            var installPath = RoleEnvironment.GetLocalResource("Install").RootPath;
            var tentacleDir = Path.Combine(installPath, "Tentacle");
            var tentaclePath = Path.Combine(Path.Combine(tentacleDir, "Agent"), "Tentacle.exe");

            Run(tentaclePath, string.Format("create-instance {0} --config \"{1}\" --console", instanceArg, Path.Combine(installPath, "Tentacle.config")));
            Run(tentaclePath, string.Format("configure {0} --home \"{1}\" --console", instanceArg, octopusDeploymentsPath.Substring(0, octopusDeploymentsPath.Length - 1)));
            Run(tentaclePath, string.Format("configure {0} --app \"{1}\" --console", instanceArg, Path.Combine(octopusDeploymentsPath, "Applications")));
            Run(tentaclePath, string.Format("register-with {0} --server \"{1}\" --environment \"{2}\" --role \"{3}\" --apiKey \"{4}\" --name \"{5}\" --comms-style TentacleActive --force --console", instanceArg, _octopusServer, _tentacleEnvironment, _tentacleRole, _octopusApiKey, _name));
            Run(tentaclePath, string.Format("service {0} --install --start --console", instanceArg));

            // todo: Check machine is online and redeploy sites to it

            return true;
        }

        public override void OnStop()
        {
            // todo: shutdown gracefully if there are pending web requests
            // todo: de-register machine from Octopus
        }

        private void Run(string executable, string arguments)
        {
            _log.Debug("Running {0} with {1}", executable, arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            };

            try
            {
                var process = Process.Start(startInfo);
                var stderr = process.StandardError.ReadToEnd();
                var stdout = process.StandardOutput.ReadToEnd();

                if (process.ExitCode != 0)
                    throw new Exception(string.Format("Non-zero exit code returned ({0}). Stdout: {1} StdErr: {2}", process.ExitCode, stdout, stderr));

                _log.Information("Executed {executable} with {arguments}. {stdout}. {stderr}.", executable, arguments, stdout, stderr);
            }
            catch (Exception e)
            {
                _log.Error(e, "Failed to execute {executable} with {arguments}", executable, arguments);
                throw;
            }
        }
    }
}
