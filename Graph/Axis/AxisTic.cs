using Godot;
using PrimerTools.LaTeX;
using PrimerTools.TweenSystem;

namespace PrimerTools.Graph;

[Tool]
public partial class AxisTic : Node3D
{
    public Axis.TicData Data;
    
    private LatexNode LatexNode => GetNode<LatexNode>("LatexNode");

    public void SetLabel()
    {
        LatexNode.Latex = Data.label;
        LatexNode.UpdateCharacters();
        this.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
    }

    public void SetLabelScale(float scale)
    {
        LatexNode.Scale = Vector3.One * scale;
    }

    public Animation AnimateLabelScale(float scale)
    {
        return LatexNode.ScaleToAnimation(scale);
    }

    public IStateChange AnimateLabelScaleStateChange(float scale)
    {
        return LatexNode.ScaleTo(scale);
    }

    public void SetLabelDistance(float? distance)
    {
        if (distance.HasValue)
        {
            LatexNode.Position = new Vector3(LatexNode.Position.X, -distance.Value, LatexNode.Position.Z);
        }
        // If no distance specified, keep the default position from the scene
    }

    public Animation AnimateLabelDistance(float? distance)
    {
        if (!distance.HasValue)
            return null;
            
        return LatexNode.MoveToAnimation(
            new Vector3(LatexNode.Position.X, -distance.Value, LatexNode.Position.Z)
        );
    }

    public IStateChange AnimateLabelDistanceStateChange(float? distance)
    {
        if (!distance.HasValue)
            return null;
            
        return LatexNode.MoveTo(
            new Vector3(LatexNode.Position.X, -distance.Value, LatexNode.Position.Z)
        );
    }
}
