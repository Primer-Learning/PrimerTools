using Godot;

// Strategy for Photoshoot: what to put in the model slot and where to save the PNG.
// Subclasses are expected to be [GlobalClass] so they can be picked from the
// Godot resource-new menu and edited inline on the Photoshoot node.
[Tool]
[GlobalClass]
public abstract partial class PhotoshootSubject : Resource
{
    // Populate modelSlot with the subject. Implementations may inspect existing
    // children to preserve editor tweaks when the desired model is already loaded,
    // or be a no-op for manual workflows where the user places the model by hand.
    public abstract void LoadModel(Node3D modelSlot);

    // res:// or absolute path for Photoshoot.SnapPicture to write the PNG to.
    // Returning null/empty aborts the capture with an error log.
    public abstract string GetSavePath();
}
