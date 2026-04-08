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
        if (color.A < 1)
        {
            Material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        }
        
        Material.AlbedoColor = color;
    }
    #endregion
    
    #region Data
    private List<Vector3[,]> _surfaceStates = new List<Vector3[,]>();
    private float _stateProgress = 0f;
    
    public enum SweepMode { XAxis, ZAxis, Radial, Diagonal }
    // private SweepMode _currentSweepMode = SweepMode.XAxis;
    
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

    // TODO: Make the min/max/points arguments optional.
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

    #region Mesh Management
    private ArrayMesh _reusableMesh;
    private Rid _meshRid;
    private bool _meshInitialized = false;
    private int _currentWidth;
    private int _currentDepth;
    private byte[] _vertexDataBuffer;
    private byte[] _normalDataBuffer;
    private Vector3[] _normalVectorBuffer;
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
    
    public IStateChange TransitionAppear(double duration = Node3DStateChangeExtensions.DefaultDuration)
    {
        return TransitionToNextState(duration);
    }
    
    public IStateChange TransitionToNextState(double duration = Node3DStateChangeExtensions.DefaultDuration)
    {
        if (_surfaceStates.Count == 0) return null;
        
        GD.Print($"{Name} Transitioning from state {StateProgress} to state {_surfaceStates.Count}");
        
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
            UpdateMeshVertices(_surfaceStates[_surfaceStates.Count - 1]);
            return;
        }

        // If we're exactly on a state (no fractional part), show it
        if (Mathf.IsEqualApprox(transitionProgress, 0f))
        {
            UpdateMeshVertices(_surfaceStates[stateIndex]);
            return;
        }

        // Interpolate between two states
        var fromState = _surfaceStates[stateIndex];
        var toState = _surfaceStates[stateIndex + 1];
        CreateInterpolatedMesh(fromState, toState, transitionProgress);
    }
    
    private void EnsureMeshInitialized(int width, int depth)
    {
        if (_meshInitialized && _currentWidth == width && _currentDepth == depth)
            return;
        
        // Create the mesh structure once with fixed topology
        CreateInitialMesh(width, depth);
        _meshInitialized = true;
        _currentWidth = width;
        _currentDepth = depth;
    }
    
    private void CreateInitialMesh(int width, int depth)
    {
        _reusableMesh ??= new ArrayMesh();
        
        // Clear any existing surfaces
        for (int i = _reusableMesh.GetSurfaceCount() - 1; i >= 0; i--)
            _reusableMesh.ClearSurfaces();
        
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        
        // Create initial vertices (will be updated later)
        var vertices = new Vector3[width * depth];
        if (_surfaceStates.Count > 0)
        {
            var initialData = _surfaceStates[0];
            var index = 0;
            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    vertices[index++] = TransformPointFromDataSpaceToPositionSpace(initialData[x, z]);
                }
            }
        }
        
        var indices = new List<int>();
        
        // Create triangles (topology stays constant)
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
        
        // Initialize with dummy normals (will be updated)
        var normals = new Vector3[width * depth];
        for (int i = 0; i < normals.Length; i++)
            normals[i] = Vector3.Up;
        
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        
        Material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        
        _reusableMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        Mesh = _reusableMesh;
        Mesh.SurfaceSetMaterial(0, Material);
        
        // Cache the RID for direct updates
        _meshRid = _reusableMesh.GetRid();
        
        // Pre-allocate buffers
        _vertexDataBuffer = new byte[width * depth * 12]; // 3 floats * 4 bytes each
        _normalDataBuffer = new byte[width * depth * 12]; // 3 floats * 4 bytes each
        _normalVectorBuffer = new Vector3[width * depth]; // Reusable normal vector array
    }
    
    private void UpdateMeshVertices(Vector3[,] data)
    {
        var width = data.GetLength(0);
        var depth = data.GetLength(1);
        
        EnsureMeshInitialized(width, depth);
        
        // Pack vertices into byte array
        var index = 0;
        for (var x = 0; x < width; x++)
        {
            for (var z = 0; z < depth; z++)
            {
                var vertex = TransformPointFromDataSpaceToPositionSpace(data[x, z]);
                BitConverter.GetBytes(vertex.X).CopyTo(_vertexDataBuffer, index);
                BitConverter.GetBytes(vertex.Y).CopyTo(_vertexDataBuffer, index + 4);
                BitConverter.GetBytes(vertex.Z).CopyTo(_vertexDataBuffer, index + 8);
                index += 12;
            }
        }
        
        // Update vertex positions
        RenderingServer.MeshSurfaceUpdateVertexRegion(_meshRid, 0, 0, _vertexDataBuffer);
        
        // Calculate and update normals
        UpdateNormals(data);
    }
    
    private void UpdateNormals(Vector3[,] data)
    {
        var width = data.GetLength(0);
        var depth = data.GetLength(1);

        // Calculate normals
        var normals = new Vector3[width * depth];
        for (var x = 0; x < width; x++)
        {
            for (var z = 0; z < depth; z++)
            {
                var idx = x * depth + z;
                var current = TransformPointFromDataSpaceToPositionSpace(data[x, z]);

                Vector3 normal = Vector3.Zero;
                int samples = 0;

                // Use one-sided differences at edges, centered differences in interior
                // Calculate tangent vectors based on available neighbors
                Vector3 tangentX = Vector3.Zero;
                Vector3 tangentZ = Vector3.Zero;

                // X direction tangent
                if (x > 0 && x < width - 1)
                {
                    // Centered difference
                    var left = TransformPointFromDataSpaceToPositionSpace(data[x - 1, z]);
                    var right = TransformPointFromDataSpaceToPositionSpace(data[x + 1, z]);
                    tangentX = (right - left) * 0.5f;
                }
                else if (x == 0 && x < width - 1)
                {
                    // Forward difference
                    var right = TransformPointFromDataSpaceToPositionSpace(data[x + 1, z]);
                    tangentX = right - current;
                }
                else if (x == width - 1 && x > 0)
                {
                    // Backward difference
                    var left = TransformPointFromDataSpaceToPositionSpace(data[x - 1, z]);
                    tangentX = current - left;
                }

                // Z direction tangent
                if (z > 0 && z < depth - 1)
                {
                    // Centered difference
                    var front = TransformPointFromDataSpaceToPositionSpace(data[x, z - 1]);
                    var back = TransformPointFromDataSpaceToPositionSpace(data[x, z + 1]);
                    tangentZ = (back - front) * 0.5f;
                }
                else if (z == 0 && z < depth - 1)
                {
                    // Forward difference
                    var back = TransformPointFromDataSpaceToPositionSpace(data[x, z + 1]);
                    tangentZ = back - current;
                }
                else if (z == depth - 1 && z > 0)
                {
                    // Backward difference
                    var front = TransformPointFromDataSpaceToPositionSpace(data[x, z - 1]);
                    tangentZ = current - front;
                }

                // Calculate normal from cross product
                if (tangentX.LengthSquared() > 0 && tangentZ.LengthSquared() > 0)
                {
                    normal = tangentZ.Cross(tangentX).Normalized();
                }
                else
                {
                    // Fallback for corner case where we have no neighbors
                    normal = Vector3.Up;
                }

                normals[idx] = normal;
            }
        }

        // Get the existing arrays from the mesh
        var arrays = _reusableMesh.SurfaceGetArrays(0);

        // Update just the normals
        arrays[(int)Mesh.ArrayType.Normal] = normals;

        // Clear and recreate the surface
        _reusableMesh.ClearSurfaces();
        _reusableMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        _reusableMesh.SurfaceSetMaterial(0, Material);

        // Re-cache the RID since we recreated the surface
        _meshRid = _reusableMesh.GetRid();
    }
    
    // private void UpdateNormals(Vector3[,] data)
    // {
    //     var width = data.GetLength(0);
    //     var depth = data.GetLength(1);
    //     
    //     // Ensure normal buffer is the right size
    //     if (_normalVectorBuffer == null || _normalVectorBuffer.Length != width * depth)
    //     {
    //         _normalVectorBuffer = new Vector3[width * depth];
    //     }
    //     
    //     // Calculate normals based on the grid structure
    //     for (var x = 0; x < width; x++)
    //     {
    //         for (var z = 0; z < depth; z++)
    //         {
    //             var idx = x * depth + z;
    //             var current = TransformPointFromDataSpaceToPositionSpace(data[x, z]);
    //             
    //             // Calculate normal using neighboring points
    //             Vector3 normal;
    //             
    //             if (x > 0 && x < width - 1 && z > 0 && z < depth - 1)
    //             {
    //                 // Interior point - use cross product of differences
    //                 var left = TransformPointFromDataSpaceToPositionSpace(data[x - 1, z]);
    //                 var right = TransformPointFromDataSpaceToPositionSpace(data[x + 1, z]);
    //                 var front = TransformPointFromDataSpaceToPositionSpace(data[x, z - 1]);
    //                 var back = TransformPointFromDataSpaceToPositionSpace(data[x, z + 1]);
    //                 
    //                 var dx = right - left;
    //                 var dz = back - front;
    //                 normal = dz.Cross(dx).Normalized();
    //             }
    //             else
    //             {
    //                 // Edge point - use simplified calculation
    //                 normal = Vector3.Up;
    //             }
    //             
    //             _normalVectorBuffer[idx] = normal;
    //         }
    //     }
    //     
    //     // Pack normals into byte array
    //     var index = 0;
    //     for (var i = 0; i < _normalVectorBuffer.Length; i++)
    //     {
    //         var normal = _normalVectorBuffer[i];
    //         BitConverter.GetBytes(normal.X).CopyTo(_normalDataBuffer, index);
    //         BitConverter.GetBytes(normal.Y).CopyTo(_normalDataBuffer, index + 4);
    //         BitConverter.GetBytes(normal.Z).CopyTo(_normalDataBuffer, index + 8);
    //         index += 12;
    //     }
    //     
    //     // Update the normals in the mesh using SurfaceUpdateAttributeRegion
    //     // The normal attribute is typically at index 1 in the vertex format
    //     RenderingServer.MeshSurfaceUpdateAttributeRegion(_meshRid, 0, 1, _normalDataBuffer);
    // }
    
    // TODO: Just use a shader for this
    // TODO: Honestly, this whole class should use a vertext shader.
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
        var visibleWidth = (int)(width * progress);
        var edgeFraction = (width * progress) % 1;

        // We need at least 2 columns to create triangles
        if (visibleWidth < 2)
        {
            if (visibleWidth == 1 || edgeFraction > 0)
            {
                // Show a very thin slice using first two columns
                visibleWidth = 2;
                edgeFraction = 0;
                // Scale the second column to be very close to the first
                var scaleFactor = (width * progress) / 2.0f;
                
                // Create vertices with interpolation
                var vertices = new List<Vector3>();
                for (var x = 0; x < 2; x++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        if (x == 0)
                        {
                            vertices.Add(TransformPointFromDataSpaceToPositionSpace(data[0, z]));
                        }
                        else
                        {
                            // Interpolate between first and second column based on progress
                            var point = data[0, z].Lerp(data[1, z], scaleFactor);
                            vertices.Add(TransformPointFromDataSpaceToPositionSpace(point));
                        }
                    }
                }
                
                // Now create triangles with these 2 columns
                var indices = new List<int>();
                for (var z = 0; z < depth - 1; z++)
                {
                    var topLeft = z;
                    var topRight = topLeft + 1;
                    var bottomLeft = depth + z;
                    var bottomRight = bottomLeft + 1;
                    
                    indices.Add(topLeft);
                    indices.Add(bottomLeft);
                    indices.Add(bottomRight);
                    
                    indices.Add(topLeft);
                    indices.Add(bottomRight);
                    indices.Add(topRight);
                }
                
                BuildMeshFromVerticesAndIndices(vertices, indices);
                return;
            }
            else
            {
                // Progress is too small to show anything
                Mesh = new ArrayMesh();
                return;
            }
        }
        
        // Rest of the original method for visibleWidth >= 2
        var vertices2 = new List<Vector3>();
        var indices2 = new List<int>();
        
        // Create vertices up to visible width
        for (var x = 0; x < visibleWidth; x++)
        {
            for (var z = 0; z < depth; z++)
            {
                vertices2.Add(TransformPointFromDataSpaceToPositionSpace(data[x, z]));
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
                vertices2.Add(TransformPointFromDataSpaceToPositionSpace(interpolatedPoint));
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
                
                indices2.Add(topLeft);
                indices2.Add(bottomLeft);
                indices2.Add(bottomRight);
                
                indices2.Add(topLeft);
                indices2.Add(bottomRight);
                indices2.Add(topRight);
            }
        }
        
        BuildMeshFromVerticesAndIndices(vertices2, indices2);
    }
    
    private void CreateInterpolatedMesh(Vector3[,] fromData, Vector3[,] toData, float progress)
    {
        var width = Math.Min(fromData.GetLength(0), toData.GetLength(0));
        var depth = Math.Min(fromData.GetLength(1), toData.GetLength(1));
        
        EnsureMeshInitialized(width, depth);
        
        // Interpolate vertices directly into the buffer
        var index = 0;
        for (var x = 0; x < width; x++)
        {
            for (var z = 0; z < depth; z++)
            {
                var fromPoint = fromData[x, z];
                var toPoint = toData[x, z];
                var interpolatedPoint = fromPoint.Lerp(toPoint, progress);
                var vertex = TransformPointFromDataSpaceToPositionSpace(interpolatedPoint);
                
                BitConverter.GetBytes(vertex.X).CopyTo(_vertexDataBuffer, index);
                BitConverter.GetBytes(vertex.Y).CopyTo(_vertexDataBuffer, index + 4);
                BitConverter.GetBytes(vertex.Z).CopyTo(_vertexDataBuffer, index + 8);
                index += 12;
            }
        }
        
        // Update vertex positions
        RenderingServer.MeshSurfaceUpdateVertexRegion(_meshRid, 0, 0, _vertexDataBuffer);
        
        // For interpolated meshes, we could interpolate normals too, but recalculating is more accurate
        // Create a temporary interpolated data array for normal calculation
        var interpolatedData = new Vector3[width, depth];
        for (var x = 0; x < width; x++)
        {
            for (var z = 0; z < depth; z++)
            {
                interpolatedData[x, z] = fromData[x, z].Lerp(toData[x, z], progress);
            }
        }
        UpdateNormals(interpolatedData);
    }

    public Tween TweenTransition(double duration = AnimationUtilities.DefaultDuration)
    {
        // For now, just create the mesh and return a default tween
        CreateMesh();
        return CreateTween();
    }

    public IStateChange Disappear()
    {
        throw new NotImplementedException();
    }

    // public Animation Disappear()
    // {
    //     // For now, return an empty animation
    //     return new Animation();
    // }

    private void CreateMesh()
    {
        if (_surfaceStates.Count == 0)
        {
            GD.Print("No height data available to create mesh.");
            return;
        }
        
        UpdateMeshVertices(_surfaceStates[_surfaceStates.Count - 1]);
    }
    
    private void BuildMeshFromVerticesAndIndices(List<Vector3> vertices, List<int> indices)
    {
        // Initialize once
        _reusableMesh ??= new ArrayMesh();
        
        // Clear existing surfaces
        for (int i = _reusableMesh.GetSurfaceCount() - 1; i >= 0; i--)
            _reusableMesh.ClearSurfaces();
        
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        
        var normals = CalculateNormals(vertices, indices);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        
        Material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        
        _reusableMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        Mesh = _reusableMesh;
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
