using System;
using Godot;

public class MethodTriggerStateChange : IAnimatedStateChange
{
    private readonly Action _callback;
    private readonly string _name;
    private bool _hasTriggered = false;

    public string Name => _name;
    public double Duration => 0; // Instant trigger

    public MethodTriggerStateChange(Action callback, string name = null)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _name = name ?? "MethodTrigger";
    }

    public void SetTriggered()
    {
        _hasTriggered = true;
    }
    public void Execute()
    {
        if (_hasTriggered) return;
        _callback.Invoke();
        _hasTriggered = true;
    }
    
    public IStateChange WithDuration(double duration)
    {
        // Method triggers are instant
        return this;
    }

    // Seeking-related methods are empty.
    // See TODO in IAnimatedStateChange
    public void EvaluateAtTime(double elapsedTime) {}

    public void ApplyEndState() {}

    public void RecordStartState() {}

    public void Revert()
    {
        _hasTriggered = false;
    }

}