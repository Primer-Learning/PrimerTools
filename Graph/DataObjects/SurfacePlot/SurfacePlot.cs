using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimerTools.TweenSystem;
using PrimerTools.Utilities;

namespace PrimerTools.Graph;

[Tool]
public partial class SurfacePlot : MeshInstance3D, IPrimerGraphData
{
    #region Data space transform
    public delegate Vector3 Transformation(Vector3 inputPoint);
    public Transformation TransformPointFromDataSpaceToPositionSpace = point => point;
    #endregion

    #region Appearance
    private StandardMaterial3D _materialCache;
    private StandardMaterial3D Material
    {
        get => _materialCache ??= new StandardMaterial3D();
        set => _materialCache = value;
    }
    
    public void SetColor(Color color)
    {
        Material.AlbedoColor = color;
    }
    #endregion
    
    #region Data
    private List<Vector3[,]> _surfaceStates = new List<Vector3[,]>();
    private float _stateProgress = 0f;
    
    public enum SweepMode { XAxis, ZAxis, Radial, Diagonal }
    private SweepMode _currentSweepMode = SweepMode.XAxis;
    
    [Export] public float StateProgress
    {
        get => _stateProgress;
        set
        {
            _stateProgress = value;
            UpdateMeshForCurrentProgress();
        }
    }
    
    public delegate Vector3[,] DataFetch();
    public DataFetch DataFetchMethod = () =>
    {
        PrimerGD.PrintWithStackTrace("Data fetch method not assigned. Returning empty array.");
        return new Vector3[0, 0];
    };

    public void FetchData()
    {
        var data = DataFetchMethod();
        AddState(data);
    }
    
    public void SetData(Vector3[,] heightData)
    {
        AddState(heightData);
    }
    
    public void AddState(Vector3[,] stateData)
    {
        _surfaceStates.Add(stateData);
    }

    public void SetDataWithHeightFunction(Func<float, float, float> heightFunction,
        float minX, float maxX, int xPoints,
        float minZ, float maxZ, int zPoints)
    {
        AddStateWithHeightFunction(heightFunction, minX, maxX, xPoints, minZ, maxZ, zPoints);
    }
    
    public void AddStateWithHeightFunction(Func<float, float, float> heightFunction,
        float minX, float maxX, int xPoints,
        float minZ, float maxZ, int zPoints)
    {
        var data = new Vector3[xPoints, zPoints];

        var xStep = (maxX - minX) / (xPoints - 1);
        var zStep = (maxZ - minZ) / (zPoints - 1);

        for (var i = 0; i < xPoints; i++)
        {
            var x = minX + i * xStep;
            for (var j = 0; j < zPoints; j++)
            {
                var z = minZ + j * zStep;
                data[i, j] = new Vector3(x, heightFunction(x, z), z);
            }
        }
        
        AddState(data);
    }

    
    #endregion

    // Implement IPrimerGraphData interface methods
    public Animation Transition(double duration = AnimationUtilities.DefaultDuration)
    {
        // For now, just create the mesh and return a default animation
        CreateMesh();
        return new Animation();
    }

    public IStateChange TransitionStateChange(double duration = Node3DStateChangeExtensions.DefaultDuration)
    {
        // Default to TransitionAppear for backward compatibility
        return TransitionAppear(duration);
    }
    
    public IStateChange TransitionAppear(double duration = Node3DStateChangeExtensions.DefaultDuration, SweepMode mode = SweepMode.XAxis)
    {
        if (_surfaceStates.Count == 0) return null;
        
        _currentSweepMode = mode;
        StateProgress = 0;
        
        return new PropertyStateChange(this, "StateProgress", 1f)
            .WithDuration(duration);
    }
    
    public IStateChange TransitionToNextState(double duration = Node3DStateChangeExtensions.DefaultDuration)
    {
        if (_surfaceStates.Count == 0) return null;
        
        // Don't go beyond the last state
        // if (targetProgress > _surfaceStates.Count - 1)
        //     targetProgress = _surfaceStates.Count - 1;
            
        return new PropertyStateChange(this, "StateProgress", _surfaceStates.Count)
            .WithDuration(duration);
    }
    
    public IStateChange TransitionToState(int stateIndex, double duration = Node3DStateChangeExtensions.DefaultDuration)
    {
        if (stateIndex < 0 || stateIndex >= _surfaceStates.Count) return null;
        
        return new PropertyStateChange(this, "StateProgress", (float)stateIndex)
            .WithDuration(duration);
    }
    
    private void UpdateMeshForCurrentProgress()
    {
        if (_surfaceStates.Count == 0)
        {
            Mesh = new ArrayMesh();
            return;
        }

        // Handle initial appearance (0 to 1 progress)
        if (_stateProgress < 1f)
        {
            CreateSweptMesh(_stateProgress);
            return;
        }

        // For progress >= 1, we're showing states or transitioning between them
        // Progress 1.0 = state 0, Progress 2.0 = state 1, etc.
        var adjustedProgress = _stateProgress - 1f;
        var stateIndex = Mathf.FloorToInt(adjustedProgress);
        var transitionProgress = adjustedProgress - stateIndex;

        // Clamp to valid state range
        if (stateIndex >= _surfaceStates.Count - 1)
        {
            // We're at or beyond the last state
            CreateMeshFromData(_surfaceStates[_surfaceStates.Count - 1]);
            return;
        }

        // If we're exactly on a state (no fractional part), show it
        if (Mathf.IsEqualApprox(transitionProgress, 0f))
        {
            CreateMeshFromData(_surfaceStates[stateIndex]);
            return;
        }

        // Interpolate between two states
        var fromState = _surfaceStates[stateIndex];
        var toState = _surfaceStates[stateIndex + 1];
        CreateInterpolatedMesh(fromState, toState, transitionProgress);
    }
    
    private void CreateSweptMesh(float progress)
    {
        if (_surfaceStates.Count == 0 || progress <= 0)
        {
            Mesh = new ArrayMesh();
            return;
        }
        
        var data = _surfaceStates[0];
        var width = data.GetLength(0);
        var depth = data.GetLength(1);
        
        // Calculate visible width based on progress
        var visibleWidth = Mathf.Max(1, (int)(width * progress));
        var edgeFraction = (width * progress) % 1;
        
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        
        // Create vertices up to visible width
        for (var x = 0; x < visibleWidth; x++)
        {
            for (var z = 0; z < depth; z++)
            {
                vertices.Add(TransformPointFromDataSpaceToPositionSpace(data[x, z]));
            }
        }
        
        // Add interpolated edge vertices if needed
        if (edgeFraction > 0 && visibleWidth < width)
        {
            for (var z = 0; z < depth; z++)
            {
                var currentPoint = data[visibleWidth - 1, z];
                var nextPoint = data[visibleWidth, z];
                var interpolatedPoint = currentPoint.Lerp(nextPoint, edgeFraction);
                vertices.Add(TransformPointFromDataSpaceToPositionSpace(interpolatedPoint));
            }
        }
        
        // Create triangles
        var actualWidth = edgeFraction > 0 ? visibleWidth + 1 : visibleWidth;
        for (var x = 0; x < actualWidth - 1; x++)
        {
            for (var z = 0; z < depth - 1; z++)
            {
                var topLeft = x * depth + z;
                var topRight = topLeft + 1;
                var bottomLeft = (x + 1) * depth + z;
                var bottomRight = bottomLeft + 1;
                
                indices.Add(topLeft);
                indices.Add(bottomLeft);
                indices.Add(bottomRight);
                
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
        Mesh.SurfaceSetMaterial(0, Material);
    }
    
    private void CreateInterpolatedMesh(Vector3[,] fromData, Vector3[,] toData, float progress)
    {
        var width = Math.Min(fromData.GetLength(0), toData.GetLength(0));
        var depth = Math.Min(fromData.GetLength(1), toData.GetLength(1));
        
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        
        // Interpolate vertices
        for (var x = 0; x < width; x++)
        {
            for (var z = 0; z < depth; z++)
            {
                var fromPoint = fromData[x, z];
                var toPoint = toData[x, z];
                var interpolatedPoint = fromPoint.Lerp(toPoint, progress);
                vertices.Add(TransformPointFromDataSpaceToPositionSpace(interpolatedPoint));
            }
        }
        
        // Create triangles
        for (var x = 0; x < width - 1; x++)
        {
            for (var z = 0; z < depth - 1; z++)
            {
                var topLeft = x * depth + z;
                var topRight = topLeft + 1;
                var bottomLeft = (x + 1) * depth + z;
                var bottomRight = bottomLeft + 1;
                
                indices.Add(topLeft);
                indices.Add(bottomLeft);
                indices.Add(bottomRight);
                
                indices.Add(topLeft);
                indices.Add(bottomRight);
                indices.Add(topRight);
            }
        }
        
        BuildMeshFromVerticesAndIndices(vertices, indices);
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
        if (_surfaceStates.Count == 0)
        {
            GD.Print("No height data available to create mesh.");
            return;
        }
        
        CreateMeshFromData(_surfaceStates[_surfaceStates.Count - 1]);
    }
    
    private void CreateMeshFromData(Vector3[,] data)
    {
        if (data == null || data.GetLength(0) == 0 || data.GetLength(1) == 0)
        {
            Mesh = new ArrayMesh();
            return;
        }

        var width = data.GetLength(0);
        var depth = data.GetLength(1);

        var vertices = new List<Vector3>();
        var indices = new List<int>();

        // Create vertices
        for (var x = 0; x < width; x++)
        {
            for (var z = 0; z < depth; z++)
            {
                vertices.Add(TransformPointFromDataSpaceToPositionSpace(data[x, z]));
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

        BuildMeshFromVerticesAndIndices(vertices, indices);
    }
    
    private void BuildMeshFromVerticesAndIndices(List<Vector3> vertices, List<int> indices)
    {
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
        Mesh.SurfaceSetMaterial(0, Material);
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
