using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Octopus.Client;
using Octopus.Client.Exceptions;
using Octopus.Client.Model;
using Serilog;

namespace AzureWebFarm.OctopusDeploy.Infrastructure
{
    internal class OctopusDeploy
    {
        private const string InstanceArg = "--instance \"Tentacle\"";
        private readonly string _machineName;
        private readonly IConfigSettings _config;
        private readonly IProcessRunner _processRunner;
        private readonly IRegistryEditor _registryEditor;
        private readonly IOctopusRepository _repository;
        private readonly string _tentaclePath;
        private readonly string _tentacleInstallPath;

        public OctopusDeploy(string machineName, IConfigSettings config, IOctopusRepository repository, IProcessRunner processRunner, IRegistryEditor registryEditor)
        {
            _machineName = machineName;
            _config = config;
            _processRunner = processRunner;
            _registryEditor = registryEditor;
            _repository = repository;
            _tentacleInstallPath = _config.TentacleInstallPath;
            _tentaclePath = Path.Combine(_tentacleInstallPath, "Tentacle", "Tentacle.exe");
        }

        public void ConfigureTentacle()
        {
            var tentacleDeploymentsPath = _config.TentacleDeploymentsPath;

            HandleFailedRemoval();

            _processRunner.Run(_tentaclePath, string.Format("create-instance {0} --config \"{1}\" --console", InstanceArg, Path.Combine(_tentacleInstallPath, "Tentacle.config")));
            _processRunner.Run(_tentaclePath, string.Format("new-certificate {0} --console", InstanceArg));
            _processRunner.Run(_tentaclePath, string.Format("configure {0} --home \"{1}\" --console", InstanceArg, tentacleDeploymentsPath.Substring(0, tentacleDeploymentsPath.Length - 1)));
            _processRunner.Run(_tentaclePath, string.Format("configure {0} --app \"{1}\" --console", InstanceArg, Path.Combine(tentacleDeploymentsPath, "Applications")));
            _processRunner.Run(_tentaclePath, string.Format("register-with {0} --server \"{1}\" --environment \"{2}\" --role \"{3}\" --apiKey \"{4}\" --name \"{5}\" --comms-style TentacleActive --force --console", InstanceArg, _config.OctopusServer, _config.TentacleEnvironment, _config.TentacleRole, _config.OctopusApiKey, _machineName));
            _processRunner.Run(_tentaclePath, string.Format("service {0} --install --start --console", InstanceArg));
        }

        private void HandleFailedRemoval()
        {
            if (File.Exists(Path.Combine(_tentacleInstallPath, "Tentacle.config")))
            {
                try
                {
                    Log.Information(
                        "Detected existing tentacle.config - possible abrupt restart. Deleting tentacle.config.");
                    File.Delete(Path.Combine(_tentacleInstallPath, "Tentacle.config"));
                    Log.Information("Successfully removed tentacle.config from role.");
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to remove certificate from Octopus server. Manual intevention required.");
                }
            }

            if (_repository.Machines.FindByName(_machineName) != null)
            {
                try
                {
                    Log.Information("Detected existing machine using this name. Attempting to remove tentacle from Octopus server.");
                    DeleteMachine();
                    Log.Information("Successfully removed tentacle from Octopus server.");
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to remove tentacle from Octopus server. Manual intevention required.");
                }
            }
        }

        public void DeleteMachine()
        {
            var machine = _repository.Machines.FindByName(_machineName);
            _repository.Machines.Delete(machine);
        }

        public void DeployAllCurrentReleasesToThisMachine()
        {
            const string targetRolePropertyName = "Octopus.Action.TargetRoles";
            List<TaskState> currentStatuses;
            try
            {
                var machineId = _repository.Machines.FindByName(_machineName).Id;
                var environment = _repository.Environments.FindByName(_config.TentacleEnvironment).Id;

                var projects = _repository.Projects.FindAll()
                    .Where(p => _repository.DeploymentProcesses
                               .Get(p.DeploymentProcessId)
                               .Steps
                               .Any(s =>
                               {
                                   PropertyValueResource value;
                                   return s.Properties.TryGetValue(targetRolePropertyName, out value) 
                                       && value != null 
                                       && value.Value.Split(',').Any(x => x.ToLowerInvariant() ==  _config.TentacleRole.ToLowerInvariant());
                               }))
                    .Select(p => p.Id);

                var dashboard = _repository.Dashboards.GetDashboard();
                var dashboardItems = dashboard.Items
                    .Where(i => i.EnvironmentId == environment)
                    .Where(i => projects.Contains(i.ProjectId))
                    .ToList();

                var releaseTasks = new List<string>();
                foreach (var currentRelease in dashboardItems)
                {
                    try
                    {
                        var deployment = _repository.Deployments.Create(new DeploymentResource
                        {
                            Comments = "Automated startup deployment by " + _machineName,
                            EnvironmentId = currentRelease.EnvironmentId,
                            ReleaseId = currentRelease.ReleaseId,
                            SpecificMachineIds = new ReferenceCollection(new[] { machineId })
                        });
                        releaseTasks.Add(deployment.TaskId);
                    }
                    catch (OctopusValidationException ex)
                    {
                        Log.Error("Attempted to create a deployment that the OctopusDeploy server didn't like.", ex);
                        // Rethowing the exception here so Azure doesn't fail trying to serialize it
                        throw new Exception("Attempted to create a deployment that the OctopusDeploy server didn't like.", ex);
                    }
                }

                Log.Information("Triggered deployments with task ids: {taskIds}", releaseTasks);

                var taskStatuses = releaseTasks.Select(taskId => _repository.Tasks.Get(taskId).State);

                // ReSharper disable once PossibleMultipleEnumeration
                currentStatuses = taskStatuses.ToList();
                while (currentStatuses.Any(s => s == TaskState.Executing || s == TaskState.Queued))
                {
                    Log.Debug("Waiting for deployments; current statuses: {statuses}", currentStatuses);
                    Thread.Sleep(1000);
                    // ReSharper disable once PossibleMultipleEnumeration
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

        public static IOctopusRepository GetRepository(IConfigSettings config)
        {
            return new OctopusRepository(new OctopusServerEndpoint(config.OctopusServer, config.OctopusApiKey));
        }

        public void UninstallTentacle()
        {
            _processRunner.Run(_tentaclePath, string.Format("service {0} --stop --uninstall --console", InstanceArg));
            _processRunner.Run(_tentaclePath, string.Format("delete-instance {0} --console", InstanceArg));
            _processRunner.Run("msiexec", string.Format("/uninstall \"{0}{1}\" /quiet", _tentacleInstallPath, "Octopus.Tentacle.msi"));
            _registryEditor.DeleteLocalMachineTree("Software", "Octopus");
            _processRunner.Run("cmd.exe", string.Format("/c del \"{0}{1}\"", _tentacleInstallPath, "Tentacle.config"));
        }
    }
}
