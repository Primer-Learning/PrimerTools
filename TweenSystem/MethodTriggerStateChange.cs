using System;
using Godot;

public class MethodTriggerStateChange : IAnimatedStateChange
{
    private readonly Action _callback;
    private readonly string _name;

    public string Name => _name;
    public double Duration => 0; // Instant trigger

    public MethodTriggerStateChange(Action callback, string name = null)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _name = name ?? "MethodTrigger";
    }

    public void AppendTweener(Tween tween, double elapsedTime = 0)
    {
        // Since this is instant, we only execute if elapsedTime hasn't passed yet
        if (elapsedTime <= 0)
        {
            tween.TweenCallback(Callable.From(_callback));
        }
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

    public void Revert() {}

}