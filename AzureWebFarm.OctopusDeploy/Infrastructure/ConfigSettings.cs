using System;
using System.Linq;

namespace AzureWebFarm.OctopusDeploy.Infrastructure
{
    internal interface IConfigSettings {
        string OctopusServer { get; }
        string OctopusApiKey { get; }
        string TentacleEnvironment { get; }
        string TentacleRole { get; }
        string TentacleMachineNameSuffix { get; }
        string TentacleDeploymentsPath { get; }
        string TentacleInstallPath { get; }
    }

    internal class ConfigSettings : IConfigSettings
    {
        private const string OctopusServerConfigName = "OctopusServer";
        private const string OctopusApiKeyConfigName = "OctopusApiKey";
        private const string TentacleEnvironmentConfigName = "TentacleEnvironment";
        private const string TentacleRoleConfigName = "TentacleRole";
        private const string TentacleDeploymentsPathConfigName = "Deployments";
        private const string TentacleInstallPathConfigName = "Install";
        private const string TentacleMachineNameSuffixConfigName = "TentacleMachineNameSuffix";

        private static readonly string[] ConfigSettingsNames = { OctopusServerConfigName, OctopusApiKeyConfigName, TentacleEnvironmentConfigName, TentacleRoleConfigName, TentacleMachineNameSuffixConfigName };

        private static string _octopusServer;
        private static string _octopusApiKey;
        private static string _tentacleEnvironment;
        private static string _tentacleRole;
        private readonly string _tentacleMachineNameSuffix;
        private readonly string _tentacleDeploymentsPath;
        private readonly string _tentacleInstallPath;
        
        public ConfigSettings(Func<string, string> configSettingsGetter, Func<string, string> configPathGetter)
        {
            _octopusServer = SafeGetConfigSetting(configSettingsGetter, OctopusServerConfigName);
            _octopusApiKey = SafeGetConfigSetting(configSettingsGetter, OctopusApiKeyConfigName);
            _tentacleEnvironment = SafeGetConfigSetting(configSettingsGetter, TentacleEnvironmentConfigName);
            _tentacleRole = SafeGetConfigSetting(configSettingsGetter, TentacleRoleConfigName);
            _tentacleMachineNameSuffix = SafeGetConfigSetting(configSettingsGetter, TentacleMachineNameSuffixConfigName);

            _tentacleDeploymentsPath = SafeGetConfigSetting(configPathGetter, TentacleDeploymentsPathConfigName);
            _tentacleInstallPath = SafeGetConfigSetting(configPathGetter, TentacleInstallPathConfigName);
        }

        public string OctopusServer { get { return _octopusServer; } }
        public string OctopusApiKey { get { return _octopusApiKey; } }
        public string TentacleEnvironment { get { return _tentacleEnvironment; } }
        public string TentacleRole { get { return _tentacleRole; } }
        public string TentacleMachineNameSuffix { get { return _tentacleMachineNameSuffix; } }

        public string TentacleDeploymentsPath { get { return _tentacleDeploymentsPath; } }
        public string TentacleInstallPath { get { return _tentacleInstallPath; } }

        public bool IsConfigSettingName(string name)
        {
            return ConfigSettingsNames.Contains(name);
        }

        public string SafeGetConfigSetting(Func<string, string> configSettingsGetter, string settingName)
        {
            try
            {
                return configSettingsGetter(settingName) ?? "";
            }
            catch (Exception e)
            {
                throw new UnableToGetConfigSettingException(settingName, e);
            }
        }
    }

    /// <summary>
    /// Error getting a config setting.
    /// </summary>
    public class UnableToGetConfigSettingException : Exception
    {
        /// <summary>
        /// Create the exception for the setting in question.
        /// </summary>
        /// <param name="settingName">The name of the setting an error was raised for</param>
        /// <param name="exception">The exception that was raised when getting the setting</param>
        public UnableToGetConfigSettingException(string settingName, Exception exception)
            : base("Unable to get config setting: " + settingName, exception) {}
    }
}
