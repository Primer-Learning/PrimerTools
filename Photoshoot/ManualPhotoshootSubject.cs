using System;
using Godot;

// Manual / ad-hoc photoshoot: the user places whatever Node3D they want to
// photograph under %ModelSlot by hand. Saves to res://photoshoot_results/
// with a timestamped filename; move/rename the file to its permanent home
// after the shoot.
[Tool]
[GlobalClass]
public partial class ManualPhotoshootSubject : PhotoshootSubject
{
    [Export] public string FileName { get; set; } = "snap";

    public override void LoadModel(Node3D modelSlot) { }

    public override string GetSavePath()
    {
        var name = string.IsNullOrWhiteSpace(FileName) ? "snap" : FileName;
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"res://photoshoot_results/{name}_{stamp}.png";
    }
}
