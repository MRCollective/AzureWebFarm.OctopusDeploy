using System;
using System.Linq;
using System.Threading;
using AzureWebFarm.OctopusDeploy.Infrastructure;
using Microsoft.Web.Administration;
using Microsoft.WindowsAzure.ServiceRuntime;
using Serilog;

namespace AzureWebFarm.OctopusDeploy
{
    public class WebRole
    {
        private readonly ConfigSettings _config;
        private readonly Infrastructure.OctopusDeploy _octopusDeploy;

        public WebRole(string machineName = null)
        {
            Log.Logger = Logging.GetAzureLogger();
            _config = new ConfigSettings(RoleEnvironment.GetConfigurationSettingValue);
            _octopusDeploy = new Infrastructure.OctopusDeploy(machineName ?? AzureEnvironment.GetMachineName(_config), _config);
            AzureEnvironment.RequestRecycleIfConfigSettingChanged(_config);
        }

        public bool OnStart()
        {
            _octopusDeploy.ConfigureTentacle();
            _octopusDeploy.DeployAllCurrentReleasesToThisMachine();
            return true;
        }

        public void Run()
        {
            if (RoleEnvironment.IsEmulated)
                Thread.Sleep(-1);

            while (true)
            {
                try
                {
                    // https://github.com/sandrinodimattia/WindowsAzure-IISApplicationInitializationModule
                    using (var serverManager = new ServerManager())
                    {
                        foreach (var application in serverManager.Sites.SelectMany(site => site.Applications))
                            application["preloadEnabled"] = true;

                        foreach (var appPool in serverManager.ApplicationPools)
                            appPool["startMode"] = "AlwaysRunning";

                        serverManager.CommitChanges();
                    }
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

        public void OnStop()
        {
            _octopusDeploy.DeleteMachine();
            AzureEnvironment.WaitForAllHttpRequestsToEnd();
        }
    }

    
}
