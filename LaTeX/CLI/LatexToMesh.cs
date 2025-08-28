using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PrimerTools.Latex;

namespace PrimerTools.LaTeX;

internal class LatexToMesh
{
    private static readonly string[] XelatexArguments = {
        "-no-pdf",
        "-interaction=batchmode",
        "-halt-on-error"
    };

    private static readonly string[] DvisvgmArguments = {
        "--no-fonts=1"
    };

    internal TempDir rootTempDir = new();
    private readonly SvgToMesh svgToMesh = new();

    public Task<string> RenderToSvg(LatexInput config, CancellationToken ct)
    {
        return Task.Run(() => RenderToSvgSync(config, ct), ct);
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

        var args = XelatexArguments.Append($"-output-directory={tmpDir}", sourcePath);
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
        var args = DvisvgmArguments.Append(dviPath, $"--output={outputPath}");

        var result = LatexBinaries.Dvisvgm(workingDirectory, args, ct);
        DumpStandardOutputs(workingDirectory, result, "dvisvgm");

        if (result.exitCode != 0) {
            throw new Exception($"Got dvisvgm error(s): {result.stderr}");
        }
    }

    public string GetPathToExisting(string latex)
    {
        return svgToMesh.GetPathToExisting(latex);
    }
    
    public async Task<string> MeshFromExpression(string latex, bool openBlender = false)
    {
        // Check if we already have a mesh for this expression
        var existing = GetPathToExisting(latex);
        if (!string.IsNullOrEmpty(existing)) return existing;
        
        // Queue the LaTeX to SVG conversion and then SVG to mesh conversion
        return await LatexProcessQueue.EnqueueAsync(
            async () => await ProcessLatexExpression(latex, openBlender),
            $"LaTeX: {latex}"
        );
    }
    
    private async Task<string> ProcessLatexExpression(string latex, bool openBlender)
    {
        GD.Print($"[LatexToMesh] Starting LaTeX processing: {latex}");
        
        // First convert LaTeX to SVG
        var input = LatexInput.From("H" + latex); // The H gets removed in blender after alignment
        var svgPath = await RenderToSvg(input, default);
        GD.Print($"[LatexToMesh] SVG created at: {svgPath}");
        
        // Then convert SVG to mesh using the shared converter
        // Note: We don't re-queue this since we're already in a queued task
        var dirPath = ProjectSettings.GlobalizePath("res://addons/PrimerTools/LaTeX");
        var gltfDirPath = Path.Combine(dirPath, "gltf");
        if (!Directory.Exists(gltfDirPath)) Directory.CreateDirectory(gltfDirPath);
        var destinationPath = Path.Combine(gltfDirPath, SvgToMesh.GenerateFileName(latex) + ".gltf");
        
        // Use the original script that expects the 'H' character for LaTeX
        var scriptPath = Path.Combine(dirPath, "svg_to_mesh.py");
        
        // TODO: Get the blender path from Godot's user settings
        var blenderPath = @"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe";
        var startInfo = new ProcessStartInfo(blenderPath)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        
        // Properly quote paths that may contain spaces
        var quotedScriptPath = $"\"{scriptPath}\"";
        var quotedSvgPath = $"\"{svgPath}\"";
        var quotedDestinationPath = $"\"{destinationPath}\"";
        
        if (openBlender)
        {
            startInfo.Arguments = $"--python {quotedScriptPath} -- {quotedSvgPath} {quotedDestinationPath}";
            startInfo.CreateNoWindow = false;
        }
        else
        {
            startInfo.Arguments = $"--background --python {quotedScriptPath} -- {quotedSvgPath} {quotedDestinationPath}";
            startInfo.CreateNoWindow = true;
        }
        
        using Process process = Process.Start(startInfo);
        string result = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            throw new Exception($"Blender process failed with exit code {process.ExitCode}");
        }
        
        GD.Print($"[Blender] Completed: {latex}");
        GD.Print(result);
        
        return destinationPath;
    }

    private static void DumpStandardOutputs(TempDir workingDir, CliProgram.ExecutionResult result, string name)
    {
        var stdout = workingDir.GetChildPath($"{name}.stdout");
        File.WriteAllText(stdout, result.stdout);

        var stderr = workingDir.GetChildPath($"{name}.stderr");
        File.WriteAllText(stderr, result.stderr);
    }
}
