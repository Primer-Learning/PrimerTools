using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace PrimerTools;

[Tool]
public partial class SceneRecorder : Node
{
    [Export] private bool _record;
    public bool IsRecording => _record;

    [Export] private bool _quitWhenFinished;
    
    public enum OutputResolutionOptions
    {
        SD,
        HD,
        FHD,
        UHD
    }

    private bool _setOutputResolution;
    private OutputResolutionOptions _outputResolution;
    
    [Export]
    public OutputResolutionOptions OutputResolution
    {
        get => _outputResolution;
        set
        {
            if (!_setOutputResolution) // Prevents it running on build
            {
                _setOutputResolution = true;
                return;
            }
            _outputResolution = value;
            
            var (width, height) = value switch
            {
                OutputResolutionOptions.SD => (854, 480),
                OutputResolutionOptions.HD => (1280, 720),
                OutputResolutionOptions.FHD => (1920, 1080),
                OutputResolutionOptions.UHD => (3840, 2160),
                _ => (1280, 720)
            };
            
            if (value == _outputResolution && !Engine.IsEditorHint())
            {
                GD.PrintErr("Unrecognized output resolution");
            }
            
            ProjectSettings.SetSetting("display/window/size/viewport_height", height);
            ProjectSettings.SetSetting("display/window/size/viewport_width", width);
            ProjectSettings.Save();
        }
    }
    
    private string _sceneName;
    private string _baseDirectory;
    private string SceneDirectory => Path.Combine(_baseDirectory, "current_take");

    [Export]
    public string SceneName
    {
        get => _sceneName;
        set
        {
            _sceneName = value;
            _baseDirectory = Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "png", _sceneName);
            DebouncedCreateDirectory();
        }
    }
    
    private CancellationTokenSource _debounceCts;
    
    private async void DebouncedCreateDirectory()
    {
        try 
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();

            await Task.Delay(1000, _debounceCts.Token);
            CreateRecordingDirectory();
        }
        catch (OperationCanceledException) {}
    }

    private void CreateRecordingDirectory()
    {
        Directory.CreateDirectory(SceneDirectory);
        var file = Path.Combine(SceneDirectory, "image.png");
        GD.Print($"Recording directory: {file}");
        
        // Set metadata on the parent node for movie maker mode
        GetParent()?.SetMeta("movie_file", file);
    }
    
    private void MovePreviousTakeToNumberedDirectory()
    {
        if (!Directory.Exists(SceneDirectory) || !Directory.EnumerateFileSystemEntries(SceneDirectory).Any()) 
            return;
    
        var number = 1;
        while (Directory.Exists(Path.Combine(_baseDirectory, $"take_{number}")))
        {
            number++;
        }

        var targetDirectory = Path.Combine(_baseDirectory, $"take_{number}");
        Directory.CreateDirectory(targetDirectory);

        foreach (string sourcePath in Directory.GetFiles(SceneDirectory, "*.png", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(SceneDirectory, sourcePath);
            var targetPath = Path.Combine(targetDirectory, relativePath);
            File.Move(sourcePath, targetPath);
        }
    }
    
    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        
        if (_record)
        {
            if (string.IsNullOrEmpty(_sceneName)) 
            {
                GD.PrintErr("Recording is enabled, but scene name is empty.");
            }
            else
            {
                MovePreviousTakeToNumberedDirectory();
                CreateRecordingDirectory();
            }
            
            // Hide FPS viewer if present
            var fpsViewer = GetParent().GetChildren().OfType<FPSViewer>().FirstOrDefault();
            if (fpsViewer != null) fpsViewer.Visible = false;
        }
    }
    
    // Call this when the animation/sequence is complete
    public void OnSequenceComplete()
    {
        if (_record && _quitWhenFinished)
        {
            GetTree().Quit();
        }
    }
}
