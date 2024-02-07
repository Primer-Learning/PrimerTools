// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Primer.Animation;
// using Primer.Shapes;
// using Sirenix.OdinInspector;
// using Sirenix.Utilities;
// using UnityEngine;
// using UnityEngine.Serialization;
//
// namespace Primer.Graph
// {
//     [ExecuteAlways]
//     public class StackedArea : MonoBehaviour, IDisposable, IPrimerGraphData
//     {
//         private List<ILine> renderedLines = new();
//         public List<Vector3[]> rawPointSets = new();
//         
//         private List<Vector3[]> transformedPointSets => rawPointSets
//             .Select(set => set
//                 .Select( x => transformPointFromDataSpaceToPositionSpace(x)).ToArray()).ToList();
//
//         public List<Color> colors = new List<Color>(PrimerColor.all);
//
//         public delegate Vector3 Transformation(Vector3 inputPoint);
//         public Transformation transformPointFromDataSpaceToPositionSpace = point => point;
//         private Graph3 graph => transform.parent.GetComponent<Graph3>();
//         
//         // private GraphDomain domainCache;
//         // private GraphDomain domain => transform.GetOrAddComponent(ref domainCache);
//
//         #region public bool doubleSided;
//         [SerializeField, HideInInspector]
//         private bool _doubleSided = true;
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
//         public void SetData(params float[][] data)
//         {
//             rawPointSets = new List<Vector3[]>();
//
//             for (var i = 0; i < data.Length; i++) {
//                 rawPointSets.Add(data[i].Select((y, j) => new Vector3(j, y)).ToArray());
//             }
//         }
//
//         public void AddData(params float[] newColumn)
//         {
//             rawPointSets ??= new List<Vector3[]>();
//             if (rawPointSets.Count == 0)
//             {
//                 for (var i = 0; i < newColumn.Length; i++) {
//                     rawPointSets.Add(Array.Empty<Vector3>());
//                 }
//             }
//             if (newColumn.Length != rawPointSets.Count)
//             {
//                 Debug.Log("New data has different length than existing data.");
//                 return;
//             }
//             
//             for (int i = 0; i < rawPointSets.Count; i++)
//             {
//                 rawPointSets[i] = rawPointSets[i].Append(new Vector3(rawPointSets[i].Length, newColumn[i]));
//             }
//             // for (var y = 0; y < newColumn.Length; y++) {
//             //     // then we ALSO need to add a point to the current areas so it doesn't deform when tweening
//             //     var rendered = renderedData[y];
//             //
//             //     // FunctionLines will adapt to the new point automatically
//             //     if (rendered is DiscreteLine discrete) {
//             //         // if the area has height at the end we need to drop straight to the bottom
//             //         var last = discrete.points.Last();
//             //         renderedData[y] = discrete.Append(new Vector3(last.x, 0));
//             //     }
//             //
//             //     // finally we add the new column to the incoming data
//             //     incomingData[y] = incomingData[y].ToDiscrete().Append(newColumn[y]);
//             // }
//         }
//
//         public void AddArea(params float[] newArea)
//         {
//             if (newArea.Length != rawPointSets[0].Length)
//             {
//                 Debug.Log("New data has different length than existing data.");
//                 return;
//             }
//             
//             rawPointSets.Add(newArea.Select((y, j) => new Vector3(j, y)).ToArray());
//         }
//
//         public Tween Transition()
//         {
//             if (transformedPointSets.Count == 0) return Tween.noop;
//             
//             // var targetData = new DiscreteLine(transformedPoints);
//             var targetData = transformedPointSets.Select(x => new DiscreteLine(x) as ILine).ToList();
//             if (targetData == renderedLines)
//                 return Tween.noop;
//
//             var linesCount = Math.Max(renderedLines.Count, targetData.Count);
//             var lines = new List<(ILine from, ILine to)>(linesCount);
//
//             if (renderedLines.Count == 0 || renderedLines.Any(x => x.numSegments == 0))
//             {
//                 return GrowFromStart();
//             }
//             
//             for (var i = 0; i < linesCount; i++) {
//                 var from = i < renderedLines.Count ? renderedLines[i] : FlatLine(renderedLines[0]);
//                 var to = i < targetData.Count ? targetData[i] : FlatLine(from);
//                 lines.Add((from, to));
//             }
//             
//             return new Tween(
//                 t => Render(lines.Select(x => ILine.Lerp(x.from, x.to, t)))
//             ).Observe(
//                 // After the transition is complete, we ensure we store the line we got
//                 // instead of the result of ILine.Lerp() which is always a DiscreteLine.
//                 afterComplete: () => Render(targetData)
//             );
//         }
//
//         public Tween GrowFromStart()
//         {
//             var targetData = transformedPointSets.Select(x => new DiscreteLine(x) as ILine).ToList();
//         
//             return new Tween(
//                 t => Render(targetData.Select(x => x.SmoothCut(x.numSegments * t, fromOrigin: false)))
//             ).Observe(
//                 // After the transition is complete, we ensure we store the line we got
//                 // instead of the result of SmoothCut() which is always a DiscreteLine.
//                 afterComplete: () => Render(targetData)
//             );
//         }
//
//         public Tween ShrinkToEnd()
//         {
//             var targetData = renderedLines;
//
//             return new Tween(
//                 t => Render(targetData.Select(x => x.SmoothCut(x.numSegments * (1 - t), fromOrigin: true)))
//             ).Observe(
//                 afterComplete: () => gameObject.SetActive(false)
//             );
//         }
//
//         public void Dispose()
//         {
//             Gnome.Dispose(this);
//         }
//
//         private void Render() => Render(renderedLines);
//         private void Render(IEnumerable<ILine> data)
//         {
//             var gnome = new Primer.SimpleGnome(transform);
//             gnome.Reset();
//             var lines = (data ?? new List<ILine>()).ToList();
//
//             if (lines.Count is 0) {
//                 return;
//             }
//
//             var prevLine = FlatLine(lines[0])
//                 .points
//                 // .Select(x => transformPointFromDataSpaceToPositionSpace(x))
//                 .ToArray();
//
//             for (var i = 0; i < lines.Count; i++) {
//                 var points = new List<Vector3>(prevLine);
//                 var triangles = new List<int>();
//                 var line = lines[i].points
//                     .Select((vec, j) => new Vector3(vec.x, vec.y + prevLine[j].y, vec.z))
//                     .ToArray();
//
//                 var lineStart = points.Count;
//                 points.Add(line[0]);
//
//                 for (var j = 1; j < line.Length; j++) {
//                     points.Add(line[j]);
//
//                     var a = j - 1;
//                     var b = j;
//                     var c = lineStart + j - 1;
//                     var d = lineStart + j;
//
//                     triangles.AddTriangle(a, d, b);
//                     triangles.AddTriangle(a, c, d);
//                 }
//
//                 var area = gnome.Add<MeshRenderer>($"Area {i}");
//                 area.material = RendererExtensions.defaultMaterial;
//                 area.SetColor(colors[i]);
//                 
//                 var vertexArray = points.ToArray();
//                 var triangleArray = triangles.ToArray();
//
//                 if (doubleSided)
//                 {
//                     MeshUtilities.MakeDoubleSided(ref vertexArray, ref triangleArray);
//                 }
//
//                 var mesh = new Mesh {
//                     vertices = vertexArray,
//                     triangles = triangleArray
//                 };
//                 mesh.RecalculateNormals();
//
//                 area.GetOrAddComponent<MeshFilter>().mesh = mesh;
//                 
//                 prevLine = line;
//             }
//
//             renderedLines = RemoveRedundantPoints(lines);
//         }
//
//         public void Reset()
//         {
//             transform.GetChildren().ForEach(x => x.gameObject.SetActive(false));
//             renderedLines = new List<ILine>();
//         }
//         
//         private static DiscreteLine FlatLine(ILine sample)
//         {
//             var points = sample.points
//                 .Select(vec => new Vector3(vec.x, 0))
//                 .ToArray();
//
//             return new DiscreteLine(points);
//         }
//
//         /// <summary>
//         ///   We add redundant points for the tweening to work properly, but we don't want to keep them
//         /// </summary>
//         private static List<ILine> RemoveRedundantPoints(IEnumerable<ILine> lines)
//         {
//             return lines
//                 .Select(line => line is DiscreteLine discrete ? discrete.RemoveRedundantPoints() : line)
//                 .ToList();
//         }
//     }
// }
