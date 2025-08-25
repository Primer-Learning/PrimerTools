using Godot;

public abstract partial class StateChangeSequence : Node3D
{
    public readonly CompositeStateChange RootComposite = new();
    public abstract void Define();
    
    protected void AddStateChangeWithDelay(IStateChange stateChange, double delay = 0)
    {
        RootComposite.AddStateChangeWithDelay(stateChange, delay);
    }
    
    protected void AddStateChangeWithDelay(IStateChange stateChange, int delayMinutes, float delaySeconds)
    {
        RootComposite.AddStateChangeWithDelay(stateChange, delayMinutes, delaySeconds);
    }
    
    protected void AddStateChangeWithDelay(IStateChange stateChange, int delayMinutes, int delaySeconds, int delayFrames)
    {
        RootComposite.AddStateChangeWithDelay(stateChange, delayMinutes, delaySeconds, delayFrames);
    }
    
    protected void AddStateChange(IStateChange stateChange, double absoluteTime = -1)
    {
        RootComposite.AddStateChange(stateChange, absoluteTime);
    }
    
    protected void AddStateChange(IStateChange stateChange, int minutes, float seconds)
    {
        RootComposite.AddStateChange(stateChange, minutes, seconds);
    }
    
    protected void AddStateChange(IStateChange stateChange, int minutes, int seconds, int frames)
    {
        RootComposite.AddStateChange(stateChange, minutes, seconds, frames);
    }
    
    protected void AddStateChangeInParallel(IStateChange stateChange, double delay = 0)
    {
        RootComposite.AddStateChangeInParallel(stateChange, delay);
    }
    
    protected void AddStateChangeInParallel(IStateChange stateChange, int delayMinutes, float delaySeconds)
    {
        RootComposite.AddStateChangeInParallel(stateChange, delayMinutes, delaySeconds);
    }
    
    protected void AddStateChangeInParallel(IStateChange stateChange, int delayMinutes, int delaySeconds, int delayFrames)
    {
        RootComposite.AddStateChangeInParallel(stateChange, delayMinutes, delaySeconds, delayFrames);
    }
}
