using System;
using AzureWebFarm.OctopusDeploy.Infrastructure;
using Shouldly;
using Xunit;

namespace AzureWebFarm.OctopusDeploy.Tests.Infrastructure
{
    public class AzureEnvironmentTests
    {
        private readonly TestConfigSettings _config = new TestConfigSettings();

        [Fact]
        public void GivenSuffixIsEmpty_WhenGeneratingFarmName_ThenUseMachineNameAndEnvironment()
        {
            AzureRoleEnvironment.IsAvailable = () => false;
            AzureRoleEnvironment.IsEmulated = () => false;
            var machineName = Environment.MachineName;
            _config.TentacleMachineNameSuffix = "";
            _config.TentacleEnvironment = "Production";

            var name = AzureEnvironment.GetMachineName(_config);

            name.ShouldBe(machineName + "_" + _config.TentacleEnvironment);
        }

        [Fact]
        public void GivenSuffixIsNotEmpty_WhenGeneratingFarmName_ThenUseMachineNameAndEnvironmentAndSuffix()
        {
            AzureRoleEnvironment.IsAvailable = () => false;
            AzureRoleEnvironment.IsEmulated = () => false;
            var machineName = Environment.MachineName;
            _config.TentacleMachineNameSuffix = "Suffix";
            _config.TentacleEnvironment = "Production";

            var name = AzureEnvironment.GetMachineName(_config);

            name.ShouldBe(machineName + "_" + _config.TentacleEnvironment + "_" + _config.TentacleMachineNameSuffix);
        }
    }

    public class TestConfigSettings : IConfigSettings
    {
        public string OctopusServer { get; set; }
        public string OctopusApiKey { get; set; }
        public string TentacleEnvironment { get; set; }
        public string TentacleRole { get; set; }
        public string TentacleMachineNameSuffix { get; set; }
        public string TentacleDeploymentsPath { get; set; }
        public string TentacleInstallPath { get; set; }
    }
}
