using System;
using System.ComponentModel.Design;
using System.Net.Configuration;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly bool _purgesites;

        /// <summary>
        /// Create the web role coordinator.
        /// </summary>
        /// <param name="machineName">Specify the machineName if you would like to override the default machine name configuration.</param>
        /// <param name="purgeSites">Specify true to remove all sites before installing the tentacle</param>
        public WebFarmRole(string machineName = null,bool purgeSites = false)
        {
            Log.Logger = AzureEnvironment.GetAzureLogger();
            var config = AzureEnvironment.GetConfigSettings();

            machineName = machineName ?? AzureEnvironment.GetMachineName(config);
            var octopusRepository = Infrastructure.OctopusDeploy.GetRepository(config);
            var processRunner = new ProcessRunner();
            var registryEditor = new RegistryEditor();
            
            _octopusDeploy = new Infrastructure.OctopusDeploy(machineName, config, octopusRepository, processRunner, registryEditor);
            _purgesites = purgeSites;

            AzureEnvironment.RequestRecycleIfConfigSettingChanged(config);
        }

        /// <summary>
        /// Call from the RoleEntryPoint.OnStart() method.
        /// </summary>
        /// <returns>true; throws exception is there is an error</returns>
        public bool OnStart()
        {
            if (_purgesites)
            {
                IisEnvironment.PurgeAllSites();
            }
            
            _octopusDeploy.ConfigureTentacle();
            _octopusDeploy.DeployAllCurrentReleasesToThisMachine();
            
            return true;
        }

        /// <summary>
        /// Call from the RoleEntryPoint.Run() method.
        /// Note: This method is an infinite loop; call from a Thread/Task if you want to run other code alongside.
        /// </summary>
        public async Task Run(CancellationToken token)
        {
            // Don't want to configure IIS if we are emulating; just sleep forever
            try
            {
                if (RoleEnvironment.IsEmulated)
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(100, token);
                    }
                }

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        IisEnvironment.ActivateAppInitialisationModuleForAllSites();
                    }
                    catch (Exception e)
                    {
                        Log.Warning(e, "Failure to configure IIS");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(10), token);
                }
            }
            catch (TaskCanceledException)
            {
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
