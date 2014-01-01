using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Web.Administration;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Platform.Model;
using Serilog;
using Serilog.Core;
using Serilog.Events;

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

        private static readonly string[] RoleConfigurations = new[] {"OctopusServer", "OctopusApiKey", "TentacleEnvironment", "TentacleRole"};

        public WebRole()
        {
            _log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString")))
                .Enrich.With<RoleEnvironmentEnricher>()
                .CreateLogger();

            _octopusServer = RoleEnvironment.GetConfigurationSettingValue("OctopusServer");
            _octopusApiKey = RoleEnvironment.GetConfigurationSettingValue("OctopusApiKey");
            _tentacleEnvironment = RoleEnvironment.GetConfigurationSettingValue("TentacleEnvironment");
            _tentacleRole = RoleEnvironment.GetConfigurationSettingValue("TentacleRole");

            _name = RoleEnvironment.IsEmulated
                ? Environment.MachineName
                : string.Format("{0}_{1}", RoleEnvironment.CurrentRoleInstance.Id, _tentacleEnvironment);

            RoleEnvironment.Changing += RoleEnvironmentChanging;

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

            DeployAllCurrentReleasesToThisRole();

            return true;
        }

        public override void Run()
        {
            if (RoleEnvironment.IsEmulated)
                base.Run();

            while (true)
            {
                try
                {
                    // https://github.com/sandrinodimattia/WindowsAzure-IISApplicationInitializationModule
                    using (var serverManager = new ServerManager())
                    {
                        foreach (var application in serverManager.Sites.SelectMany(site => site.Applications))
                            application["preloadEnabled"] = true;

                        foreach (var appPool in serverManager.ApplicationPools)
                            appPool["startMode"] = "AlwaysRunning";

                        serverManager.CommitChanges();
                    }
                }
                catch (Exception e)
                {
                    _log.Warning(e, "Failure to configure IIS");
                }

                Thread.Sleep(TimeSpan.FromMinutes(10));
            }
        }

        public override void OnStop()
        {
            WaitForAllHttpRequestsToEnd();

            var machine = _repository.Machines.FindByName(_name);
            _repository.Machines.Delete(machine);
        }

        private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            // If there is a setting change for something that is used to register with Octopus
            //  then we need to recycle the role so it re-registers with the Octopus Server
            var recycle = e.Changes
                .OfType<RoleEnvironmentConfigurationSettingChange>()
                .Any(c => RoleConfigurations.Contains(c.ConfigurationSettingName));
            if (recycle)
                RoleEnvironment.RequestRecycle();
        }

        private void WaitForAllHttpRequestsToEnd()
        {
            // http://blogs.msdn.com/b/windowsazure/archive/2013/01/14/the-right-way-to-handle-azure-onstop-events.aspx
            var pcrc = new PerformanceCounter("ASP.NET", "Requests Current", "");
            while (true)
            {
                var rc = pcrc.NextValue();
                _log.Information("ASP.NET Requests Current = {0}, {1}.", rc, rc <= 0 ? "permitting role exit" : "blocking role exit");
                if (rc <= 0)
                    break;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
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

        private void DeployAllCurrentReleasesToThisRole()
        {
            var environment = _repository.Environments.FindByName(_tentacleEnvironment).Id;

            var dashboard = _repository.Dashboards.GetDashboard();
            var releaseTasks = dashboard.Items
                .Where(i => i.EnvironmentId == environment)
                .ToList()
                .Select(currentRelease => _repository.Deployments.Create(
                    new DeploymentResource
                    {
                        Comments = "Automated deployment by " + RoleEnvironment.CurrentRoleInstance.Id,
                        EnvironmentId = currentRelease.EnvironmentId,
                        ReleaseId = currentRelease.ReleaseId
                    }
                ))
                .Select(d => d.TaskId)
                .ToList();
            _log.Information("Triggered deployments with task ids: {taskIds}", releaseTasks);

            var taskStatuses = releaseTasks.Select(taskId => _repository.Tasks.Get(taskId).State);

            var currentStatuses = taskStatuses.ToList();
            while (currentStatuses.Any(s => s == TaskState.Executing || s == TaskState.Queued))
            {
                _log.Debug("Waiting for deployments; current statuses: {statuses}", currentStatuses);
                Thread.Sleep(1000);
                currentStatuses = taskStatuses.ToList();
            }

            _log.Information("Deployments completed with statuses: {statuses}", currentStatuses);

            if (currentStatuses.Any(s => s == TaskState.Failed || s == TaskState.TimedOut))
                throw new Exception("Failed to deploy to this role - at least one necessary deployment either failed or timed out - recycling role to try again");
        }
    }

    class RoleEnvironmentEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("InstanceId", RoleEnvironment.CurrentRoleInstance.Id));
        }
    }
}
