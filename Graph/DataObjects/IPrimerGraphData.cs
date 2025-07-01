using Godot;

namespace PrimerTools.Graph;

public interface IPrimerGraphData
{
    public void FetchData();
    public Animation Transition(double duration);
    public IStateChange TransitionStateChange(double duration);
    public Tween TweenTransition(double duration);
    public Animation Disappear();
}