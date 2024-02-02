using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace PrimerTools.Latex
{
    internal static class LatexBinaries
    {
        // internal static readonly DirectoryInfo rootTempDir;
        private const string LatexBinDir = "";
        private const string XelatexDir = "";
        private const string DvisvgmDir = "";

        public static CliProgram.ExecutionResult Xelatex(TempDir cwd, string[] args, CancellationToken ct)
        {
            var program = GetCliProgram(XelatexDir, "xelatex");
            return program.Execute(cwd.FullPath, args, ct);
        }

        public static CliProgram.ExecutionResult Dvisvgm(TempDir cwd, string[] args, CancellationToken ct)
        {
            var program = GetCliProgram(DvisvgmDir, "dvisvgm");
            return program.Execute(cwd.FullPath, args, ct);
        }

        public static string GetXelatexErrorLogsFrom(TempDir cwd, string filename)
        {
            var logFile = cwd.GetChildPath(filename);
            if (!File.Exists(logFile)) return null;

            var errors =
                from line in File.ReadAllLines(logFile)
                where line.StartsWith("! ") && line.Length > 2
                select line[2..];

            return string.Join(", ", errors);
        }


        private static CliProgram GetCliProgram(string setting, string filename)
        {
            var path = GetBinary(setting, filename);
            if (string.IsNullOrWhiteSpace(path)) return null;

            var binDir = string.IsNullOrWhiteSpace(LatexBinDir)
                ? Path.GetDirectoryName(path)
                : LatexBinDir;

            var cli = new CliProgram(path);
            cli.EnvVars["PATH"] = binDir;
            return cli;
        }

        private static string GetBinary(string setting, string filename)
        {
            if (!string.IsNullOrWhiteSpace(setting)) {
                return setting;
            }

            if (!string.IsNullOrWhiteSpace(LatexBinDir)) {
                var found = FindBinary(LatexBinDir, filename);
                if (found is not null) return found;
            }

            foreach (var path in PathEnvVar()) {
                var found = FindBinary(path, filename);
                if (found is not null) return found;
            }

            throw new FileNotFoundException($"Could not find {filename} in your PATH. You can configure it in Unity editor preferences.");
        }

        private static string FindBinary(string path, string filename)
        {
            var plain = Path.Combine(path, filename);
            if (File.Exists(plain)) return plain;

            var exe = $"{plain}.exe";
            return File.Exists(exe) ? exe : null;
        }

        private static IEnumerable<string> PathEnvVar()
        {
            var delimiter = Path.PathSeparator;
            var path = Environment.GetEnvironmentVariable("PATH");
            return path?.Split(delimiter) ?? new string[] {};
        }
    }
}
