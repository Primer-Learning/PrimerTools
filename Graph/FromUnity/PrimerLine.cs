// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Primer.Animation;
// using Primer.Shapes;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// namespace Primer.Graph
// {
//     [ExecuteAlways]
//     [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
//     public class PrimerLine : MonoBehaviour, IMeshController, IDisposable, IPrimerGraphData
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
// }
