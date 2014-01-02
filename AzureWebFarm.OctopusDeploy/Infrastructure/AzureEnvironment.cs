using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;
using Serilog;

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

        public static void WaitForAllHttpRequestsToEnd()
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
    }
}
