using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using ApprovalTests;
using ApprovalTests.Reporters;
using AutofacContrib.NSubstitute;
using AzureWebFarm.OctopusDeploy.Infrastructure;
using NSubstitute;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Platform.Model;
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

        public OctopusDeployTests()
        {
            _container = new AutoSubstitute();
            _config = new ConfigSettings(s => string.Format("%{0}%", s), s => string.Format("c:\\{0}", s));
            _container.Provide(_config);
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
            DeploymentResource createdDeployment = null;
            _container.Resolve<IOctopusRepository>().Deployments
                .WhenForAnyArgs(d => d.Create(null))
                .Do(a => createdDeployment = a.Arg<DeploymentResource>());

            _sut.DeployAllCurrentReleasesToThisMachine();

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
            var dashboard = new DashboardResource {Items = new List<DashboardItemResource>()};
            dashboard.Items.Add(new DashboardItemResource {EnvironmentId = "ignore"});
            dashboard.Items.Add(new DashboardItemResource {EnvironmentId = EnvironmentId, ReleaseId = ReleaseId});
            repo.Dashboards.GetDashboard().Returns(dashboard);
            repo.Deployments.Create(null).ReturnsForAnyArgs(new DeploymentResource { TaskId = TaskId });
            repo.Tasks.Get(TaskId).Returns(new TaskResource { State = TaskState.Success });
        }
    }
}
