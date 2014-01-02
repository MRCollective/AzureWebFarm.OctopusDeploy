using System;
using System.Linq;

namespace AzureWebFarm.OctopusDeploy.Infrastructure
{
    internal class ConfigSettings
    {
        private const string OctopusServerConfigName = "OctopusServer";
        private const string OctopusApiKeyConfigName = "OctopusApiKey";
        private const string TentacleEnvironmentConfigName = "TentacleEnvironment";
        private const string TentacleRoleConfigName = "TentacleRole";
        private const string TentacleDeploymentsPathConfigName = "Deployments";
        private const string TentacleInstallPathConfigName = "Install";

        private static readonly string[] ConfigSettingsNames = new[] { OctopusServerConfigName, OctopusApiKeyConfigName, TentacleEnvironmentConfigName, TentacleRoleConfigName };

        private static string _octopusServer;
        private static string _octopusApiKey;
        private static string _tentacleEnvironment;
        private static string _tentacleRole;
        private readonly string _tentacleDeploymentsPath;
        private readonly string _tentacleInstallPath;

        public ConfigSettings(Func<string, string> configSettingsGetter, Func<string, string> configPathGetter)
        {
            _octopusServer = configSettingsGetter(OctopusServerConfigName);
            _octopusApiKey = configSettingsGetter(OctopusApiKeyConfigName);
            _tentacleEnvironment = configSettingsGetter(TentacleEnvironmentConfigName);
            _tentacleRole = configSettingsGetter(TentacleRoleConfigName);

            _tentacleDeploymentsPath = configPathGetter(TentacleDeploymentsPathConfigName);
            _tentacleInstallPath = configPathGetter(TentacleInstallPathConfigName);
        }

        public string OctopusServer { get { return _octopusServer; } }
        public string OctopusApiKey { get { return _octopusApiKey; } }
        public string TentacleEnvironment { get { return _tentacleEnvironment; } }
        public string TentacleRole { get { return _tentacleRole; } }

        public string TentacleDeploymentsPath { get { return _tentacleDeploymentsPath; } }
        public string TentacleInstallPath { get { return _tentacleInstallPath; } }

        public bool IsConfigSettingName(string name)
        {
            return ConfigSettingsNames.Contains(name);
        }
    }
}
