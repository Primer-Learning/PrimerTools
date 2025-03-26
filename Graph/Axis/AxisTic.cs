using Godot;
using PrimerTools.LaTeX;

namespace PrimerTools.Graph;

[Tool]
public partial class AxisTic : Node3D
{
    public Axis.TicData data;
    
    private LatexNode latexNode => GetNode<LatexNode>("LatexNode");

    public void SetLabel()
    {
        latexNode.Latex = data.label;
        latexNode.UpdateCharacters();
        this.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
    }

    public void SetLabelScale(float scale)
    {
        latexNode.Scale = Vector3.One * scale;
    }

    public Animation AnimateLabelScale(float scale)
    {
        return latexNode.ScaleTo(scale);
    }

    public void SetLabelDistance(float? distance)
    {
        if (distance.HasValue)
        {
            latexNode.Position = new Vector3(latexNode.Position.X, -distance.Value, latexNode.Position.Z);
        }
        // If no distance specified, keep the default position from the scene
    }

    public Animation AnimateLabelDistance(float? distance)
    {
        if (!distance.HasValue)
            return null;
            
        return latexNode.MoveTo(
            new Vector3(latexNode.Position.X, -distance.Value, latexNode.Position.Z)
        );
    }
}
