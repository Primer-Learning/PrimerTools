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
            ReloadModel();
        }
    }

    [ExportToolButton("Snap Picture")]
    public Callable SnapPictureButton => Callable.From(() => _ = SnapPicture());

    // Lazy accessors. The node references are re-resolved from %-unique-names
    // on demand because [Tool] Photoshoot can receive inspector edits before
    // _Ready (during deserialization) and after hot-reload (which swaps the C#
    // instance but does not re-fire _Ready), leaving stale/null cached fields.
    private SubViewport _viewport;
    private SubViewport Viewport
    {
        get
        {
            if (_viewport == null && IsInsideTree())
                _viewport = GetNodeOrNull<SubViewport>("%SubViewport");
            return _viewport;
        }
    }

    private Node3D _modelSlot;
    private Node3D ModelSlot
    {
        get
        {
            if (_modelSlot == null && IsInsideTree())
                _modelSlot = GetNodeOrNull<Node3D>("%ModelSlot");
            return _modelSlot;
        }
    }

    public override void _Ready()
    {
        ReloadModel();
    }

    private void OnSubjectChanged()
    {
        ReloadModel();
    }

    private void ReloadModel()
    {
        var slot = ModelSlot;
        if (slot == null || _subject == null) return;
        _subject.LoadModel(slot);
        ClaimOwnership(slot);
    }

    // Give every loaded node (and its descendants) Owner = edited scene root so
    // the subtree is visible and editable in the Scene dock while framing the
    // shot. Editor-only — outside the editor, Owner is meaningless for behavior.
    //
    // Note: saving the scene while a model is loaded will persist these nodes
    // into the .tscn. Reloading (re-open / re-assign Subject) wipes them again,
    // so in practice this just means "don't Ctrl-S while a subject is loaded."
    private void ClaimOwnership(Node3D slot)
    {
        if (!Engine.IsEditorHint()) return;
        var root = GetTree()?.EditedSceneRoot;
        if (root == null) return;
        foreach (Node child in slot.GetChildren())
            SetOwnerRecursive(child, root);
    }

    private static void SetOwnerRecursive(Node node, Node owner)
    {
        node.Owner = owner;
        foreach (Node child in node.GetChildren())
            SetOwnerRecursive(child, owner);
    }

    private async Task SnapPicture()
    {
        var viewport = Viewport;
        if (viewport == null) return;
        if (_subject == null)
        {
            GD.PrintErr("[photoshoot] No Subject assigned");
            return;
        }
        var savePath = _subject.GetSavePath();
        if (string.IsNullOrEmpty(savePath)) return;

        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var img = viewport.GetTexture().GetImage();
        var absPath = ProjectSettings.GlobalizePath(savePath);
        var dir = System.IO.Path.GetDirectoryName(absPath);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
        var err = img.SavePng(absPath);
        GD.Print($"[photoshoot] save {absPath} => {err}");
    }
}
