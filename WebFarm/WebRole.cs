using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Xml.Linq;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Platform.Model;
using Serilog;

namespace WebFarm
{
    public class WebRole : RoleEntryPoint
    {
        private readonly ILogger _log;
        private readonly IOctopusRepository _repository;
        private string _id;
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
            var roleName = RoleEnvironment.CurrentRoleInstance.Id;
            var tentacleEnvironment = RoleEnvironment.GetConfigurationSettingValue("TentacleEnvironment");
            var tentacleRole = RoleEnvironment.GetConfigurationSettingValue("TentacleRole");
            var squid = string.Format("SQ-{0}-001BCB38", Environment.MachineName);

            const string masterKey = "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA/khnM72Ll0uXJi6VId2Q6gQAAAACAAAAAAAQZgAAAAEAACAAAAC1rLqj7rzbQiPJkCeeFteV8QUSNDNfVrXj8Lcrywr2lQAAAAAOgAAAAAIAACAAAAD7ZoP4bw7DcrrQm1SDeCCy/cTqL0T7UN3lwia9cPEifCAAAADp5uP8cCSmOvbaAAbO7PHFXf/aU99GIt3s3f5PvnWnoEAAAAAd7yNuLZ19TCyFrjMsLSsbMb4MkquT69IluEzCrinKOqnN75XZ1qf17tBRw+oaN4wry93yb3fjso9y3Tik7WAM";
            const string thumbprint = "AE520BEECE08DD674FF90C2A324B2F478EB97B1A";
            const string certificate = "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";
            
            var environments = _repository.Environments.GetAll();

            var existing = _repository.Machines.FindByName(Environment.MachineName);
            if (existing != null)
                _repository.Machines.Delete(existing);

            var machine = new MachineResource
            {
                CommunicationStyle = CommunicationStyle.TentacleActive,
                EnvironmentIds = new ReferenceCollection(new[] { environments.First(e => e.Name == tentacleEnvironment).Id }),
                Name = string.Format("{0}-{1}", roleName, Environment.MachineName),
                Roles = new ReferenceCollection(new[] { tentacleRole }),
                Thumbprint = thumbprint,
                Squid = squid
            };
            _id = _repository.Machines.Create(machine).Id;

            var serverThumbprint = _repository.Certificates.GetOctopusCertificate().Thumbprint;
            var serverSquid = _repository.ServerStatus.GetServerStatus().Squid;
            var serverAddress = new Uri(_octopusServer);
            var serverApiAddress = new Uri("https://" + serverAddress.Host + ":10943");

            var installPath = RoleEnvironment.GetLocalResource("Install").RootPath;
            var configPath = Path.Combine(installPath, "Tentacle.config");

            var doc = XDocument.Load(configPath);
            doc.Root
                .AddSetting("Octopus.Storage.MasterKey", masterKey)
                .AddSetting("Tentacle.CertificateThumbprint", thumbprint)
                .AddSetting("Tentacle.Certificate", certificate)
                .AddSetting("Octopus.Communications.Squid", squid)
                .AddSetting("Tentacle.Communication.TrustedOctopusServers", string.Format("[{3}\"Thumbprint\":\"{0}\",\"CommunicationStyle\":\"TentacleActive\",\"Address\":\"{1}\",\"Squid\":\"{2}\"{4}]", serverThumbprint, serverApiAddress, serverSquid, "{", "}"));
            doc.Save(configPath);

            StartService();

            // todo: Check machine is online and redeploy sites to it

            return true;
        }

        private void StartService()
        {
            using (var sc = new ServiceController {ServiceName = "OctopusDeploy Tentacle"})
            {
                try
                {
                    _log.Debug("Starting service");
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 10));

                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        _log.Information("Started Tentacle successfully");
                    }
                    else
                    {
                        _log.Error("Service failed to start; current state: {status}", sc.Status);
                        throw new Exception("Failed to start Tentacle");
                    }
                }
                catch (InvalidOperationException e)
                {
                    _log.Error(e, "Could not start the service.");
                }
            }
        }
    }

    static class XElementExtensions
    {
        public static XElement AddSetting(this XElement settingsElement, string name, string value)
        {
            var thumbprintElement = new XElement("set", value);
            thumbprintElement.Add(new XAttribute("key", name));
            settingsElement.Add(thumbprintElement);
            return settingsElement;
        }
    }
}
