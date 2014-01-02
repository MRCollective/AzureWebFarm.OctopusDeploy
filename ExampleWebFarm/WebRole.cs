using Microsoft.WindowsAzure.ServiceRuntime;

namespace WebFarm
{
    public class WebRole : RoleEntryPoint
    {
        private readonly AzureWebFarm.OctopusDeploy.WebRole _webRole;

        public WebRole()
        {
            _webRole = new AzureWebFarm.OctopusDeploy.WebRole();
        }

        public override bool OnStart()
        {
            return _webRole.OnStart();
        }

        public override void Run()
        {
            _webRole.Run();
        }

        public override void OnStop()
        {
            _webRole.OnStop();
        }
    }
}
