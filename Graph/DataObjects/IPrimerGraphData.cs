using Godot;

namespace PrimerTools.Graph;

public interface IPrimerGraphData
{
    public Animation Transition(double duration);
    public Animation Disappear();
}