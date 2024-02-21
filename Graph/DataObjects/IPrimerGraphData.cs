using Godot;

namespace PrimerTools.Graph;

public interface IPrimerGraphData
{
    public Animation Transition(float duration);
    public Animation ShrinkToEnd();
}