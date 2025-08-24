using Godot;

public abstract partial class StateChangeSequence : Node3D
{
    public readonly CompositeStateChange RootComposite = new();
    public abstract void Define();
    
    protected void AddStateChange(IStateChange stateChange, double delay = 0)
    {
        RootComposite.AddStateChange(stateChange, delay);
    }
    
    protected void AddStateChange(IStateChange stateChange, int delayMinutes, float delaySeconds)
    {
        RootComposite.AddStateChange(stateChange, delayMinutes, delaySeconds);
    }
    
    protected void AddStateChange(IStateChange stateChange, int delayMinutes, int delaySeconds, int delayFrames)
    {
        RootComposite.AddStateChange(stateChange, delayMinutes, delaySeconds, delayFrames);
    }
    
    protected void AddStateChangeAt(IStateChange stateChange, double absoluteTime)
    {
        RootComposite.AddStateChangeAt(stateChange, absoluteTime);
    }
    
    protected void AddStateChangeAt(IStateChange stateChange, int minutes, float seconds)
    {
        RootComposite.AddStateChangeAt(stateChange, minutes, seconds);
    }
    
    protected void AddStateChangeAt(IStateChange stateChange, int minutes, int seconds, int frames)
    {
        RootComposite.AddStateChangeAt(stateChange, minutes, seconds, frames);
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
