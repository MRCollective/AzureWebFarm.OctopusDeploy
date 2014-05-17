using System;
using AzureWebFarm.OctopusDeploy.Infrastructure;
using Shouldly;
using Xunit;

namespace AzureWebFarm.OctopusDeploy.Tests.Infrastructure
{
    public class ConfigSettingsTests
    {
        [Fact]
        public void GivenExceptionWhenGettingConfigSetting_WhenConstructingConfigSettings_ThenThrowNiceException()
        {
            var e = new Exception("Error");
            Func<string, string> getter = _ => { throw e; };

            var ex = Assert.Throws<UnableToGetConfigSettingException>(() => new ConfigSettings(getter, getter));

            ex.Message.ShouldBe("Unable to get config setting: OctopusServer");
            ex.InnerException.ShouldBe(e);
        }
    }
}
