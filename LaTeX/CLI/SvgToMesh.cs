using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;

namespace PrimerTools.LaTeX;

internal class SvgToMesh
{
    public string GetPathToExisting(string identifier)
    {
        var dirPath = "addons/PrimerTools/LaTeX";
        var gltfDirPath = Path.Combine(dirPath, "gltf");
        if (!Directory.Exists(gltfDirPath)) Directory.CreateDirectory(gltfDirPath);
        var destinationPath = Path.Combine(gltfDirPath, GenerateFileName(identifier) + ".gltf");
        return File.Exists(destinationPath) ? destinationPath : string.Empty;
    }
    
    public async Task<string> ConvertSvgToMesh(string svgPath, string identifier, bool openBlender = false)
    {
        var dirPath = "addons/PrimerTools/LaTeX";
        var gltfDirPath = Path.Combine(dirPath, "gltf");
        if (!Directory.Exists(gltfDirPath)) Directory.CreateDirectory(gltfDirPath);
        var destinationPath = Path.Combine(gltfDirPath, GenerateFileName(identifier) + ".gltf");
        
        var scriptPath = Path.Combine(dirPath, "svg_to_mesh.py");
        
        // Queue the actual processing work
        return await LatexProcessQueue.EnqueueAsync(
            async () => await ProcessSvgToMesh(svgPath, identifier, openBlender, scriptPath, destinationPath),
            $"SVG: {identifier}"
        );
    }
    
    private async Task<string> ProcessSvgToMesh(string svgPath, string identifier, bool openBlender, string scriptPath, string destinationPath)
    {
        GD.Print($"[SvgToMesh] Starting processing: {identifier}");
        GD.Print($"[SvgToMesh] SVG path: {svgPath}");
        
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
        string result = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            throw new Exception($"Blender process failed with exit code {process.ExitCode}");
        }
        
        GD.Print($"[Blender] Completed: {identifier}");
        GD.Print(result);
        
        return destinationPath;
    }
    
    public static string GenerateFileName(string identifier)
    {
        // Replace invalid file name characters with '_'. 
        // This list covers characters invalid in Windows and the '/' for UNIX-based systems.
        var invalidChars = new string(Path.GetInvalidFileNameChars()) + " " + "\x00..\x1F";
        var sanitized = Regex.Replace(identifier, $"[{Regex.Escape(invalidChars)}]", "_");

        // Shorten if too long to avoid path length issues, keeping under 255 characters
        const int maxBaseLength = 100;
        if (sanitized.Length > maxBaseLength)
        {
            sanitized = sanitized[..maxBaseLength];
        }

        // Generate a hash of the original expression for uniqueness
        string hash = GetShortHash(identifier);

        // Combine sanitized expression with hash
        string fileName = $"{sanitized}_{hash}";

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
