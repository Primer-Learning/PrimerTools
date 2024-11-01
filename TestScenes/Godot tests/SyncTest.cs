using Godot;
using System;
using System.Diagnostics;

public partial class SyncTest : Node
{
    // Configuration
    [Export]
    public double PhysicsDelaySeconds { get; set; } = 1.0;
    
    [Export]
    public double RenderDelaySeconds { get; set; } = 0.0;
    
    [Export]
    public bool LogToFile { get; set; } = true;
    
    // State tracking
    private int physicsFrameCount = 0;
    private int renderFrameCount = 0;
    private Stopwatch physicsStopwatch = new Stopwatch();
    private Stopwatch renderStopwatch = new Stopwatch();
    private DateTime lastPhysicsTime;
    private DateTime lastRenderTime;
    private FileAccess logFile;

    // Busy wait helper
    private void BusyWait(double seconds)
    {
        if (seconds <= 0) return;
        
        long operations = 0;
        var sw = new Stopwatch();
        sw.Start();
        
        // Perform meaningless calculations until enough time has passed
        while (sw.Elapsed.TotalSeconds < seconds)
        {
            operations++;
            
            // Prevent potential compiler optimizations from eliminating the loop
            if (operations % 10000000 == 0)
            {
                GD.Print("", operations); // Empty string concat to force evaluation
            }
        }
    }
    
    public override void _Ready()
    {
        Engine.MaxFps = 60;
        Engine.PhysicsTicksPerSecond = 60;
        Engine.MaxPhysicsStepsPerFrame = 2;
        
        if (LogToFile)
        {
            logFile = FileAccess.Open("res://sync_test_log.txt", FileAccess.ModeFlags.Write);
            logFile.StoreLine("Timestamp,Type,Frame,Duration(ms),TimeSinceLastFrame(ms)");
        }
        
        lastPhysicsTime = DateTime.Now;
        lastRenderTime = DateTime.Now;
        
        GD.Print($"Starting sync test with:");
        GD.Print($"Physics delay: {PhysicsDelaySeconds}s");
        GD.Print($"Render delay: {RenderDelaySeconds}s");
    }
    
    public override void _PhysicsProcess(double delta)
    {
        physicsStopwatch.Restart();
        
        // Simulate heavy computation with busy waiting
        BusyWait(PhysicsDelaySeconds);
        
        physicsStopwatch.Stop();
        
        var now = DateTime.Now;
        var timeSinceLastPhysics = (now - lastPhysicsTime).TotalMilliseconds;
        lastPhysicsTime = now;
        
        string message = $"Physics frame {physicsFrameCount}: " +
                        $"Took {physicsStopwatch.ElapsedMilliseconds}ms, " +
                        $"Time since last physics: {timeSinceLastPhysics:F1}ms";
        GD.Print(message);
        
        if (LogToFile)
        {
            logFile.StoreLine($"{now:yyyy-MM-dd HH:mm:ss.fff},Physics,{physicsFrameCount}," +
                            $"{physicsStopwatch.ElapsedMilliseconds},{timeSinceLastPhysics:F1}");
        }
        
        physicsFrameCount++;
    }
    
    public override void _Process(double delta)
    {
        renderStopwatch.Restart();
        
        // Simulate heavy computation with busy waiting
        BusyWait(RenderDelaySeconds);
        
        renderStopwatch.Stop();
        
        var now = DateTime.Now;
        var timeSinceLastRender = (now - lastRenderTime).TotalMilliseconds;
        lastRenderTime = now;
        
        string message = $"Render frame {renderFrameCount}: " +
                        $"Took {renderStopwatch.ElapsedMilliseconds}ms, " +
                        $"Time since last render: {timeSinceLastRender:F1}ms";
        GD.Print(message);
        
        if (LogToFile)
        {
            logFile.StoreLine($"{now:yyyy-MM-dd HH:mm:ss.fff},Render,{renderFrameCount}," +
                            $"{renderStopwatch.ElapsedMilliseconds},{timeSinceLastRender:F1}");
        }
        
        renderFrameCount++;
    }
    
    public override void _ExitTree()
    {
        if (LogToFile && logFile != null)
        {
            logFile.Close();
        }
    }
}