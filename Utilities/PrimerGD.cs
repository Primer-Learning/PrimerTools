using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Godot;

namespace PrimerTools;

public static class PrimerGD
{
    public static void PrintWithStackTrace(params object[] what)
    {
        GD.Print(AppendStackTrace(what));
    }
    public static void PushWarningWithStackTrace(params object[] what)
    {
        GD.PushWarning(AppendStackTrace(what));
    }
    public static void PrintErrorWithStackTrace(params object[] what)
    {
        GD.PrintErr(AppendStackTrace(what));
    }
    
    private static object[] AppendStackTrace(params object[] what)
    {
        var whatWithStackTrace = new object[what.Length + 1];
        what.CopyTo(whatWithStackTrace, 0);
        
        var stackTrace = new System.Diagnostics.StackTrace(true);
        whatWithStackTrace[what.Length] = "\n" + stackTrace;
        
        return whatWithStackTrace;
    }
}