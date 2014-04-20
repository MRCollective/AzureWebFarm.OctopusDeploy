using AzureWebFarm.OctopusDeploy.Infrastructure;
using Microsoft.Win32;
using Shouldly;
using Xunit;

namespace AzureWebFarm.OctopusDeploy.Tests.Infrastructure
{
    public class RegistryEditorTests
    {
        private RegistryEditor _sut = new RegistryEditor();

        [Fact]
        public void GivenTreeExistsInRegistry_WhenDeletingThatTree_ThenItsNoLongerThere()
        {
            Registry.LocalMachine.OpenSubKey("Software", true).CreateSubKey("AzureWebFarm.OctopusDeploy").CreateSubKey("Test").SetValue("test", "Hello World!");

            _sut.DeleteLocalMachineTree("Software", "AzureWebFarm.OctopusDeploy");

            Registry.LocalMachine.OpenSubKey("Software").GetSubKeyNames().ShouldNotContain("AzureWebFarm.OctopusDeploy");
        }

        [Fact]
        public void GivenTreeDoesntExistInRegistry_WhenDeletingThatTree_ThenDontThrowAnException()
        {
            Registry.LocalMachine.OpenSubKey("Software").GetSubKeyNames().ShouldNotContain("AzureWebFarm.OctopusDeploy");

            _sut.DeleteLocalMachineTree("Software", "AzureWebFarm.OctopusDeploy");
        }
    }
}
