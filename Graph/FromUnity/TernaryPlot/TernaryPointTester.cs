// using Sirenix.OdinInspector;
// using UnityEngine;
// using UnityEngine.Serialization;
//
// namespace Primer.Graph
// {
//     [RequireComponent(typeof(TernaryPlot))]
//     public class TernaryPointTester : MonoBehaviour
//     {
//         [FormerlySerializedAs("startOnValueChange")]
//         public bool liveUpdate = false;
//         public int automaticPointIncrements = 3;
//         public float sphereSize = 1;
//
//         public void OnValidate()
//         {
//             if (liveUpdate)
//                 PlotPoints();
//         }
//
//         [Button]
//         private void PlotPoints()
//         {
//             var ternaryPlot = GetComponent<TernaryPlot>();
//             var gnome = ternaryPlot.GetContentGnome("Points");
//
//             var points = ternaryPlot.isQuaternary
//                 ? TernaryPlotUtility.EvenlyDistributedPoints3D(automaticPointIncrements)
//                 : TernaryPlotUtility.EvenlyDistributedPoints(automaticPointIncrements);
//
//             foreach (var point in points) {
//                 var sphere = gnome.AddPrimitive(PrimitiveType.Sphere);
//                 sphere.localScale = Vector3.one * sphereSize / 10; // Arbitrary, but 1 is way too big
//                 sphere.localPosition = ternaryPlot.CoordinatesToLocalPosition(point);
//             }
//
//             gnome.Purge(defer: true);
//         }
//     }
// }
