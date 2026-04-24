using Godot;
using System.Threading.Tasks;

// Photoshoot: render a subject to PNG. Runs as a [Tool] so the configured
// subject appears in the editor for framing, posing, and animation scrubbing.
// Assign a PhotoshootSubject via the inspector (CharacterPhotoshootSubject,
// ManualPhotoshootSubject, etc.), click Snap Picture to write the PNG at the
// path the subject supplies. There is no runtime capture path — running the
// scene only previews the model. Capture lives in the editor to ensure tweaked
// poses aren't overwritten by AnimationPlayer autoplay.
//
// The instantiated model is owned by the scene root so its subtree is editable.
[Tool]
public partial class Photoshoot : Node
{
    private PhotoshootSubject _subject;
    [Export]
    public PhotoshootSubject Subject
    {
        get => _subject;
        set
        {
            if (_subject != null) _subject.Changed -= OnSubjectChanged;
            _subject = value;
            if (_subject != null) _subject.Changed += OnSubjectChanged;
            if (_modelSlot != null) _subject?.LoadModel(_modelSlot);
        }
    }

    [ExportToolButton("Snap Picture")]
    public Callable SnapPictureButton => Callable.From(() => _ = SnapPicture());

    private SubViewport _viewport;
    private Node3D _modelSlot;

    public override void _Ready()
    {
        _viewport = GetNode<SubViewport>("%SubViewport");
        _modelSlot = GetNode<Node3D>("%ModelSlot");
        _subject?.LoadModel(_modelSlot);
    }

    private void OnSubjectChanged()
    {
        if (_modelSlot != null) _subject?.LoadModel(_modelSlot);
    }

    private async Task SnapPicture()
    {
        if (_viewport == null) return;
        if (_subject == null)
        {
            GD.PrintErr("[photoshoot] No Subject assigned");
            return;
        }
        var savePath = _subject.GetSavePath();
        if (string.IsNullOrEmpty(savePath)) return;

        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var img = _viewport.GetTexture().GetImage();
        var absPath = ProjectSettings.GlobalizePath(savePath);
        var dir = System.IO.Path.GetDirectoryName(absPath);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
        var err = img.SavePng(absPath);
        GD.Print($"[photoshoot] save {absPath} => {err}");
    }
}
