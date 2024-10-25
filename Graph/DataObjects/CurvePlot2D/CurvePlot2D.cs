using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools.Utilities;
using Array = Godot.Collections.Array;

namespace PrimerTools.Graph;

[Tool]
public partial class CurvePlot2D : MeshInstance3D, IPrimerGraphData
{
    #region Data space transform
    public delegate Vector3 Transformation(Vector3 inputPoint);
    public Transformation TransformPointFromDataSpaceToPositionSpace = point => point;
    #endregion

    #region Appearance
    private float _width = 1;

    public float Width
    {
        set => _width = value / 1000;
        get => _width * 1000;
    }
    private int _jointVertices = 3;
    private int _endCapVertices = 5;
    
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
    private List<Vector3> _dataPoints = new();
    public delegate List<Vector3> DataFetch();
    public DataFetch DataFetchMethod = () =>
    {
        PrimerGD.PrintWithStackTrace("Data fetch method not assigned. Returning empty list.");
        return new List<Vector3>();
    };

    public void FetchData()
    {
        _dataPoints = DataFetchMethod();
    }
    
    /// <summary>
    /// Sets the data that will be the target of the next transition.
    /// </summary>
    /// <param name="data"></param>
    public void SetData(params Vector3[] data)
    {
        _dataPoints = data.ToList();
    }
    /// <summary>
    /// Sets the data which will be the target of the next transition.
    /// This overload takes just a float, and the x value will be inferred by value index. 
    /// </summary>
    /// <param name="data"></param>
    public void SetData(params float[] data)
    {
        _dataPoints = data.Select((x, i) => new Vector3(i, x, 0)).ToList();
    }
    
    /// <summary>
    /// Gets the current data that will be the target of the next transition.
    /// </summary>
    /// <returns></returns>
    public Vector3[] GetData()
    {
        return _dataPoints.ToArray();
    }
    public void AddDataPoint(Vector3 newDataPoint)
    {
        _dataPoints.Add(newDataPoint);
    }

    #endregion
    
    #region Transitions
    public readonly List<Vector3[]> pointsOfStages = new();
    private float _renderExtent;
    [Export] public float RenderExtent
    {
        get => _renderExtent;
        set
        {
            // Comments on line interpolation method
            // - There are multiple ways to do this, which will have different visual effects.
            // - The approach implemented below identifies the line with a smaller number of points as "shorterLine".
            // Points with index less than the length of the shorter line are blended between the two lines.
            // Points with index greater are added one after another with timing based on the total length of 
            // segments to be added. It's usually not the case that we want to both move existing points and add new ones.
            // So this approach handles the case of adjusting points in a line, and also adding length to a line at a
            // constant rate. But it is perhaps a bit weird when it does both.
            // - Other approaches could include
            // -- Updating along the length of the line, including existing points, which would create a whiplash effect
            // -- Adding duplicate points to the end of the shorter line, then updating all points at once. Seems bad?
            // -- Adding points to the middle of the shorter line depending on lengths. Good for uniformly changing
            // a whole curve to one with a different number of points, but seems more complicated.
            //
            // In any case, just wanted to record the thought that this is just one choice for how to do this, which
            // works well for the case of adding data to a line. So if a new context arises, adding interpolation
            // options would be possible.
            
            if (_width == 0) GD.PushWarning("Line width is zero. Just so you know. :)");

            // if (value != renderExtent) GD.Print("RenderExtent: " + value);
            _renderExtent = value;
            if (value > pointsOfStages.Count - 1) return;
            var stepProgress = value % 1;
            Vector3[] targetPoints;
            if (stepProgress == 0)
            {
                // This is mainly for handling the case where we're right on the final stage
                // Since in that case the other section breaks
                // But also a small shortcut when it happens to be an integer.
                targetPoints = pointsOfStages[(int)value];
            }
            else
            {
                // Identify the two stages to blend between
                var prev = pointsOfStages[(int)value];
                var next = pointsOfStages[(int)value + 1];

                // Swap the two stages if the next stage is shorter
                // Need to record this to know whether to reverse the blending
                var backward = false;
                if (prev.Length > next.Length)
                {
                    (prev, next) = (next, prev);
                    backward = true;
                }

                var pointCountDifference = next.Length - prev.Length;

                var lengthPerAdditionalPoint = new List<float>();
                for (var i = 0; i < pointCountDifference; i++)
                    lengthPerAdditionalPoint.Add(
                        (next[prev.Length + i] - next[prev.Length - 1 + i]).Length());

                var lengthOfAdditionalPoints = lengthPerAdditionalPoint.Sum();
                var lengthToExtend = stepProgress * lengthOfAdditionalPoints;

                if (backward) lengthToExtend = lengthOfAdditionalPoints - lengthToExtend;

                targetPoints = new Vector3[Mathf.Max(next.Length, prev.Length)];
                var lengthSoFar = 0f;
                foreach (var i in Enumerable.Range(0, targetPoints.Length))
                {
                    var minPointCount = Mathf.Min(prev.Length, next.Length);
                    // If the point exists in both stages, blend between them
                    if (i < minPointCount)
                    {
                        targetPoints[i] = prev[i].Lerp(next[i], stepProgress);
                        continue;
                    }

                    // New points that have already been drawn
                    var segmentLength = lengthPerAdditionalPoint[i - minPointCount];
                    if (lengthToExtend > lengthSoFar + segmentLength)
                        targetPoints[i] = next[i];
                    // Segments in the progress of being drawn
                    else if (lengthToExtend > lengthSoFar)
                        targetPoints[i] = next[i - 1]
                            .Lerp(next[i], (lengthToExtend - lengthSoFar) / segmentLength);
                    // Segments that haven't been drawn yet. Just put the end points on top of the previous point.
                    else
                        targetPoints[i] = targetPoints[i - 1];

                    lengthSoFar += segmentLength;
                }
            }

            // Render a mesh based on the new set of points
            var arrayMesh = new ArrayMesh();
            Mesh = arrayMesh;
            arrayMesh.AddSurfaceFromArrays(
                Mesh.PrimitiveType.Triangles,
                MakeMeshData(targetPoints)
            );
            Mesh.SurfaceSetMaterial(0, Material);
        }
    }
    
    /// <summary>
    /// Creates an animation that transitions to the current data, set by SetData.
    /// </summary>
    /// <param name="duration"></param>
    /// <returns></returns>
    public Animation Transition(double duration = AnimationUtilities.DefaultDuration)
    {
        // If there's not a previous stage, add the first point of the data as the first stage
        if (pointsOfStages.Count == 0)
            pointsOfStages.Add(new[] { TransformPointFromDataSpaceToPositionSpace(_dataPoints[0]) });
        pointsOfStages.Add(_dataPoints.Select(x => TransformPointFromDataSpaceToPositionSpace(x)).ToArray());
        return this.AnimateValue(RenderExtent + 1, "RenderExtent");
    }

    /// <summary>
    /// Creates a tween that will transition the curve to the latest data. The returned tween will usually not be needed,
    /// but it's there if you want to chain things or whatever.
    /// TODO: It currently only works if one data point has been added. If it needs to work with zero or more than one,
    /// we'll need to add that.
    /// </summary>
    /// <param name="duration"></param>
    /// <returns></returns>
    public Tween TweenTransition(double duration = AnimationUtilities.DefaultDuration)
    {
        if (pointsOfStages.Count == 0)
            pointsOfStages.Add(new[] { TransformPointFromDataSpaceToPositionSpace(_dataPoints[0]) });
        pointsOfStages.Add(_dataPoints.Select(x => TransformPointFromDataSpaceToPositionSpace(x)).ToArray());
        
        var tween = CreateTween();
        tween.TweenProperty(
            this,
            "RenderExtent",
            RenderExtent + 1,
            duration
        );
        return tween;
    }

    public Animation Disappear()
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Mesh construction
    private Array MakeMeshData(Vector3[] points)
    {
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        CreateSegments(points, vertices, indices);
        AddJoints(points, vertices, indices);
        AddEndCap(vertices, indices, points);
        AddEndCap(vertices, indices, points);

        // Make sure there's at least one point in the mesh so a line with a single point is considered valid, if invisible.
        // Needed because the animations usually start by drawing a line from nothing.
        if (vertices.Count == 0)
        {
            vertices = new List<Vector3> { Vector3.Zero };
            indices = new List<int> { 0, 0, 0 };
        }

        var vertexArray = vertices.ToArray();
        var indexArray = indices.ToArray();
        var normalArray = Enumerable.Repeat(Vector3.Back, vertexArray.Length).ToArray();
        MeshUtilities.MakeDoubleSided(ref vertexArray, ref indexArray, ref normalArray);

        var surfaceArray = new Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        surfaceArray[(int)Mesh.ArrayType.Vertex] = vertexArray;
        surfaceArray[(int)Mesh.ArrayType.Normal] = normalArray;
        surfaceArray[(int)Mesh.ArrayType.Index] = indexArray;

        return surfaceArray;
    }
    private void CreateSegments(Vector3[] points, List<Vector3> vertices, List<int> indices)
    {
        for (var i = 0; i < points.Length - 1; i++)
        {
            var currentPoint = points[i];
            var nextPoint = points[i + 1];
            var direction = (nextPoint - currentPoint).Normalized();
            var perpendicular = new Vector3(-direction.Y, direction.X, 0) * _width / 2;

            // Add the four vertices of the rectangle to the list
            vertices.Add(currentPoint + perpendicular);
            vertices.Add(currentPoint - perpendicular);
            vertices.Add(nextPoint + perpendicular);
            vertices.Add(nextPoint - perpendicular);

            // Add the indices of the two triangles to the list
            var index = i * 4;
            indices.AddTriangle(index, index + 2, index + 1);
            indices.AddTriangle(index + 1, index + 2, index + 3);
        }
    }
    private void AddJoints(Vector3[] points, List<Vector3> vertices, List<int> triangles)
    {
        // TODO: The segments overlap on one side of the joint. On the other side, there is a gap.
        // Currently, we add some rounding to fill the gap, and that's the joint.
        // Additional options could be to 
        // - Add a miter joint, which extends both outer lines until they intersect. This intersection would be
        //   calculated by adding the line's width to the outer length of both segments.
        // - The inner part of the joint could be made to not overlap by doing the reverse the process for the outer
        //   miter joint.
        // - With a miter joint, the UV map could be made nice for drawing a texture or gradient on the line.
        // - If inner joint is a miter but the outer is still curved, the rounded part of the curve wouldn't
        //   agree super will with UVs, perhaps needing to be a solid color in a gradient situation. But in a situation
        //   where we want a gradient, there might be so many points that miter angles are small, so the rounding
        //   wouldn't matter anyway.

        // Join segments with triangles in a curve
        for (var i = 0; i < points.Length - 2; i++)
        {
            var a = points[i];
            var b = points[i + 1];
            var c = points[i + 2];

            // The stuff below was trying to get around some Vector3.Slerp errors. But now we just rotate the vector.
            // These still seem like good checks, though.
            // It might be better to purge these points from the list rather than skipping a joint.
            // A duplicated point would break the joint even though you might still want one there.
           
            if ((c - a).IsZeroApprox() || (b - a).IsZeroApprox()) continue;
            if ((c - a).Normalized().IsEqualApprox((b - a).Normalized())) { continue; }
            if ((c - a).Normalized().IsEqualApprox((a - b).Normalized())) { continue; }

            var quadIndex = i * 4;
            var center = vertices.Count;
            vertices.Add(b);

            var flip = (b - a).Cross(c - b).Z < 0;
            var (leftIndex, rightIndex) = flip
                ? (quadIndex + 2, quadIndex + 4)
                : (quadIndex + 3, quadIndex + 5);

            var left = vertices[leftIndex];
            var right = vertices[rightIndex];

            if (_jointVertices <= 0)
            {
                triangles.AddTriangle(center, rightIndex, leftIndex, flip);
                continue;
            }

            for (var j = 0; j < _jointVertices; j++)
            {
                var t = (j + 1) / (float)(_jointVertices + 1);
                
                vertices.Add(PrimerMathUtils.SlerpThatWorks(left - b, right - b, t) + b);

                var corner = j == 0 ? leftIndex : center + j;
                triangles.AddTriangle(corner, center, center + j + 1, flip);
            }

            triangles.AddTriangle(center + _jointVertices, center, rightIndex, flip);
        }
    }
    private void AddEndCap(List<Vector3> vertices, List<int> triangles, Vector3[] points, bool end = true)
    {
        Vector3 point;
        Vector3 prev;
        if (end)
        {
            point = points[^1];
            prev = point;
            for (var i = points.Length - 2; i >= 0; i--)
            {
                if (points[i] == point) continue;
                prev = points[i];
                break;
            }
        }
        else
        {
            point = points[0];
            prev = point;
            for (var i = 1; i < points.Length; i++)
            {
                if (points[i] == point) continue;
                prev = points[i];
                break;
            }
        }

        if (prev == point) return;

        var direction = (point - prev).Normalized() * _width / 2;

        var perpendicular = new Vector3(direction.Y, -direction.X, 0);
        var center = vertices.Count;
        var totalVertices = (float)_endCapVertices + 2;

        vertices.Add(point);
        vertices.Add(point - perpendicular);

        for (var i = 1; i <= totalVertices; i++)
        {
            var t = i / totalVertices;
            var a = t < 0.5
                ? (-perpendicular).Slerp(direction, t * 2)
                : direction.Slerp(perpendicular, t * 2 - 1);

            vertices.Add(a + point);
            triangles.AddTriangle(center, center + i, center + i + 1);
        }
    }
    #endregion

    #region Old Unity code for reference
    // public class PrimerLine : MonoBehaviour, IMeshController, IDisposable, IPrimerGraphData
//     {
//         private ILine renderedLine = new DiscreteLine(1);
//         private Vector3[] rawPoints = Array.Empty<Vector3>();
//         public int pointCount => rawPoints.Length;
//         private Vector3[] transformedPoints => rawPoints.Select( x => transformPointFromDataSpaceToPositionSpace(x)).ToArray();
//
//         private MeshFilter meshFilterCache;
//         private MeshFilter meshFilter => transform.GetOrAddComponent(ref meshFilterCache);
//
//         private MeshRenderer meshRendererCache;
//         private MeshRenderer meshRenderer => transform.GetOrAddComponent(ref meshRendererCache);
//
//         public delegate Vector3 Transformation(Vector3 inputPoint);
//         public Transformation transformPointFromDataSpaceToPositionSpace = point => point;
//
//         private Graph3 graph => transform.parent.GetComponent<Graph3>();
//         
//         #region public float width;
//         [SerializeField, HideInInspector]
//         private float _width = 0.1f;
//
//         [ShowInInspector]
//         public float width {
//             get => _width;
//             set {
//                 _width = value;
//                 Render();
//             }
//         }
//         #endregion
//
//         #region public float resolution;
//         [ShowInInspector]
//         [MinValue(1)]
//         public int resolution {
//             get => renderedLine.numSegments;
//             set => Render(renderedLine.ChangeResolution(value));
//         }
//         #endregion
//
//         #region public int endCapVertices;
//         [SerializeField, HideInInspector]
//         private int _endCapVertices = 18;
//
//         [ShowInInspector]
//         public int endCapVertices {
//             get => _endCapVertices;
//             set {
//                 _endCapVertices = value;
//                 Render();
//             }
//         }
//
//         public bool endCapRight = true;
//         public bool endCapLeft = true;
//         #endregion
//
//         #region public int miterVertices;
//         [SerializeField, HideInInspector]
//         private int _miterVertices = 3;
//
//         [ShowInInspector]
//         public int miterVertices {
//             get => _miterVertices;
//             set {
//                 _miterVertices = value;
//                 Render();
//             }
//         }
//         #endregion
//
//         #region public bool doubleSided;
//         [SerializeField, HideInInspector]
//         private bool _doubleSided = false;
//
//         [ShowInInspector]
//         public bool doubleSided {
//             get => _doubleSided;
//             set {
//                 _doubleSided = value;
//                 Render();
//             }
//         }
//         #endregion
//
//         /// <summary>
//         ///   Allows to set one lines above the others.
//         ///   Positive values will move the line closer to the camera.
//         ///   Default index is 0 for all lines.
//         /// </summary>
//         public void SetZIndex(int index)
//         {
//             var pos = transform.localPosition;
//             transform.localPosition = new Vector3(pos.x, pos.y, index * -0.01f);
//         }
//
//         #region SetData(float[] | Vector2[] | Vector3[])
//         public void SetData(IEnumerable<float> data)
//         {
//             rawPoints = data.Select((y, i) => new Vector3(i, y, 0)).ToArray();
//         }
//
//         public void SetData(IEnumerable<Vector2> data)
//         {
//             rawPoints = data.Cast<Vector3>().ToArray();
//         }
//
//         public void SetData(IEnumerable<Vector3> data)
//         {
//             rawPoints = data.ToArray();
//         }
//         #endregion
//
//         #region AddPoint(float | Vector2 | Vector3)
//         public void AddPoint(params float[] data)
//         {
//             rawPoints = rawPoints.Concat(data.Select((y, i) => new Vector3(rawPoints.Length + i, y, 0))).ToArray();
//         }
//         public void AddPoint(params Vector2[] data)
//         {
//             rawPoints = rawPoints.Concat(data.Cast<Vector3>()).ToArray();
//         }
//
//         public void AddPoint(params Vector3[] data)
//         {
//             rawPoints = rawPoints.Concat(data).ToArray();
//         }
//         #endregion
//
//         #region SetFunction(Func<float, float> | Func<float, Vector2> | Func<float, Vector3>, int?, float?, float?)
//         public void SetFunction(Func<float, float> function, int? numPoints = null, float? xStart = null, float? xEnd = null)
//         {
//             numPoints ??= resolution;
//
//             if (xStart is null)
//             {
//                 if (graph is not null)
//                 {
//                     xStart = graph.xAxis.min;
//                 }
//                 else
//                 {
//                     xStart = 0;
//                 }
//             }
//
//             if (xEnd is null)
//             {
//                 if (graph is not null)
//                 {
//                     xEnd = graph.xAxis.max;
//                 }
//                 else
//                 {
//                     xEnd = 10;
//                 }
//             }
//             
//             rawPoints = new Vector3[numPoints.Value];
//             for (var i = 0; i < numPoints; i++)
//             {
//                 var extent = (float)i / numPoints.Value;
//                 var x = extent * (xEnd.Value - xStart.Value) + xStart.Value;
//                 var y = function(x);
//                 rawPoints[i] = new Vector3(x, y, 0);
//             }
//         }
//         
//         // Parametric 2D function
//         public void SetFunction(Func<float, Vector2> function, float tStart, float tEnd, int? numPoints = null)
//         {
//             SetFunction(Vector3Function, tStart, tEnd, numPoints: numPoints);
//             return;
//
//             Vector3 Vector3Function(float t)
//             {
//                 return function(t);
//             }
//         }
//         
//         // Parametric 3D function
//         public void SetFunction(Func<float, Vector3> function, float tStart, float tEnd, int? numPoints = null)
//         {
//             numPoints ??= resolution;
//
//             rawPoints = new Vector3[numPoints.Value];
//             for (var i = 0; i < numPoints; i++)
//             {
//                 var extent = (float)i / numPoints.Value;
//                 var t = extent * (tEnd - tStart) + tStart;
//                 rawPoints[i] = function(t);
//             }
//         }
//         #endregion
//         
//         public Tween Transition()
//         {
//             if (transformedPoints.Length == 0)
//                 return Tween.noop;
//             
//             if (renderedLine.numSegments == 0)
//             {
//                 return GrowFromStart();
//             }
//             
//             var targetLine = new DiscreteLine(transformedPoints);
//             if (targetLine.points == renderedLine.points)
//                 return Tween.noop;
//             
//             var (from, to) = ILine.SameResolution(renderedLine, targetLine);
//
//
//             return new Tween(
//                 t => Render(ILine.Lerp(from, to, t))
//             ).Observe(
//                 // After the transition is complete, we ensure we store the line we got
//                 // instead of the result of ILine.Lerp() which is always a DiscreteLine.
//                 afterComplete: () => Render(targetLine)
//             );
//         }
//
//         public Tween GrowFromStart()
//         {
//             var targetLine = new DiscreteLine(transformedPoints);
//             var resolution = targetLine.numSegments;
//
//             return new Tween(
//                 t => Render(targetLine.SmoothCut(resolution * t, fromOrigin: false))
//             ).Observe(
//                 // After the transition is complete, we ensure we store the line we got
//                 // instead of the result of SmoothCut() which is always a DiscreteLine.
//                 afterComplete: () => Render(targetLine)
//             );
//         }
//
//         public Tween ShrinkToEnd()
//         {
//             var targetLine = renderedLine;
//             var resolution = targetLine.numSegments;
//
//             return new Tween(
//                 t => Render(targetLine.SmoothCut(resolution * (1 - t), fromOrigin: true))
//             ).Observe(
//                 afterComplete: () => gameObject.SetActive(false)
//             );
//         }
//
//         public void Dispose()
//         {
//             gameObject.SetActive(false);
//         }
//
//         public void Reset()
//         {
//             Render(new DiscreteLine(1));
//         }
//         
//         public MeshRenderer[] GetMeshRenderers() => new[] { meshRenderer };
//
//         private void Render() => Render(renderedLine);
//         private void Render(ILine line)
//         {
//             if (line == null)
//                 return;
//
//             var points = line.points;
//             var vertices = new List<Vector3>();
//             var triangles = new List<int>();
//
//             CreateSegments(points, vertices, triangles);
//             AddMiter(points, vertices, triangles);
//
//             if (points.Length is not 0 && endCapVertices > 1) {
//                 if (points.Length is 1) {
//                     if (endCapLeft) AddEndCap(vertices, triangles, points[0], Vector3.left);
//                     if (endCapRight) AddEndCap(vertices, triangles, points[0], Vector3.right);
//                 }
//                 else {
//                     if (endCapLeft) AddEndCap(vertices, triangles, points[0], points[1]);
//                     if (endCapRight)
//                     {
//                         var point1 = points[^1];
//                         var point2 = points[^2];
//
//                         if (point1 == point2)
//                         {
//                             point1 = points[^2];
//                             point2 = points[^3];
//                         }
//                         
//                         AddEndCap(vertices, triangles, point1, point2);
//                     } 
//                 }
//             }
//
//             var vertexArray = vertices.ToArray();
//             var triangleArray = triangles.ToArray();
//             
//             if (_doubleSided)
//             {
//                 MeshUtilities.MakeDoubleSided(ref vertexArray, ref triangleArray);
//             }
//
//             var mesh = new Mesh {
//                 vertices = vertexArray,
//                 triangles = triangleArray,
//             };
//             mesh.RecalculateNormals();
//
//             meshFilter.mesh = mesh;
//             renderedLine = line;
//         }
//
//
//         #region Drawing mesh
//         private void CreateSegments(Vector3[] points, List<Vector3> vertices, List<int> triangles)
//         {
//             for (var i = 0; i < points.Length - 1; i++) {
//                 var currentPoint = points[i];
//                 var nextPoint = points[i + 1];
//                 var direction = (nextPoint - currentPoint).normalized;
//                 var perpendicular = new Vector3(-direction.y, direction.x, 0) * width / 2;
//
//                 // Add the four vertices of the rectangle to the list
//                 vertices.Add(currentPoint + perpendicular);
//                 vertices.Add(currentPoint - perpendicular);
//                 vertices.Add(nextPoint + perpendicular);
//                 vertices.Add(nextPoint - perpendicular);
//
//                 // Add the indices of the two triangles to the list
//                 var index = i * 4;
//                 triangles.AddTriangle(index, index + 2, index + 1);
//                 triangles.AddTriangle(index + 1, index + 2, index + 3);
//             }
//         }
//
//         private void AddMiter(Vector3[] points, List<Vector3> vertices, List<int> triangles)
//         {
//             // Join segments with triangles in a curve
//             for (var i = 0; i < points.Length - 2; i++) {
//                 var a = points[i];
//                 var b = points[i + 1];
//                 var c = points[i + 2];
//
//                 var quadIndex = i * 4;
//                 var center = vertices.Count;
//                 vertices.Add(b);
//
//                 var flip = Vector3.Cross(b - a, c - b).z < 0 ;
//                 var (leftIndex, rightIndex) = flip
//                     ? (quadIndex + 2, quadIndex + 4)
//                     : (quadIndex + 3, quadIndex + 5);
//
//                 var left = vertices[leftIndex];
//                 var right = vertices[rightIndex];
//
//                 // miterVertices can also be calculated from the amount of degrees in the angle
//                 // miterVertices = Vector3.Angle(left - b, right - b) / degreesPerVertex
//
//                 if (miterVertices <= 0) {
//                     triangles.AddTriangle(center, rightIndex, leftIndex, flip);
//                     continue;
//                 }
//
//                 for (var j = 0; j < miterVertices; j++) {
//                     var t = (j + 1) / (float)(miterVertices + 1);
//                     vertices.Add(Vector3.Slerp(left - b, right - b, t) + b);
//
//                     var corner = j == 0 ? leftIndex : center + j;
//                     triangles.AddTriangle(corner, center, center + j + 1, flip);
//                 }
//
//                 triangles.AddTriangle(center + miterVertices, center, rightIndex, flip);
//             }
//         }
//
//         private void AddEndCap(List<Vector3> vertices, List<int> triangles, Vector3 point, Vector3 prev)
//         {
//             var direction = -(prev - point).normalized * width / 2;
//             
//             var perpendicular = new Vector3(direction.y, -direction.x, 0);
//             var center = vertices.Count;
//             var totalVertices = (float)endCapVertices + 2;
//
//             vertices.Add(point);
//             vertices.Add(point - perpendicular);
//
//             for (var i = 1; i <= totalVertices; i++) {
//                 var t = i / totalVertices;
//                 var a = t < 0.5
//                     ? Vector3.Slerp(-perpendicular, direction, t * 2)
//                     : Vector3.Slerp(direction, perpendicular, t * 2 - 1);
//
//                 vertices.Add(a + point);
//                 triangles.AddTriangle(center, center + i, center + i + 1);
//             }
//         }
//         #endregion
//     }
    #endregion
}