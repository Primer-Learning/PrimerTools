using Godot;

public interface IAnimatedStateChange : IStateChange
{
    void EvaluateAtTime(double elapsedTime);
    void ApplyEndState();
    void RecordStartState();
    void Revert();
}
