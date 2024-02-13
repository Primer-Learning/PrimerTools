using Godot;

namespace PrimerTools.Graph;

public interface IPrimerGraphData
{
    public Animation Transition();
    public Animation ShrinkToEnd();
}