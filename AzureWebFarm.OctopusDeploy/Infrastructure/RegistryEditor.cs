using System.Linq;
using Microsoft.Win32;
using Serilog;

namespace AzureWebFarm.OctopusDeploy.Infrastructure
{
    /// <summary>
    /// Performs edits to the machines local registry.
    /// </summary>
    public interface IRegistryEditor
    {
        /// <summary>
        /// Recursively delete a sub tree from the HKLM node in local registry.
        /// </summary>
        /// <param name="pathToTree">The path to the sub tree to delete</param>
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
