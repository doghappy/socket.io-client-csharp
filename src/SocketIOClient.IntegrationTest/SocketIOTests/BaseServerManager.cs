using SocketIOClient.IntegrationTest.Helpers;
using System.Diagnostics;
using System.IO;

namespace SocketIOClient.IntegrationTest.SocketIOTests
{
    public class BaseServerManager
    {
        private readonly string directory;
        private Process nodeProcess;

        public BaseServerManager(string directory) 
        {
            this.directory = directory;
        }

        public void Create()
        {
            var workingDirectory = Path.GetFullPath(directory);
            var npmInstallProcess = CommandHelper.RunCommand("npm", "install", workingDirectory);
            
            // We wait until the installation process is finished (or we get a timeout).
            if (!npmInstallProcess.WaitForExit(60000)) 
            {
                throw new System.SystemException("Failed restoring packages of server");
            }

            // Time to run the node server.
            this.nodeProcess = CommandHelper.RunCommand("node", "app.js", workingDirectory);
        }

        public void Destroy()
        {
            nodeProcess?.Kill();
        }
    }
}