using System;
using System.Linq;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace AzureWebFarm.OctopusDeploy.Infrastructure
{
    internal static class AzureEnvironment
    {
        public static void RequestRecycleIfConfigSettingChanged(ConfigSettings config)
        {
            RoleEnvironment.Changing += (_, e) =>
            {
                var shouldRecycle = e.Changes
                    .OfType<RoleEnvironmentConfigurationSettingChange>()
                    .Any(c => config.IsSettingName(c.ConfigurationSettingName));

                if (shouldRecycle)
                    RoleEnvironment.RequestRecycle();
            };
        }

        public static string GetMachineName(ConfigSettings config)
        {
            return RoleEnvironment.IsEmulated
                ? Environment.MachineName
                : string.Format("{0}_{1}", RoleEnvironment.CurrentRoleInstance.Id, config.TentacleEnvironment);
        }
    }
}
