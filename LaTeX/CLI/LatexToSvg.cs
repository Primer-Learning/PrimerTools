using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PrimerTools.Latex;
using FileAccess = System.IO.FileAccess;

namespace PrimerTools.LaTeX;
internal class LatexToSvg
{
    private static readonly string[] xelatexArguments = {
        "-no-pdf",
        "-interaction=batchmode",
        "-halt-on-error",
    };

    private static readonly string[] dvisvgmArguments = {
        "--no-fonts=1",
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
        var input = LatexInput.From("H" + latex); // The H gets removed in blender after alignment
        var svgPath = await RenderToSvg(input, default);

        var blenderPath = @"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe";
        var scriptPath = "addons/PrimerTools/LaTeX/svg_to_mesh.py";
        var destinationPath = "addons/PrimerTools/LaTeX/latex_test_mesh.gltf";

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
    
    private static bool IsFileLocked(FileInfo file)
    {
        try
        {
            using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Close();
            }
        }
        catch (IOException)
        {
            return true;
        }

        return false;
    }

    private static void DumpStandardOutputs(TempDir workingDir, CliProgram.ExecutionResult result, string name)
    {
        var stdout = workingDir.GetChildPath($"{name}.stdout");
        File.WriteAllText(stdout, result.stdout);

        var stderr = workingDir.GetChildPath($"{name}.stderr");
        File.WriteAllText(stderr, result.stderr);
    }
}

