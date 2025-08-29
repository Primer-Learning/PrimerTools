using Godot;

public abstract partial class StateChangeSequence : Node3D
{
    [Export] public bool Active = true; 
    public readonly CompositeStateChange RootComposite = new();
    public abstract void Define();
    
    protected void AddStateChange(IStateChange stateChange, double absoluteTime = -1, bool log = false)
    {
        RootComposite.AddStateChange(stateChange, absoluteTime, log);
    }
    
    protected void AddStateChange(IStateChange stateChange, int minutes, float seconds)
    {
        RootComposite.AddStateChange(stateChange, minutes, seconds);
    }
    
    protected void AddStateChange(IStateChange stateChange, int minutes, int seconds, int frames)
    {
        RootComposite.AddStateChange(stateChange, minutes, seconds, frames);
    }
    
    protected void AddStateChangeWithDelay(IStateChange stateChange, double delay = 0)
    {
        RootComposite.AddStateChangeWithDelay(stateChange, delay);
    }
    
    // Removing the ones with minute-scale delays.
    // They are never used and make it more likely to accidentally add a delay instead of an absolute time. 
    // protected void AddStateChangeWithDelay(IStateChange stateChange, int delayMinutes, float delaySeconds)
    // {
    //     RootComposite.AddStateChangeWithDelay(stateChange, delayMinutes, delaySeconds);
    // }
    //
    // protected void AddStateChangeWithDelay(IStateChange stateChange, int delayMinutes, int delaySeconds, int delayFrames)
    // {
    //     RootComposite.AddStateChangeWithDelay(stateChange, delayMinutes, delaySeconds, delayFrames);
    // }
    
    protected void AddStateChangeInParallel(IStateChange stateChange, double delay = 0)
    {
        RootComposite.AddStateChangeInParallel(stateChange, delay);
    }
    
    // protected void AddStateChangeInParallel(IStateChange stateChange, int delayMinutes, float delaySeconds)
    // {
    //     RootComposite.AddStateChangeInParallel(stateChange, delayMinutes, delaySeconds);
    // }
    //
    // protected void AddStateChangeInParallel(IStateChange stateChange, int delayMinutes, int delaySeconds, int delayFrames)
    // {
    //     RootComposite.AddStateChangeInParallel(stateChange, delayMinutes, delaySeconds, delayFrames);
    // }
}
