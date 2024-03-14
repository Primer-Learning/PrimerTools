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
        latexNode.latex = data.label;
        latexNode.UpdateCharacters();
        this.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
    } 
}