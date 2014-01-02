using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using AzureWebFarm.OctopusDeploy.Infrastructure;
using Microsoft.Web.Administration;
using Microsoft.WindowsAzure.ServiceRuntime;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Platform.Model;
using Serilog;

namespace AzureWebFarm.OctopusDeploy
{
    public class WebRole
    {
        private readonly IOctopusRepository _repository;
        private readonly string _name;
        private readonly ConfigSettings _config;

        public WebRole()
        {
            Log.Logger = Logging.GetAzureLogger();
            _config = new ConfigSettings(RoleEnvironment.GetConfigurationSettingValue);
            _name = AzureEnvironment.GetMachineName(_config);
            _repository = new OctopusRepository(new OctopusServerEndpoint(_config.OctopusServer, _config.OctopusApiKey));

            AzureEnvironment.RequestRecycleIfConfigSettingChanged(_config);
        }

        public bool OnStart()
        {
            const string instanceArg = "--instance \"Tentacle\"";
            var octopusDeploymentsPath = RoleEnvironment.GetLocalResource("Deployments").RootPath;
            var installPath = RoleEnvironment.GetLocalResource("Install").RootPath;
            var tentacleDir = Path.Combine(installPath, "Tentacle");
            var tentaclePath = Path.Combine(Path.Combine(tentacleDir, "Agent"), "Tentacle.exe");

            ProcessRunner.Run(tentaclePath, string.Format("create-instance {0} --config \"{1}\" --console", instanceArg, Path.Combine(installPath, "Tentacle.config")));
            ProcessRunner.Run(tentaclePath, string.Format("new-certificate --console"));
            ProcessRunner.Run(tentaclePath, string.Format("configure {0} --home \"{1}\" --console", instanceArg, octopusDeploymentsPath.Substring(0, octopusDeploymentsPath.Length - 1)));
            ProcessRunner.Run(tentaclePath, string.Format("configure {0} --app \"{1}\" --console", instanceArg, Path.Combine(octopusDeploymentsPath, "Applications")));
            ProcessRunner.Run(tentaclePath, string.Format("register-with {0} --server \"{1}\" --environment \"{2}\" --role \"{3}\" --apiKey \"{4}\" --name \"{5}\" --comms-style TentacleActive --force --console", instanceArg, _config.OctopusServer, _config.TentacleEnvironment, _config.TentacleRole, _config.OctopusApiKey, _name));
            ProcessRunner.Run(tentaclePath, string.Format("service {0} --install --start --console", instanceArg));

            DeployAllCurrentReleasesToThisRole();

            return true;
        }

        public void Run()
        {
            if (RoleEnvironment.IsEmulated)
                Thread.Sleep(-1);

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
                    Log.Warning(e, "Failure to configure IIS");
                }

                Thread.Sleep(TimeSpan.FromMinutes(10));
            }
        // ReSharper disable FunctionNeverReturns
        }
        // ReSharper restore FunctionNeverReturns

        public void OnStop()
        {
            WaitForAllHttpRequestsToEnd();

            var machine = _repository.Machines.FindByName(_name);
            _repository.Machines.Delete(machine);
        }

        private void WaitForAllHttpRequestsToEnd()
        {
            // http://blogs.msdn.com/b/windowsazure/archive/2013/01/14/the-right-way-to-handle-azure-onstop-events.aspx
            var pcrc = new PerformanceCounter("ASP.NET", "Requests Current", "");
            while (true)
            {
                var rc = pcrc.NextValue();
                Log.Information("ASP.NET Requests Current = {0}, {1}.", rc, rc <= 0 ? "permitting role exit" : "blocking role exit");
                if (rc <= 0)
                    break;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private void DeployAllCurrentReleasesToThisRole()
        {
            var machineId = _repository.Machines.FindByName(_name).Id;
            var environment = _repository.Environments.FindByName(_config.TentacleEnvironment).Id;

            var dashboard = _repository.Dashboards.GetDashboard();
            var releaseTasks = dashboard.Items
                .Where(i => i.EnvironmentId == environment)
                .ToList()
                .Select(currentRelease => _repository.Deployments.Create(
                    new DeploymentResource
                    {
                        Comments = "Automated startup deployment by " + RoleEnvironment.CurrentRoleInstance.Id,
                        EnvironmentId = currentRelease.EnvironmentId,
                        ReleaseId = currentRelease.ReleaseId,
                        SpecificMachineIds = new ReferenceCollection(new[]{machineId})
                    }
                ))
                .Select(d => d.TaskId)
                .ToList();
            Log.Information("Triggered deployments with task ids: {taskIds}", releaseTasks);

            var taskStatuses = releaseTasks.Select(taskId => _repository.Tasks.Get(taskId).State);

            var currentStatuses = taskStatuses.ToList();
            while (currentStatuses.Any(s => s == TaskState.Executing || s == TaskState.Queued))
            {
                Log.Debug("Waiting for deployments; current statuses: {statuses}", currentStatuses);
                Thread.Sleep(1000);
                currentStatuses = taskStatuses.ToList();
            }

            Log.Information("Deployments completed with statuses: {statuses}", currentStatuses);

            if (currentStatuses.Any(s => s == TaskState.Failed || s == TaskState.TimedOut))
                throw new Exception("Failed to deploy to this role - at least one necessary deployment either failed or timed out - recycling role to try again");
        }
    }

    
}
