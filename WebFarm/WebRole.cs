using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Octopus.Client;
using Serilog;

namespace WebFarm
{
    public class WebRole : RoleEntryPoint
    {
        private readonly ILogger _log;
        private readonly IOctopusRepository _repository;
        private readonly string _octopusServer;

        public WebRole()
        {
            _log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString")))
                .CreateLogger();

            _octopusServer = RoleEnvironment.GetConfigurationSettingValue("OctopusServer");
            var octopusApiKey = RoleEnvironment.GetConfigurationSettingValue("OctopusApiKey");
            var endpoint = new OctopusServerEndpoint(_octopusServer, octopusApiKey);
            _repository = new OctopusRepository(endpoint);
        }

        public override bool OnStart()
        {
            // todo: Check machine is online and redeploy sites to it

            return true;
        }

    }
}
