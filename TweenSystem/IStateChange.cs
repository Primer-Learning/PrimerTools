// Minimal interface for anything that can be added to a sequence
public interface IStateChange
{
    string Name { get; }
    double Duration { get; }
}
