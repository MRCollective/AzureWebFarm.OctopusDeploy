using System;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace AzureWebFarm.OctopusDeploy.Infrastructure
{
    internal static class AzureRoleEnvironment
    {
        public static Func<bool> IsAvailable = () => RoleEnvironment.IsAvailable;
        public static Func<string> CurrentRoleInstanceId = () => IsAvailable() ? RoleEnvironment.CurrentRoleInstance.Id : Environment.MachineName;
        public static Func<bool> IsEmulated = () => RoleEnvironment.IsEmulated;
    }
}
