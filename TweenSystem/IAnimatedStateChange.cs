using Godot;

public interface IAnimatedStateChange : IStateChange
{
    // TODO: Separate this interface.
    // AppendTweener is needed for anything that expects to play (currently, everything but CompositeStateChange)
    // The other methods are for seeking. So that could be its own, more specific interface.
    void AppendTweener(Tween tween, double elapsedTime = 0);
    void EvaluateAtTime(double elapsedTime);
    void ApplyEndState();
    void RecordStartState();
    void Revert();
}
