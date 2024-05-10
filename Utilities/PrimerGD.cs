using System.Collections.Generic;
using Godot;

namespace PrimerTools;

public static class PrimerGD
{
    public static void PrintWithStackTrace(params object[] what)
    {
        var stackTrace = new System.Diagnostics.StackTrace(true);
        var whatWithStackTrace = new List<object>();
        whatWithStackTrace.AddRange(what);
        whatWithStackTrace.Add("\n" + stackTrace);
        GD.Print(whatWithStackTrace.ToArray());
    }
}