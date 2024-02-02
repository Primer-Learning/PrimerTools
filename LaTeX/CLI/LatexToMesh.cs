using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PrimerTools.Latex;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PrimerTools.LaTeX;
internal class LatexToMesh
{
    private static readonly string[] xelatexArguments = {
        "-no-pdf",
        "-interaction=batchmode",
        "-halt-on-error"
    };

    private static readonly string[] dvisvgmArguments = {
        "--no-fonts=1"
    };

    readonly object executionLock = new();
    Task<string> currentTask;

    internal TempDir rootTempDir = new();

    public Task<string> RenderToSvg(LatexInput config, CancellationToken ct)
    {
        lock (executionLock) {
            if (currentTask is not null && !currentTask.IsCompleted) {
                throw new Exception("A LaTeX rendering task is already running.");
            }

            return Task.Run(() => RenderToSvgSync(config, ct), ct);
        }
    }

    private string RenderToSvgSync(LatexInput config, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var dir = rootTempDir.CreateChild("build-", true);
        var sourcePath = dir.GetChildPath("source.tex");
        var outputPath = dir.GetChildPath("output.svg");

        ct.ThrowIfCancellationRequested();

        File.WriteAllText(sourcePath, @$"
            {string.Join((string)System.Environment.NewLine, config.headers)}
            \begin{{document}}
            \color{{white}}
            {config.code}
            \end{{document}}
        ");

        ExecuteXelatex(dir, sourcePath, ct);
        ExecuteDvisvgm(dir, outputPath, ct);

        ct.ThrowIfCancellationRequested();
        // return File.ReadAllText(outputPath);
        return outputPath;
    }

    private static void ExecuteXelatex(TempDir tmpDir, string sourcePath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var args = xelatexArguments.Append($"-output-directory={tmpDir}", sourcePath);
        var result = LatexBinaries.Xelatex(tmpDir, args, ct);

        ct.ThrowIfCancellationRequested();
        DumpStandardOutputs(tmpDir, result, "xelatex");

        if (result.exitCode != 0) {
            var errors = LatexBinaries.GetXelatexErrorLogsFrom(tmpDir, "source.log") ?? result.stderr;
            throw new Exception($"Got xelatex error(s): {errors}");
        }
    }

    private static void ExecuteDvisvgm(TempDir workingDirectory, string outputPath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var dviPath = workingDirectory.GetChildPath("source.xdv");
        var args = dvisvgmArguments.Append(dviPath, $"--output={outputPath}");

        var result = LatexBinaries.Dvisvgm(workingDirectory, args, ct);
        DumpStandardOutputs(workingDirectory, result, "dvisvgm");

        if (result.exitCode != 0) {
            throw new Exception($"Got dvisvgm error(s): {result.stderr}");
        }
    }

    public async Task<string> MeshFromExpression(string latex, bool openBlender = false)
    {
        var dirPath = "addons/PrimerTools/LaTeX";
        var scriptPath = Path.Combine(dirPath, "svg_to_mesh.py");
        var gltfDirPath = Path.Combine(dirPath, "gltf");
        if (!Directory.Exists(gltfDirPath)) Directory.CreateDirectory(gltfDirPath);
        var destinationPath = Path.Combine(gltfDirPath, GenerateFileName(latex) + ".gltf");
        if (File.Exists(destinationPath)) return destinationPath;
        
        var input = LatexInput.From("H" + latex); // The H gets removed in blender after alignment
        var svgPath = await RenderToSvg(input, default);
        
        // TODO: Get the blender path from Godot's user settings
        var blenderPath = @"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe";
        var startInfo = new ProcessStartInfo(blenderPath)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        if (openBlender)
        {
            startInfo.Arguments = $"--python {scriptPath} -- {svgPath} {destinationPath}";
            startInfo.CreateNoWindow = false;
        }
        else
        {
            startInfo.Arguments = $"--background --python {scriptPath} -- {svgPath} {destinationPath}";
            startInfo.CreateNoWindow = true;
        }
        
        using Process process = Process.Start(startInfo);
        using (StreamReader reader = process.StandardOutput)
        {
            string result = await reader.ReadToEndAsync();
            GD.Print(result);
        }
        
        return destinationPath;
    }

    private static void DumpStandardOutputs(TempDir workingDir, CliProgram.ExecutionResult result, string name)
    {
        var stdout = workingDir.GetChildPath($"{name}.stdout");
        File.WriteAllText(stdout, result.stdout);

        var stderr = workingDir.GetChildPath($"{name}.stderr");
        File.WriteAllText(stderr, result.stderr);
    }
    
    public static string GenerateFileName(string latexExpression)
    {
        // Replace invalid file name characters with '_'. 
        // This list covers characters invalid in Windows and the '/' for UNIX-based systems.
        var invalidChars = new string(Path.GetInvalidFileNameChars()) + " " + "\x00..\x1F";
        var sanitized = Regex.Replace(latexExpression, $"[{Regex.Escape(invalidChars)}]", "_");

        // Shorten if too long to avoid path length issues, keeping under 255 characters
        if (sanitized.Length > 200)
        {
            sanitized = sanitized[..200];
        }

        // Generate a hash of the original expression for uniqueness
        string hash = GetShortHash(latexExpression);

        // Combine sanitized expression with hash
        string fileName = $"{sanitized}_{hash}.txt"; // .txt or your preferred extension

        return fileName;
    }

    private static string GetShortHash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            // Compute hash - this returns byte array
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Convert byte array to a short string representation
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 4; i++) // Use only the first 4 bytes for a short hash
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}

