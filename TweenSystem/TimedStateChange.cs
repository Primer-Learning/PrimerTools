namespace GladiatorManager.addons.PrimerTools.TweenSystem;

public class TimedStateChange
{
    public IAnimatedStateChange StateChange { get; }
    public double StartTime { get; }
    public double EndTime => StartTime + StateChange.Duration;
    
    public TimedStateChange(IAnimatedStateChange stateChange, double startTime)
    {
        StateChange = stateChange;
        StartTime = startTime;
    }
}
