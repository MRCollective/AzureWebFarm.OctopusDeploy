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

        public static void PurgeAllDefaultSites()
        {
            using (var serverManager = new ServerManager())
            {
                Guid appGuid;

                var applications = serverManager.ApplicationPools
                    .Where(appPool => Guid.TryParse(appPool.Name, out appGuid)).ToList();

                if (!applications.Any())
                {
                    // There does'nt seem to be any to remove.

                    return;
                }

                foreach (var appPool in applications)
                {
                    // Assumption any pool with name of a GUID was created by Azure.
                    
                    appPool.Stop();
                    
                    // Find all site & applications using this pool (Should one be one).
                    
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
