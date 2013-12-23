using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Serilog;

namespace WebRole1
{
    public class WebRole : RoleEntryPoint
    {
        /*private readonly ILogger _log;

        public WebRole()
        {
            _log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString")))
                .CreateLogger();
        }

        public override bool OnStart()
        {
            var octopusServer = RoleEnvironment.GetConfigurationSettingValue("OctopusServer");
            var octopusApiKey = RoleEnvironment.GetConfigurationSettingValue("OctopusApiKey");
            var tentacleEnvironment = RoleEnvironment.GetConfigurationSettingValue("TentacleEnvironment");
            var tentacleRole = RoleEnvironment.GetConfigurationSettingValue("TentacleRole");

            var installPath = RoleEnvironment.GetLocalResource("Install").RootPath;
            var octopusDeploymentsPath = RoleEnvironment.GetLocalResource("Deployments").RootPath;

            var tentacleInstallerPath = Path.Combine(installPath, "Octopus.Tentacle.msi");
            var tentacleDir = Path.Combine(installPath, "Tentacle");
            var tentaclePath = Path.Combine(Path.Combine(tentacleDir, "Agent"), "Tentacle.exe");
            const string instanceArg = "--instance \"Tentacle\"";

            if (!File.Exists(tentacleInstallerPath))
            {
                const string tentacleDownloadPath = "http://download.octopusdeploy.com/octopus/Octopus.Tentacle.2.0.6.950.msi";
                _log.Debug("Downloading {0}", tentacleDownloadPath);
                new WebClient().DownloadFile(tentacleDownloadPath, tentacleInstallerPath);
                _log.Information("Downloaded {0}", File.Exists(tentacleInstallerPath));
            }
            else
            {
                _log.Debug("Already found tentacle installer at: {0}", tentacleInstallerPath);
            }

            Run("msiexec.exe", string.Format("INSTALLLOCATION=\"{0}\" /i \"{1}\" /quiet", tentacleDir, tentacleInstallerPath));
            Run(tentaclePath, string.Format("create-instance {0} --config \"{1}\"", instanceArg, Path.Combine(installPath, "Tentacle.config")));
            Run(tentaclePath, string.Format("configure {0} --home \"{1}\" --console", instanceArg, octopusDeploymentsPath.Substring(0, octopusDeploymentsPath.Length - 1)));
            Run(tentaclePath, string.Format("configure {0} --app \"{1}\" --console", instanceArg, Path.Combine(octopusDeploymentsPath, "Applications")));
            Run(tentaclePath, string.Format("register-with {0} --server \"{1}\" --environment \"{2}\" --role \"{3}\" --apiKey \"{4}\" --comms-style TentacleActive --force --console", instanceArg, octopusServer, tentacleEnvironment, tentacleRole, octopusApiKey));
            Run(tentaclePath, string.Format("service {0} --install --start --console", instanceArg));

            return true;
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
        }*/
    }
}
