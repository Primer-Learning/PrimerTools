using System.Collections.Generic;
using Godot;

namespace PrimerTools.Utilities;

public static class MeshUtilities
{
    // This is for triangle-type meshes in index mode 
    public static void AddTriangle(this List<int> indices, int a, int b, int c, bool flip = false)
    {
        if (flip)
        {
            indices.Add(a);
            indices.Add(c);
            indices.Add(b);
            return;
        }
        indices.Add(a);
        indices.Add(b);
        indices.Add(c);
    }
    public static void MakeDoubleSided(ref Vector3[] vertices, ref int[] indices, ref Vector3[] normals)
    {
        // Prepare arrays to contain vertices and triangles for both sides
        // If doubleSided is false, the empty entries won't matter
        var verticesDouble = new Vector3[vertices.Length * 2];
        var trianglesDouble = new int[indices.Length * 2];
        var normalsDouble = new Vector3[normals.Length * 2];
                
        // Fill the vertices and triangles for the first side
        vertices.CopyTo(verticesDouble, 0);
        indices.CopyTo(trianglesDouble, 0);
        normals.CopyTo(normalsDouble, 0);
        
        // Add the copied set of vertices
        vertices.CopyTo(verticesDouble, vertices.Length);
        // Add the copied set of triangles, but reverse the order of the vertices for each one
        // so that the normals point in the opposite direction.
        for (var i = 0; i < indices.Length; i += 3)
        {
            trianglesDouble[i + indices.Length] = indices[i + 2] + vertices.Length;
            trianglesDouble[i + 1 + indices.Length] = indices[i + 1] + vertices.Length;
            trianglesDouble[i + 2 + indices.Length] = indices[i] + vertices.Length;
        }
        // Flip the second half of the normals
        for (var i = 0; i < normals.Length; i++)
        {
            normalsDouble[i + normals.Length] = -normals[i];
        }

        vertices = verticesDouble;
        indices = trianglesDouble;
        normals = normalsDouble;
    }
}