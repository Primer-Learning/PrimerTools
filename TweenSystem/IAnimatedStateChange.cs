using Godot;

public interface IAnimatedStateChange : IStateChange
{
    void AppendTweener(Tween tween, double elapsedTime = 0);
    void EvaluateAtTime(double elapsedTime);
    void ApplyEndState();
    void RecordStartState();
    void Revert();
}
