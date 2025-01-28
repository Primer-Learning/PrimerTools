using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace PrimerTools.Latex
{
    public class CliProgram
    {
        public class ExecutionResult
        {
            public int exitCode;
            public string stdout;
            public string stderr;
        }


        readonly string binaryPath;
        public Dictionary<string, string> EnvVars { get; } = new();

        public CliProgram(string bin) => binaryPath = bin;


        public ExecutionResult Execute(string cwd, string[] args, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            using var process = new Process {
                StartInfo = CreateStartInfo(args, cwd)
            };

            var wasKilled = WaitForCompletion(ct, process);

            ct.ThrowIfCancellationRequested();

            if (!wasKilled) {
                return new ExecutionResult {
                    exitCode = process.ExitCode,
                    stdout = process.StandardOutput.ReadToEnd(),
                    stderr = process.StandardError.ReadToEnd(),
                };
            }

            var stdout = Path.Combine(cwd, "stdout.txt");
            var stderr = Path.Combine(cwd, "stderr.txt");

            using (var stdoutFile = File.Open(stdout, FileMode.Append)) {
                process.StandardOutput.BaseStream.CopyTo(stdoutFile);
            }

            using (var stderrFile = File.Open(stderr, FileMode.Append)) {
                process.StandardError.BaseStream.CopyTo(stderrFile);
            }

            throw new TimeoutException($"Process {binaryPath} was killed.");
        }

        private static bool WaitForCompletion(CancellationToken ct, Process process)
        {
            process.Start();

            while (!process.HasExited && !ct.IsCancellationRequested) {
                process.WaitForExit(200);
            }

            var forceStop = !process.HasExited;
            if (forceStop) process.Kill();

            return forceStop;
        }

        private ProcessStartInfo CreateStartInfo(string[] args, string cwd)
        {
            var startInfo = new ProcessStartInfo(binaryPath) {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = cwd,
                CreateNoWindow = true,
            };

            foreach (var arg in args) {
                startInfo.ArgumentList.Add(arg);
            }

            foreach (var dir in EnvVars) {
                startInfo.Environment.Add(dir.Key, dir.Value);
            }

            return startInfo;
        }
    }
}
