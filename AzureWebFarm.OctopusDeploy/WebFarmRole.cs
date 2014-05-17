using System;
using System.Threading;
using AzureWebFarm.OctopusDeploy.Infrastructure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Serilog;

namespace AzureWebFarm.OctopusDeploy
{
    /// <summary>
    /// Coordinates an OctopusDeploy-powered webfarm Azure Web Role.
    /// </summary>
    public class WebFarmRole
    {
        private readonly Infrastructure.OctopusDeploy _octopusDeploy;

        /// <summary>
        /// Create the web role coordinator.
        /// </summary>
        /// <param name="machineName">Specify the machineName if you would like to override the default machine name configuration.</param>
        public WebFarmRole(string machineName = null)
        {
            Log.Logger = AzureEnvironment.GetAzureLogger();
            var config = AzureEnvironment.GetConfigSettings();

            machineName = machineName ?? AzureEnvironment.GetMachineName(config);
            var octopusRepository = Infrastructure.OctopusDeploy.GetRepository(config);
            var processRunner = new ProcessRunner();
            var registryEditor = new RegistryEditor();
            _octopusDeploy = new Infrastructure.OctopusDeploy(machineName, config, octopusRepository, processRunner, registryEditor);

            AzureEnvironment.RequestRecycleIfConfigSettingChanged(config);
        }

        /// <summary>
        /// Call from the RoleEntryPoint.OnStart() method.
        /// </summary>
        /// <returns>true; throws exception is there is an error</returns>
        public bool OnStart()
        {
            _octopusDeploy.ConfigureTentacle();
            _octopusDeploy.DeployAllCurrentReleasesToThisMachine();
            return true;
        }

        /// <summary>
        /// Call from the RoleEntryPoint.Run() method.
        /// Note: This method is an infinite loop; call from a Thread/Task if you want to run other code alongside.
        /// </summary>
        public void Run()
        {
            // Don't want to configure IIS if we are emulating; just sleep forever
            if (RoleEnvironment.IsEmulated)
                Thread.Sleep(-1);

            while (true)
            {
                try
                {
                    IisEnvironment.ActivateAppInitialisationModuleForAllSites();
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Failure to configure IIS");
                }

                Thread.Sleep(TimeSpan.FromMinutes(10));
            }
        // ReSharper disable FunctionNeverReturns
        }
        // ReSharper restore FunctionNeverReturns

        /// <summary>
        /// Call from RoleEntryPoint.OnStop().
        /// </summary>
        public void OnStop()
        {
            _octopusDeploy.UninstallTentacle();
            _octopusDeploy.DeleteMachine();
            IisEnvironment.WaitForAllHttpRequestsToEnd();
        }
    }
}
