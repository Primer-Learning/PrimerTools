using Godot;

public abstract partial class StateChangeSequence : Node
{
    public readonly CompositeStateChange RootComposite = new();
    public abstract void Define();
    
    protected void AddStateChange(IStateChange stateChange, double delay = 0)
    {
        RootComposite.AddStateChange(stateChange, delay);
    }
    protected void AddStateChangeAt(IStateChange stateChange, double absoluteTime)
    {
        RootComposite.AddStateChangeAt(stateChange, absoluteTime);
    }
    protected void AddStateChangeInParallel(IStateChange stateChange, double delay = 0)
    {
        RootComposite.AddStateChangeInParallel(stateChange, delay);
    }
}
