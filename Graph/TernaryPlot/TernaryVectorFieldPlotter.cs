// using Primer.Graph;
// using Primer.Shapes;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// [RequireComponent(typeof(TernaryPlot))]
// public abstract class TernaryVectorFieldPlotter : MonoBehaviour
// {
//     public bool liveUpdate = false;
//     public int automaticPointIncrements = 3;
//     public float baseArrowLength = 1;
//     public float baseArrowThickness = 1;
//
//     protected TernaryPlot ternaryPlot => GetComponent<TernaryPlot>();
//     
//     public void OnValidate()
//     {
//         if (liveUpdate)
//             PlotArrows();
//     }
//
//     public void TriggeredPlotArrows()
//     {
//         if (liveUpdate)
//         {
//             PlotArrows();
//         }
//     }
//
//     [Button]
//     private void PlotArrows()
//     {
//         SetUp();
//         var gnome = ternaryPlot.GetContentGnome("VectorFieldArrows");
//
//         var points = ternaryPlot.isQuaternary
//             ? TernaryPlotUtility.EvenlyDistributedPoints3D(automaticPointIncrements)
//             : TernaryPlotUtility.EvenlyDistributedPoints(automaticPointIncrements);
//
//         foreach (var point in points)
//         {
//             var difference = TernaryDifferential(point);
//
//             // Get the difference between start and end result as a normalized vector
//             var pointAsVector = TernaryPlot.CoordinatesToPosition(point);
//             var differenceVector = TernaryPlot.CoordinatesToPosition(difference);
//
//             if (!(differenceVector.sqrMagnitude > 0.000001f))
//                 continue;
//
//             differenceVector /= differenceVector.magnitude;
//             var adjustedDifferenceVector = baseArrowLength * differenceVector / automaticPointIncrements;
//
//             // Draw the arrow
//             var arrow = gnome.AddArrow();
//             arrow.thickness = baseArrowThickness / automaticPointIncrements;
//             arrow.tail = pointAsVector - adjustedDifferenceVector / 2;
//             arrow.head = pointAsVector + adjustedDifferenceVector / 2;
//         }
//
//         gnome.Purge(defer: true);
//     }
//
//     protected abstract void SetUp();
//
//     protected abstract float[] TernaryDifferential(float[] point);
// }
