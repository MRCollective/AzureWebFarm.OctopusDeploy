using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Platform.Model;
using Serilog;

namespace AzureWebFarm.OctopusDeploy.Infrastructure
{
    internal class OctopusDeploy
    {
        private readonly string _machineName;
        private readonly ConfigSettings _config;
        private readonly IProcessRunner _processRunner;
        private readonly IOctopusRepository _repository;

        public OctopusDeploy(string machineName, ConfigSettings config, IOctopusRepository repository, IProcessRunner processRunner)
        {
            _machineName = machineName;
            _config = config;
            _processRunner = processRunner;
            _repository = repository;
        }

        public void ConfigureTentacle()
        {
            const string instanceArg = "--instance \"Tentacle\"";
            var octopusDeploymentsPath = RoleEnvironment.GetLocalResource("Deployments").RootPath;
            var installPath = RoleEnvironment.GetLocalResource("Install").RootPath;
            var tentacleDir = Path.Combine(installPath, "Tentacle");
            var tentaclePath = Path.Combine(Path.Combine(tentacleDir, "Agent"), "Tentacle.exe");

            _processRunner.Run(tentaclePath, string.Format("create-instance {0} --config \"{1}\" --console", instanceArg, Path.Combine(installPath, "Tentacle.config")));
            _processRunner.Run(tentaclePath, string.Format("new-certificate --console"));
            _processRunner.Run(tentaclePath, string.Format("configure {0} --home \"{1}\" --console", instanceArg, octopusDeploymentsPath.Substring(0, octopusDeploymentsPath.Length - 1)));
            _processRunner.Run(tentaclePath, string.Format("configure {0} --app \"{1}\" --console", instanceArg, Path.Combine(octopusDeploymentsPath, "Applications")));
            _processRunner.Run(tentaclePath, string.Format("register-with {0} --server \"{1}\" --environment \"{2}\" --role \"{3}\" --apiKey \"{4}\" --name \"{5}\" --comms-style TentacleActive --force --console", instanceArg, _config.OctopusServer, _config.TentacleEnvironment, _config.TentacleRole, _config.OctopusApiKey, _machineName));
            _processRunner.Run(tentaclePath, string.Format("service {0} --install --start --console", instanceArg));
        }

        public void DeleteMachine()
        {
            var machine = _repository.Machines.FindByName(_machineName);
            _repository.Machines.Delete(machine);
        }

        public void DeployAllCurrentReleasesToThisMachine()
        {
            List<TaskState> currentStatuses;
            try
            {
                var machineId = _repository.Machines.FindByName(_machineName).Id;
                var environment = _repository.Environments.FindByName(_config.TentacleEnvironment).Id;

                var dashboard = _repository.Dashboards.GetDashboard();
                var releaseTasks = dashboard.Items
                    .Where(i => i.EnvironmentId == environment)
                    .ToList()
                    .Select(currentRelease => _repository.Deployments.Create(
                        new DeploymentResource
                        {
                            Comments = "Automated startup deployment by " + _machineName,
                            EnvironmentId = currentRelease.EnvironmentId,
                            ReleaseId = currentRelease.ReleaseId,
                            SpecificMachineIds = new ReferenceCollection(new[] {machineId})
                        }
                    ))
                    .Select(d => d.TaskId)
                    .ToList();
                Log.Information("Triggered deployments with task ids: {taskIds}", releaseTasks);

                var taskStatuses = releaseTasks.Select(taskId => _repository.Tasks.Get(taskId).State);

                currentStatuses = taskStatuses.ToList();
                while (currentStatuses.Any(s => s == TaskState.Executing || s == TaskState.Queued))
                {
                    Log.Debug("Waiting for deployments; current statuses: {statuses}", currentStatuses);
                    Thread.Sleep(1000);
                    currentStatuses = taskStatuses.ToList();
                }

                Log.Information("Deployments completed with statuses: {statuses}", currentStatuses);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error configuring tentacle");
                throw;
            }

            if (currentStatuses.Any(s => s == TaskState.Failed || s == TaskState.TimedOut))
                throw new Exception("Failed to deploy to this role - at least one necessary deployment either failed or timed out - recycling role to try again");
        }
    }
}
