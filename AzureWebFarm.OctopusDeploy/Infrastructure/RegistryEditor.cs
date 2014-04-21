using System.Linq;
using Microsoft.Win32;
using Serilog;

namespace AzureWebFarm.OctopusDeploy.Infrastructure
{
    public interface IRegistryEditor
    {
        void DeleteLocalMachineTree(params string[] pathToTree);
    }

    class RegistryEditor : IRegistryEditor
    {
        public void DeleteLocalMachineTree(params string[] pathToTree)
        {
            var keyToDelete = pathToTree[pathToTree.Length - 1];
            var currentNode = Registry.LocalMachine;

            for (var i = 0; i < pathToTree.Length - 1; i++)
            {
                currentNode = currentNode.OpenSubKey(pathToTree[i], true);
                if (currentNode == null)
                {
                    Log.Debug("Couldn't find HKLM:" + string.Join("/", pathToTree));
                    return;
                }
            }

            if (currentNode.GetSubKeyNames().Contains(keyToDelete))
                currentNode.DeleteSubKeyTree(keyToDelete);
        }
    }
}
