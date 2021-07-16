using System;
using System.Diagnostics;
using System.IO;

namespace SocketIOClient.IntegrationTest.Helpers
{
    public static class CommandHelper
    {
        public static Process RunCommand(string fileName, string argument, string workingDirectory = null)
        {
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());
            }

            var processStartInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = argument,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized,
            };

            var process = Process.Start(processStartInfo);

            if (process == null)
            {
                throw new Exception("Process should not be null.");
            }

            return process;
        }
    }
}
