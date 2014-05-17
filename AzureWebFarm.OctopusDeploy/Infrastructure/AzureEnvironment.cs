using System;
using System.Linq;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Serilog;
using Serilog.Core;
using Serilog.Events;

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
                    .Any(c => config.IsConfigSettingName(c.ConfigurationSettingName));

                if (shouldRecycle)
                    RoleEnvironment.RequestRecycle();
            };
        }

        public static string GetMachineName(IConfigSettings config)
        {
            var name = AzureRoleEnvironment.IsEmulated()
                ? Environment.MachineName
                : string.Format("{0}_{1}", AzureRoleEnvironment.CurrentRoleInstanceId(), config.TentacleEnvironment);

            if (!string.IsNullOrEmpty(config.TentacleMachineNameSuffix))
                name = string.Format("{0}_{1}", name, config.TentacleMachineNameSuffix);

            return name;
        }

        public static ConfigSettings GetConfigSettings()
        {
            return new ConfigSettings(
                RoleEnvironment.GetConfigurationSettingValue,
                name => RoleEnvironment.GetLocalResource(name).RootPath
            );
        }

        private const string DiagnosticsConfigSetting = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";

        public static ILogger GetAzureLogger()
        {
            return new LoggerConfiguration()
                .WriteTo.AzureTableStorage(CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(DiagnosticsConfigSetting)))
                .Enrich.With<RoleEnvironmentEnricher>()
                .CreateLogger();
        }

        private class RoleEnvironmentEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("InstanceId", RoleEnvironment.CurrentRoleInstance.Id));
            }
        }
    }
}
