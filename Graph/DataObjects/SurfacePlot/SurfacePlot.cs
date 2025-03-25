using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimerTools.Utilities;

namespace PrimerTools.Graph;

[Tool]
public partial class SurfacePlot : MeshInstance3D, IPrimerGraphData
{
    #region Data space transform
    public delegate Vector3 Transformation(Vector3 inputPoint);
    public Transformation TransformPointFromDataSpaceToPositionSpace = point => point;
    #endregion

    #region Data
    private Vector3[,] _data;
    public delegate Vector3[,] DataFetch();
    public DataFetch DataFetchMethod = () =>
    {
        PrimerGD.PrintWithStackTrace("Data fetch method not assigned. Returning empty array.");
        return new Vector3[0, 0];
    };

    public void FetchData()
    {
        _data = DataFetchMethod();
    }
    
    public void SetData(Vector3[,] heightData)
    {
        _data = heightData;
    }

    public void SetDataWithHeightFunction(Func<float, float, float> heightFunction,
        float minX, float maxX, int xPoints,
        float minZ, float maxZ, int zPoints)
    {
        _data = new Vector3[xPoints, zPoints];

        var xStep = (maxX - minX) / (xPoints - 1);
        var zStep = (maxZ - minZ) / (zPoints - 1);

        for (var i = 0; i < xPoints; i++)
        {
            var x = minX + i * xStep;
            for (var j = 0; j < zPoints; j++)
            {
                var z = minZ + j * zStep;
                _data[i, j] = new Vector3(x, heightFunction(x, z), z);
            }
        }
    }

    
    #endregion

    // Implement IPrimerGraphData interface methods
    public Animation Transition(double duration = AnimationUtilities.DefaultDuration)
    {
        // For now, just create the mesh and return a default animation
        CreateMesh();
        return new Animation();
    }

    public Tween TweenTransition(double duration = AnimationUtilities.DefaultDuration)
    {
        // For now, just create the mesh and return a default tween
        CreateMesh();
        return CreateTween();
    }

    public Animation Disappear()
    {
        // For now, return an empty animation
        return new Animation();
    }

    private void CreateMesh()
    {
        if (_data == null || _data.GetLength(0) == 0 || _data.GetLength(1) == 0)
        {
            GD.Print("No height data available to create mesh.");
            return;
        }

        var width = _data.GetLength(0);
        var depth = _data.GetLength(1);

        var vertices = new List<Vector3>();
        var indices = new List<int>();

        // Create vertices
        for (var x = 0; x < width; x++)
        {
            for (var z = 0; z < depth; z++)
            {
                // Create a vertex at this position
                vertices.Add(TransformPointFromDataSpaceToPositionSpace(_data[x, z]));
            }
        }

        // Create triangles (two per grid cell)
        for (var x = 0; x < width - 1; x++)
        {
            for (var z = 0; z < depth - 1; z++)
            {
                var topLeft = x * depth + z;
                var topRight = topLeft + 1;
                var bottomLeft = (x + 1) * depth + z;
                var bottomRight = bottomLeft + 1;

                // First triangle (top-left, bottom-left, bottom-right)
                indices.Add(topLeft);
                indices.Add(bottomLeft);
                indices.Add(bottomRight);

                // Second triangle (top-left, bottom-right, top-right)
                indices.Add(topLeft);
                indices.Add(bottomRight);
                indices.Add(topRight);
            }
        }

        // Create the mesh
        var arrayMesh = new ArrayMesh();
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        
        var normals = CalculateNormals(vertices, indices);
        var vertexArray = vertices.ToArray();
        var indexArray = indices.ToArray();
        MeshUtilities.MakeDoubleSided(ref vertexArray, ref indexArray, ref normals);
        arrays[(int)Mesh.ArrayType.Vertex] = vertexArray;
        arrays[(int)Mesh.ArrayType.Index] = indexArray;
        arrays[(int)Mesh.ArrayType.Normal] = normals;

        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        Mesh = arrayMesh;
    }
    
    private Vector3[] CalculateNormals(List<Vector3> vertices, List<int> indices)
    {
        var normals = new Vector3[vertices.Count];

        // Initialize normals array
        for (var i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.Zero;
        }

        // Calculate normals for each triangle and add to the vertices
        for (var i = 0; i < indices.Count; i += 3)
        {
            var index1 = indices[i];
            var index2 = indices[i + 1];
            var index3 = indices[i + 2];

            var side1 = vertices[index2] - vertices[index1];
            var side2 = vertices[index3] - vertices[index1];
            var normal = side2.Cross(side1).Normalized();

            // Add the normal to each vertex of the triangle
            normals[index1] += normal;
            normals[index2] += normal;
            normals[index3] += normal;
        }

        // Normalize all normals
        for (var i = 0; i < normals.Length; i++)
        {
            normals[i] = normals[i].Normalized();
        }

        return normals;
    }
}