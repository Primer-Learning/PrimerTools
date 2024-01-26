using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PrimerTools.Latex;

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
            {string.Join(Environment.NewLine, config.headers)}
            \begin{{document}}
            \color{{white}}
            {config.code}
            \end{{document}}
        ");

        // PrimerLogger.Log("AAAAAA", new { dir, sourcePath});
        ExecuteXelatex(dir, sourcePath, ct);
        ExecuteDvisvgm(dir, outputPath, ct);

        ct.ThrowIfCancellationRequested();
        return File.ReadAllText(outputPath);
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

    private static void DumpStandardOutputs(TempDir workingDir, CliProgram.ExecutionResult result, string name)
    {
        var stdout = workingDir.GetChildPath($"{name}.stdout");
        File.WriteAllText(stdout, result.stdout);

        var stderr = workingDir.GetChildPath($"{name}.stderr");
        File.WriteAllText(stderr, result.stderr);
    }
}

