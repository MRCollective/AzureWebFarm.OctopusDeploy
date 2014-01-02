using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AzureWebFarm.OctopusDeploy.Infrastructure
{
    internal static class Logging
    {
        private const string DiagnosticsConfigSetting = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";

        public static ILogger GetAzureLogger()
        {
            return new LoggerConfiguration()
                .WriteTo.AzureTableStorage(CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(DiagnosticsConfigSetting)))
                .Enrich.With<RoleEnvironmentEnricher>()
                .CreateLogger();
        }

        class RoleEnvironmentEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("InstanceId", RoleEnvironment.CurrentRoleInstance.Id));
            }
        }
    }
}
