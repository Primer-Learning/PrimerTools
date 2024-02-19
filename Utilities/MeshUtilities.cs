using System.Collections.Generic;
using Godot;

namespace PrimerTools.Utilities;

public static class MeshUtilities
{
    // This is for triangle-type meshes in index mode 
    public static void AddTriangle(this List<int> triangles, int a, int b, int c, bool flip = false)
    {
        if (flip)
        {
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(b);
            return;
        }
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }
}