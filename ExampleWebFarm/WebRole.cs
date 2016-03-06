using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace WebFarm
{
    public class WebRole : RoleEntryPoint
    {
        private readonly AzureWebFarm.OctopusDeploy.WebFarmRole _webFarmRole;

        public WebRole()
        {
            _webFarmRole = new AzureWebFarm.OctopusDeploy.WebFarmRole();
        }

        public override bool OnStart()
        {
            return _webFarmRole.OnStart();
        }

        public override void Run()
        {
            Task.Run(() => _webFarmRole.Run(new CancellationTokenSource().Token)).Wait();
        }

        public override void OnStop()
        {
            _webFarmRole.OnStop();
        }
    }
}
