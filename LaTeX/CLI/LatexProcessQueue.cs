using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace PrimerTools.LaTeX;

public static class LatexProcessQueue
{
    private static readonly Queue<(TaskCompletionSource<string> tcs, Func<Task<string>>
taskFactory, string description)> PendingTasks = new();
    private static readonly List<Task> ActiveTasks = new();
    private static readonly object LockObject = new();
    private static int _totalQueued = 0;
    private static int _totalCompleted = 0;
    private static bool _isProcessing = false;

    // Configurable limit
    public const int MaxConcurrentProcesses = 5;

    public static Task<string> EnqueueAsync(Func<Task<string>> taskFactory, string description)
    {
        var tcs = new TaskCompletionSource<string>();

        lock (LockObject)
        {
            PendingTasks.Enqueue((tcs, taskFactory, description));
            _totalQueued++;
            GD.Print($"[LatexQueue] Added: {description} (Queue size: {PendingTasks.Count}, Total queued: {_totalQueued})");

            if (!_isProcessing)
            {
                _isProcessing = true;
                _ = ProcessQueueAsync();
            }
        }

        return tcs.Task;
    }

    private static async Task ProcessQueueAsync()
    {
        GD.Print("[LatexQueue] ProcessQueueAsync started");

        while (true)
        {
            (TaskCompletionSource<string> tcs, Func<Task<string>> taskFactory, string description)
nextTask;

            lock (LockObject)
            {
                // Clean up completed tasks
                ActiveTasks.RemoveAll(t => t.IsCompleted);

                // Check if we can start a new task
                if (ActiveTasks.Count >= MaxConcurrentProcesses || PendingTasks.Count == 0)
                {
                    if (PendingTasks.Count == 0 && ActiveTasks.Count == 0)
                    {
                        _isProcessing = false;
                        GD.Print("[LatexQueue] Queue empty, processor stopping");
                        return;
                    }

                    // Wait a bit before checking again
                    _ = Task.Delay(100).ContinueWith(_ => ProcessQueueAsync());
                    return;
                }

                nextTask = PendingTasks.Dequeue();
                GD.Print($"[LatexQueue] Dequeued: {nextTask.description} (Active: {ActiveTasks.Count + 1}/{MaxConcurrentProcesses})");
            }

            // Start the task
            var task = ProcessTaskAsync(nextTask.tcs, nextTask.taskFactory, nextTask.description);

            lock (LockObject)
            {
                ActiveTasks.Add(task);
            }
        }
    }

    private static async Task ProcessTaskAsync(TaskCompletionSource<string> tcs, Func<Task<string>>
taskFactory, string description)
    {
        try
        {
            GD.Print($"[LatexQueue] Starting: {description}");
            var result = await taskFactory();
            tcs.SetResult(result);

            lock (LockObject)
            {
                _totalCompleted++;
                GD.Print($"[LatexQueue] Completed: {description} (Progress: {_totalCompleted}/{_totalQueued})");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LatexQueue] Failed: {description} - {ex.Message}");
            GD.PrintErr($"[LatexQueue] Stack trace: {ex.StackTrace}");
            tcs.SetException(ex);
        }
    }
}
