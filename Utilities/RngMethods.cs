using System.Collections.Generic;
using Godot;

namespace PrimerTools.Utilities;

public class RngMethods
{
    public static List<Vector3> RandomizedXZPlanePositions(int count, Vector2 min, Vector2 max, Rng rng = null)
    {
        var posList = new List<Vector3>();
        for (var i = 0; i < count; i++)
        {
            posList.Add(
                new Vector3(
                    rng.RangeFloat(min.X, max.X),
                    0,
                    rng.RangeFloat(min.Y, max.Y)
                )
            );
        }

        return posList;
    }
}