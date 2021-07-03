using SocketIOClient.Test.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace SocketIOClient.Test.SocketIOTests
{
    public class BaseServerManager
    {
        private readonly string directory;
        private List<Process> processesToKill;

        public BaseServerManager(string directory) 
        {
            this.directory = directory;
        }

        public void Create()
        {
            var installProcess = CommandHelper.RunCommand("npm", "install", Path.GetFullPath(directory));
            
            if (!installProcess.WaitForExit(60000)) 
            {
                throw new System.SystemException("Failed restoring packages of server");
            }

            var before = Process.GetProcesses().ToList().Select(x => x.Id);
            var startProcess = CommandHelper.RunCommand("npm", "start", Path.GetFullPath(directory));
            var after = Process.GetProcesses().ToList();
            this.processesToKill = this.GetProcessesToKill(before, after);
        }

        public void Destroy()
        {
            foreach (var process in processesToKill)
            {
                process.Kill();
            }
        }

        private List<Process> GetProcessesToKill(IEnumerable<int> before, List<Process> after)
        {
            var processesToKill = after;
            processesToKill.RemoveAll(x => before.Contains(x.Id) && !x.ProcessName.Contains("npm"));
            return processesToKill;
        }
    }
}