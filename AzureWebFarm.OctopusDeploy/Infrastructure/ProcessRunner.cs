using System;
using System.Diagnostics;
using Serilog;

namespace AzureWebFarm.OctopusDeploy.Infrastructure
{
    internal interface IProcessRunner
    {
        void Run(string executable, string arguments);
    }

    internal class ProcessRunner : IProcessRunner
    {
        public void Run(string executable, string arguments)
        {
            Log.Debug("Running {0} with {1}", executable, arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            };

            try
            {
                var process = Process.Start(startInfo);
                var stderr = process.StandardError.ReadToEnd();
                var stdout = process.StandardOutput.ReadToEnd();

                if (process.ExitCode != 0)
                    throw new Exception(string.Format("Non-zero exit code returned ({0}). Stdout: {1} StdErr: {2}", process.ExitCode, stdout, stderr));

                Log.Information("Executed {executable} with {arguments}. {stdout}. {stderr}.", executable, arguments, stdout, stderr);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to execute {executable} with {arguments}", executable, arguments);
                throw;
            }
        }
    }
}
