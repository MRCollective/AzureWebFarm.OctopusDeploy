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

        private static readonly string[] RoleConfigurations = new[] { OctopusServerConfigName, OctopusApiKeyConfigName, TentacleEnvironmentConfigName, TentacleRoleConfigName };

        private static string _octopusServer;
        private static string _octopusApiKey;
        private static string _tentacleEnvironment;
        private static string _tentacleRole;

        public ConfigSettings(Func<string, string> configSettingsGetter)
        {
            _octopusServer = configSettingsGetter(OctopusServerConfigName);
            _octopusApiKey = configSettingsGetter(OctopusServerConfigName);
            _tentacleEnvironment = configSettingsGetter(OctopusServerConfigName);
            _tentacleRole = configSettingsGetter(OctopusServerConfigName);
        }

        public string OctopusServer { get { return _octopusServer; } }
        public string OctopusApiKey { get { return _octopusApiKey; } }
        public string TentacleEnvironment { get { return _tentacleEnvironment; } }
        public string TentacleRole { get { return _tentacleRole; } }

        public bool IsSettingName(string name)
        {
            return RoleConfigurations.Contains(name);
        }
    }
}
