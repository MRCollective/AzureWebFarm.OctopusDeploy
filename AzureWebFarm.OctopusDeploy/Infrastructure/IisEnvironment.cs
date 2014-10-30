using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Web.Administration;
using Serilog;

namespace AzureWebFarm.OctopusDeploy.Infrastructure
{
    internal static class IisEnvironment
    {
        public static void WaitForAllHttpRequestsToEnd()
        {
            // http://blogs.msdn.com/b/windowsazure/archive/2013/01/14/the-right-way-to-handle-azure-onstop-events.aspx
            var pcrc = new PerformanceCounter("ASP.NET", "Requests Current", "");
            while (true)
            {
                var rc = pcrc.NextValue();
                Log.Information("ASP.NET Requests Current = {0}, {1}.", rc, rc <= 0 ? "permitting role exit" : "blocking role exit");
                if (rc <= 0)
                    break;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        public static void PurgeAllSites()
        {
            using (var serverManager = new ServerManager())
            {
                var applications = serverManager.ApplicationPools.ToList();

                if (!applications.Any())
                {
                    // There is nothing to do.

                    return;
                }

                foreach (var appPool in applications)
                {
                    appPool.Stop();
                    
                    // Find all site & applications using this pool.
                    
                    var sites = serverManager.Sites
                        .Where(site => site.Applications.Any(x => x.ApplicationPoolName == appPool.Name)).ToList();

                    foreach (var site in sites)
                    {
                        serverManager.Sites[site.Name].Stop();

                        serverManager.Sites.Remove(site);
                    }

                    serverManager.ApplicationPools.Remove(appPool);
                }

                serverManager.CommitChanges();
            }
        }

        public static void ActivateAppInitialisationModuleForAllSites()
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
    }
}
