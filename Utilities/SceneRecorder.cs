using System;
using System.IO;
using System.Linq;
using Godot;

namespace PrimerTools;

[Tool]
public partial class SceneRecorder : Node
{
    // --- Singleton access ---
    public static SceneRecorder? Instance { get; private set; }
    public static bool IsOn => Instance?.IsRecording == true;

    public override void _EnterTree()
    {
        // First-in wins
        if (Instance == null) Instance = this;
        else
        {
            GD.PushWarning("Duplicate SceneRecorder autoload detected; freeing this one.");
            QueueFree();
            return;
        }
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
        base._ExitTree();
    }
    
    [Export] private bool _record;
    public bool IsRecording => _record;
    [Export] private bool _quitWhenFinished;
    [Export] private bool _transparentBackground;

    public enum OutputResolutionOptions { SD, HD, FHD, UHD }

    private OutputResolutionOptions _outputResolution = OutputResolutionOptions.HD;

    [Export]
    public OutputResolutionOptions OutputResolution
    {
        get => _outputResolution;
        set
        {
            if (_outputResolution == value) return;
            _outputResolution = value;
            if (Engine.IsEditorHint())
                UpdateResolutionSettings();
        }
    }

    private void UpdateResolutionSettings()
    {
        var (w, h) = _outputResolution switch
        {
            OutputResolutionOptions.SD  => (854, 480),
            OutputResolutionOptions.HD  => (1280, 720),
            OutputResolutionOptions.FHD => (1920, 1080),
            OutputResolutionOptions.UHD => (3840, 2160),
            _ => (1280, 720)
        };
        ProjectSettings.SetSetting("display/window/size/viewport_width", w);
        ProjectSettings.SetSetting("display/window/size/viewport_height", h);
        ProjectSettings.Save();
        GD.Print($"Resolution updated to {_outputResolution}: {w}x{h}");
    }

    [Export] private string _sceneName = "";
    public string SceneName => _sceneName;

    // --- New recording options ---

    public enum SaveMode { FreshDirectory, RepairDirectory }

    [Export] public SaveMode Mode { get; set; } = SaveMode.FreshDirectory;

    // When Mode == RepairDirectory, use this path; if empty, defaults to current_take.
    // Can be absolute or relative to base png/scene folder.
    [Export] public string RepairDirectory = "";

    // Optional explicit target viewport. If null, uses GetViewport() (root).
    [Export] public Viewport TargetViewport = null;

    // Frame window (inclusive). Leave EndFrame null to run until sequence quit.
    [Export] public int StartFrame = 0;
    [Export] public int EndFrame = -1;

    // ---- Internals ----
    private string _baseDirectory;
    private string _currentTakeDir;
    private string _activeOutputDir;
    private int _frame = 1;

    private string SceneDirectory => Path.Combine(_baseDirectory, "current_take");

    private void EnsureDir(string dir)
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private void MovePreviousTakeToNumberedDirectory()
    {
        if (!Directory.Exists(SceneDirectory) || !Directory.EnumerateFileSystemEntries(SceneDirectory).Any())
            return;

        var number = 1;
        while (Directory.Exists(Path.Combine(_baseDirectory, $"take_{number}")))
            number++;

        var targetDirectory = Path.Combine(_baseDirectory, $"take_{number}");
        Directory.CreateDirectory(targetDirectory);

        foreach (string sourcePath in Directory.GetFiles(SceneDirectory, "*.png", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(SceneDirectory, sourcePath);
            var targetPath = Path.Combine(targetDirectory, relativePath);

            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            File.Move(sourcePath, targetPath);
        }

        // Clean out current_take after move
        foreach (var leftover in Directory.EnumerateFileSystemEntries(SceneDirectory))
        {
            try
            {
                if (File.Exists(leftover)) File.Delete(leftover);
                else if (Directory.Exists(leftover)) Directory.Delete(leftover, recursive: true);
            }
            catch (Exception e) { GD.PushWarning($"Cleanup warning: {e.Message}"); }
        }

        GD.Print($"Moved previous take to: {targetDirectory}");
    }
    
    private void RotateCurrentTakeByRename()
    {
        // Nothing to rotate?
        if (!Directory.Exists(_currentTakeDir) ||
            !Directory.EnumerateFileSystemEntries(_currentTakeDir).Any())
            return;

        // Find next take_N
        int n = 1;
        string takeDir;
        do takeDir = Path.Combine(_baseDirectory, $"take_{n++}");
        while (Directory.Exists(takeDir));

        try
        {
            // Single metadata op if same volume
            Directory.Move(_currentTakeDir, takeDir);

            // Recreate fresh current_take
            Directory.CreateDirectory(_currentTakeDir);

            GD.Print($"Rotated current_take -> {takeDir}");
        }
        catch (IOException ioex)
        {
            // Fallback: cross-volume or other edge case
            GD.PushWarning($"Fast rotate failed ({ioex.Message}). Falling back to slow per-file move...");
            MovePreviousTakeToNumberedDirectory(); // your old per-file method as fallback
            EnsureDir(_currentTakeDir);
        }
        catch (Exception ex)
        {
            GD.PushError($"Rotate failed: {ex.Message}");
            // Ensure current_take exists so recording can continue
            EnsureDir(_currentTakeDir);
        }
    }

    private void ConfigureOutputDirs()
    {
        _baseDirectory   = Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "png2", _sceneName);
        _currentTakeDir  = SceneDirectory;
        EnsureDir(_baseDirectory);
        EnsureDir(_currentTakeDir);

        if (Mode == SaveMode.FreshDirectory)
        {
            RotateCurrentTakeByRename();
            EnsureDir(_currentTakeDir);
            _activeOutputDir = _currentTakeDir;
        }
        else // RepairDirectory
        {
            if (string.IsNullOrWhiteSpace(RepairDirectory))
            {
                _activeOutputDir = _currentTakeDir; // default to current_take
            }
            else
            {
                _activeOutputDir = Path.IsPathRooted(RepairDirectory)
                    ? RepairDirectory
                    : Path.Combine(_baseDirectory, RepairDirectory);
            }
            EnsureDir(_activeOutputDir);
        }

        GD.Print($"Recording to: {_activeOutputDir}");
    }
    
    private bool ValidateRepairResolution()
    {
        if (!Directory.Exists(_activeOutputDir)) return true;

        // Find any existing png (prefer the first frame if present)
        string first = Path.Combine(_activeOutputDir, $"{StartFrame:D06}.png");
        string samplePath = File.Exists(first)
            ? first
            : Directory.EnumerateFiles(_activeOutputDir, "*.png").OrderBy(f => f).FirstOrDefault();

        if (string.IsNullOrEmpty(samplePath)) return true; // nothing saved yet

        // Current intended capture size
        var vp = TargetViewport ?? GetViewport();
        var vpSize = vp.GetVisibleRect().Size; // Vector2I

        // Load existing frame’s size
        var img = Image.LoadFromFile(samplePath);
        if (img == null)
        {
            GD.PushWarning($"Resolution check: couldn’t load sample frame: {samplePath}. Continuing.");
            return true;
        }
        var w = img.GetWidth();
        var h = img.GetHeight();

        if (w != vpSize.X || h != vpSize.Y)
        {
            GD.PrintErr(
                $"SceneRecorder: Resolution mismatch in RepairDirectory.\n" +
                $" Existing frames: {w}x{h}\n Intended capture: {vpSize.X}x{vpSize.Y}\n" +
                $" Folder: {_activeOutputDir}\n Aborting to avoid corrupt mixed sizes."
            );
            return false;
        }
        return true;
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;

        if (!_record)
            return;

        if (string.IsNullOrEmpty(_sceneName))
        {
            GD.PrintErr("Recording is enabled, but scene name is empty.");
            return;
        }

        ConfigureOutputDirs();
        
        if (Mode == SaveMode.RepairDirectory && !ValidateRepairResolution())
        {
            GetTree().Quit();
            return;
        }

        if (_transparentBackground)
        {
            var vp = TargetViewport ?? GetViewport();
            vp.TransparentBg = true;
            RenderingServer.SetDefaultClearColor(new Color(0, 0, 0, 0));
            var env = vp.GetWorld3D()?.Environment;
            if (env != null) {
                env.BackgroundMode = Godot.Environment.BGMode.Color;
                env.BackgroundColor = new Color(0, 0, 0, 0);
                env.Sky = null;
            }
        }

        // Hide Sequence controller if present (kept from your original)
        var sequenceController = GetParent()?.GetChildren()
            .OfType<StateChangeSequencePlayerController>()
            .FirstOrDefault();
        if (sequenceController != null)
            sequenceController.Visible = false;

        _frame = StartFrame;
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) return;
        if (!_record) return;

        // Stop at EndFrame if set (inclusive)
        if (EndFrame >= 0 && _frame > EndFrame)
        {
            if (_quitWhenFinished) GetTree().Quit();
            SetProcess(false);
            return;
        }
        
        var filename = $"image{_frame:D08}.png";
        var path = Path.Combine(_activeOutputDir, filename);

        // In Repair mode, skip existing frames to go fast.
        var shouldWrite = Mode == SaveMode.FreshDirectory || !File.Exists(path);

        if (shouldWrite)
        {
            // Make sure the frame has been drawn this tick
            RenderingServer.ForceDraw(false);

            var vp = TargetViewport ?? GetViewport();
            var img = vp.GetTexture().GetImage();
            // Guard: some platforms require flipping; if needed, uncomment:
            // img.FlipY();
            var ok = img.SavePng(path);
            if (ok != Error.Ok)
                GD.PushWarning($"Failed to save {path}: {ok}");
        }

        _frame++;
    }

    // Call this when the animation/sequence is complete
    public void OnSequenceComplete()
    {
        if (_record && _quitWhenFinished)
        {
            GD.Print("Sequence complete, quitting...");
            GetTree().Quit();
        }
    }
}
