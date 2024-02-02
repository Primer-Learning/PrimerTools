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
//     public class BarPlot : MonoBehaviour, IDisposable, IPrimerGraphData
//     {
//         private List<Tuple<float, float, float>> renderedRectProperties = new();
//         public List<float> data;
//         
//         private List<Tuple<float, float, float>> DataAsRectProperties()
//         {
//             return data.Select( (value, i) =>
//                 new Tuple<float, float, float>(
//                     transformPointFromDataSpaceToPositionSpace(new Vector3(i + _offset, 0, 0)).x,
//                     transformPointFromDataSpaceToPositionSpace(new Vector3(_barWidth, 0, 0)).x,
//                     transformPointFromDataSpaceToPositionSpace(new Vector3(0, value, 0)).y
//                 )
//             ).ToList();
//         }
//         
//         public delegate Vector3 Transformation(Vector3 inputPoint);
//         public Transformation transformPointFromDataSpaceToPositionSpace = point => point;
//
//         public List<Color> colors = new List<Color>(PrimerColor.all);
//
//         #region public float offset;
//         [SerializeField, HideInInspector]
//         private float _offset = 1;
//
//         [ShowInInspector]
//         public float offset {
//             get => _offset;
//             set {
//                 _offset = value;
//                 Render();
//             }
//         }
//         #endregion
//
//         #region public float barWidth;
//         [SerializeField, HideInInspector]
//         private float _barWidth = 0.8f;
//
//         [ShowInInspector]
//         public float barWidth {
//             get => _barWidth;
//             set {
//                 _barWidth = value;
//                 Render();
//             }
//         }
//         #endregion
//
//         public void SetData(params float[] bars)
//         {
//             data = bars.ToList();
//         }
//
//         public void AddData(params float[] newData)
//         {
//             data ??= new List<float>();
//             data.AddRange(newData);
//         }
//
//         public Tween Transition()
//         {
//             if (DataAsRectProperties().SequenceEqual(renderedRectProperties))
//                 return Tween.noop;
//
//             // Make a copy of the renderedRectProperties so that we can tween from it
//             var from = new List<Tuple<float, float, float>>(renderedRectProperties);
//
//             return new Tween(t => Render(LerpFloatTriple(from, DataAsRectProperties(), t)))
//                 .Observe(afterComplete: () => Render(DataAsRectProperties()));
//         }
//
//         public Tween GrowFromStart()
//         {
//             var initial = new List<Tuple<float, float, float>>();
//             var target = DataAsRectProperties() ?? renderedRectProperties;
//             return new Tween(t => Render(LerpFloatTriple(initial, target, t)));
//         }
//
//         public Tween ShrinkToEnd()
//         {
//             var initial = renderedRectProperties;
//             var target = new List<Tuple<float, float, float>>();
//             return new Tween(t => Render(LerpFloatTriple(initial, target, t)));
//         }
//
//         public void Dispose()
//         {
//             Gnome.Dispose(this);
//         }
//
//         private void Render() => Render(renderedRectProperties);
//         private void Render(List<Tuple<float, float, float>> dataToRender)
//         {;
//             var gnome = new Primer.SimpleGnome(transform);
//             gnome.Reset();
//
//             for (var i = 0; i < dataToRender.Count; i++) {
//                 var bar = dataToRender[i];
//
//                 var rect = gnome.Add<Rectangle>($"Bar {i} stack {0}");
//                 rect.transform.localPosition = new Vector3(bar.Item1, 0, 0);
//                 rect.pivot = RectPivot.BottomCenter;
//                 rect.width = bar.Item2;
//                 rect.height = bar.Item3;
//                 rect.color = colors[i % colors.Count];
//             }
//
//             renderedRectProperties = dataToRender;
//         }
//
//         private static List<Tuple<float, float, float>> LerpFloatTriple(List<Tuple<float, float, float>> a, List<Tuple<float, float, float>> b, float t)
//         {
//             // The default values don't properly handle offset. Since this is a static method, we can't look at 
//             // instance variables to subtract offset from the position.
//             // It works fine as long as offset is 1.
//             // To make this work properly with other offset values, we need to pass in the offset as a parameter
//             // or make this a non-static method, or ensure the tuple lists are the same length before passing them
//             // to this method.
//             
//             var result = new List<Tuple<float, float, float>>();
//             var barsCount = Mathf.Max(a.Count, b.Count);
//             
//             var xSpacingA = a.Count > 0 ? a[0].Item1 : b.Count > 0 ? b[0].Item1 : 1f;
//             var defaultWidth = a.Count > 0 ? a[0].Item2 : b.Count > 0 ? b[0].Item2 : 0f;
//             var defaultHeight = 0f;
//             var xSpacingB = b.Count > 0 ? b[0].Item1 : a.Count > 0 ? a[0].Item1 : 0f;
//
//             for (var i = 0; i < barsCount; i++) {
//                 var barA = i < a.Count ? a[i] : new Tuple<float, float, float>((i + 1) * xSpacingA, defaultWidth, defaultHeight);
//                 var barB = i < b.Count ? b[i] : new Tuple<float, float, float>((i + 1) * xSpacingB, defaultWidth, defaultHeight);
//                 result.Add(
//                     new Tuple<float, float, float>(
//                         Mathf.Lerp(barA.Item1, barB.Item1, t),
//                         Mathf.Lerp(barA.Item2, barB.Item2, t),
//                         Mathf.Lerp(barA.Item3, barB.Item3, t)
//                     )
//                 );
//             }
//
//             return result;
//         }
//     }
// }
