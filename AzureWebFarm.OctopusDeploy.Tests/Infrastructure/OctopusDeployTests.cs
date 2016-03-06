using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ApprovalTests;
using ApprovalTests.Reporters;
using AutofacContrib.NSubstitute;
using AzureWebFarm.OctopusDeploy.Infrastructure;
using NSubstitute;
using Octopus.Client;
using Octopus.Client.Model;
using Xunit;
using Shouldly;

namespace AzureWebFarm.OctopusDeploy.Tests.Infrastructure
{
    public class OctopusDeployTests
    {
        private readonly OctopusDeploy.Infrastructure.OctopusDeploy _sut;
        private readonly AutoSubstitute _container;
        private readonly ConfigSettings _config;
        private const string MachineName = "%machineName%";
        private const string EnvironmentId = "environment-1";
        private const string ReleaseId = "release-1";
        private const string MachineId = "machine-1";
        private const string TaskId = "task-1";
        private const string ProjectId = "project-1";
        private const string ProjectId2 = "project-2";

        public OctopusDeployTests()
        {
            _container = new AutoSubstitute();
            _config = new ConfigSettings(s => string.Format("%{0}%", s), s => string.Format("c:\\{0}", s));
            _container.Provide<IConfigSettings>(_config);
            _container.Provide(MachineName);
            _sut = _container.Resolve<OctopusDeploy.Infrastructure.OctopusDeploy>();
        }

        [Fact]
        [UseReporter(typeof(DiffReporter))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WhenConfiguringTentacle_ThenTheCorrectCommandsShouldBeSentToTentacleExe()
        {
            var b = new StringBuilder();
            _container.Resolve<IProcessRunner>().WhenForAnyArgs(r => r.Run(null, null)).Do(a => b.AppendLine(string.Format("{0} {1}", a[0], a[1])));

            _sut.ConfigureTentacle();

            Approvals.Verify(b.ToString());
        }

        [Fact]
        [UseReporter(typeof(DiffReporter))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WhenUninstallingTentacle_ThenTheCorrectCommandsShouldBeSentToTentacleExe()
        {
            var b = new StringBuilder();
            _container.Resolve<IProcessRunner>().WhenForAnyArgs(r => r.Run(null, null)).Do(a => b.AppendLine(string.Format("{0} {1}", a[0], a[1])));

            _sut.UninstallTentacle();

            Approvals.Verify(b.ToString());
        }

        [Fact]
        public void WhenUninstallingTentacle_ThenDeleteTheOctopusRegistryKey()
        {
            _sut.UninstallTentacle();

            _container.Resolve<IRegistryEditor>().Received().DeleteLocalMachineTree("Software", "Octopus");
        }

        [Fact]
        public void WhenDeletingMachine_ThenDeleteTheMachineFromOctopusServer()
        {
            var thisMachine = new MachineResource();
            var machineRepo = _container.Resolve<IOctopusRepository>().Machines;
            machineRepo
                .FindByName(MachineName)
                .Returns(thisMachine);

            _sut.DeleteMachine();

            machineRepo
                .Received()
                .Delete(thisMachine);
        }

        [Fact]
        public void WhenDeployingCurrentRelease_InvokeCorrectDeployments()
        {
            ArrangeDashboardData();
            var createdDeployments = new List<DeploymentResource>();
            _container.Resolve<IOctopusRepository>().Deployments
                .WhenForAnyArgs(d => d.Create(null))
                .Do(a => createdDeployments.Add(a.Arg<DeploymentResource>()));

            _sut.DeployAllCurrentReleasesToThisMachine();

            createdDeployments.Count.ShouldBe(1);
            var createdDeployment = createdDeployments.Single();
            createdDeployment.ShouldNotBe(null);
            createdDeployment.ReleaseId.ShouldBe(ReleaseId);
            createdDeployment.EnvironmentId.ShouldBe(EnvironmentId);
            createdDeployment.SpecificMachineIds.ShouldContain(MachineId);
            createdDeployment.Comments.ShouldBe(string.Format("Automated startup deployment by {0}", MachineName));
        }

        private void ArrangeDashboardData()
        {
            var repo = _container.Resolve<IOctopusRepository>();
            repo.Machines.FindByName(MachineName).Returns(new MachineResource {Id = MachineId});
            repo.Environments.FindByName(_config.TentacleEnvironment).Returns(new EnvironmentResource {Id = EnvironmentId});
            var projects = new List<ProjectResource>
            {
                new ProjectResource{ Id = ProjectId, DeploymentProcessId = "Deploy1"},
                new ProjectResource{ Id = ProjectId2, DeploymentProcessId = "Deploy2"}
            };
            repo.Projects.FindAll().Returns(projects);
            repo.DeploymentProcesses.Get(projects[0].DeploymentProcessId).Returns(GetDeploymentProcessWithStepAgainstTarget(_config.TentacleRole));
            repo.DeploymentProcesses.Get(projects[1].DeploymentProcessId).Returns(GetDeploymentProcessWithStepAgainstTarget("random"));
            var dashboard = new DashboardResource {Items = new List<DashboardItemResource>()};
            dashboard.Items.Add(new DashboardItemResource {EnvironmentId = "ignore"});
            dashboard.Items.Add(new DashboardItemResource {EnvironmentId = EnvironmentId, ReleaseId = ReleaseId, ProjectId = ProjectId });
            dashboard.Items.Add(new DashboardItemResource {EnvironmentId = EnvironmentId, ReleaseId = ReleaseId, ProjectId = ProjectId2 });
            repo.Dashboards.GetDashboard().Returns(dashboard);
            repo.Deployments.Create(null).ReturnsForAnyArgs(new DeploymentResource { TaskId = TaskId });
            repo.Tasks.Get(TaskId).Returns(new TaskResource { State = TaskState.Success });
        }

        private DeploymentProcessResource GetDeploymentProcessWithStepAgainstTarget(string target)
        {
            var deploymentProcess = new DeploymentProcessResource();
            var step = new DeploymentStepResource();
            step.Properties["Octopus.Action.TargetRoles"] = target;
            deploymentProcess.Steps.Add(step);
            return deploymentProcess;
        }
    }
}
