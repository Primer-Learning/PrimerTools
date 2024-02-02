using Godot;
using PrimerTools.LaTeX;

namespace PrimerTools.Graph;

[Tool]
public partial class AxisTic : Node3D
{
    public Axis.TicData data;
    private LatexNode latexNode => GetNode<LatexNode>("LatexNode");

    // Handling a non-existent label, but this should be unnecessary because tics are always instantiated
    // public override void _EnterTree()
    // {
    //     if (latexNode == null)
    //     {
    //         var newLatexNode = new LatexNode();
    //         newLatexNode.Name = "LatexNode";
    //         AddChild(latexNode);
    //     }
    // }

    public void SetLabel()
    {
        latexNode.latex = data.label;
        latexNode.UpdateCharacters();
    } 
}