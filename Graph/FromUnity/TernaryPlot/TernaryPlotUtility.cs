// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
//
// namespace Primer.Graph
// {
//     public static class TernaryPlotUtility
//     {
//         public static List<float[]> EvenlyDistributedPoints(int steps, bool nudgeAwayFromZero = false)
//         {
//             if (steps < 1)
//                 throw new ArgumentException("EvenlyDistributedPoints needs at least one step");
//
//             var points = new List<float[]>();
//
//             // We need to iterate over two of the variables. Doesn't matter which.
//             // I chose b first because it's the horizontal direction, according to how we draw the graph.
//             // It goes from 0 to the max.
//             // Also, we're stepping by one to avoid float errors that can give negative numbers and make things weird.
//             for (var b = 0; b <= steps; b++) {
//                 // I chose c next, because it also has a direction according to how the graph is drawn. (a is at the origin)
//                 // It starts at 0 but stops before the total would get too high
//                 for (var c = 0; c <= steps - b; c++)
//                 {
//                     // a is what's left over
//                     // And we have to norm
//                     var provisionalPoint = new float[] {steps - b - c, b, c};
//                     points.Add(nudgeAwayFromZero
//                         ? NudgedNormalizedPoint(provisionalPoint)
//                         : NormalizedPoint(provisionalPoint));
//                 }
//             }
//             
//
//             return points;
//         }
//
//         public static List<float[]> EvenlyDistributedPoints3D(int steps, bool nudgeAwayFromZero = false)
//         {
//             if (steps < 1)
//                 throw new ArgumentException("EvenlyDistributedPoints needs at least one step");
//
//             var points = new List<float[]>();
//
//             for (var b = 0; b <= steps; b++) {
//                 for (var c = 0; c <= steps - b; c++) {
//                     for (var d = 0; d <= steps - b - c; d++)
//                     {
//                         var provisionalPoint = new float[] {steps - b - c - d, b, c, d};
//                         points.Add(nudgeAwayFromZero
//                             ? NudgedNormalizedPoint(provisionalPoint)
//                             : NormalizedPoint(provisionalPoint));
//                     }
//                 }
//             }
//
//             return points;
//         }
//
//         // This is a static method in this class because "normalize" means different things in different contexts.
//         // Seems good to make sure this is always used with that context in mind, rather than making it an array extension.
//         public static float[] NormalizedPoint(params float[] point)
//         {
//             var sum = point.Sum();
//             var normalizedPoint = new float[point.Length];
//
//             for (var i = 0; i < point.Length; i++) {
//                 normalizedPoint[i] = point[i] / sum;
//             }
//
//             return normalizedPoint;
//         }
//
//         public static float[] NudgedNormalizedPoint(float[] point)
//         {
//             var floatArray = new float[point.Length];
//             for (int i = 0; i < point.Length; i++) 
//             {
//                 if (point[i] == 0) {floatArray[i] = 1;}
//                 else floatArray[i] = point[i] * 100; // Make everything else big by comparison
//             }
//             return NormalizedPoint(floatArray);
//         }
//
//         public static float[] AddFloatArrays(float[] thisOne, float[] thatOne)
//         {
//             if (thisOne.Length != thatOne.Length)
//             {
//                 throw new ArgumentException("Arrays must be the same length to add them");
//             }
//
//             var sum = new float[thisOne.Length];
//             for (int i = 0; i < thisOne.Length; i++)
//             {
//                 sum[i] = thisOne[i] + thatOne[i];
//             }
//
//             return sum;
//         }
//         
//         public static float[] SubtractFloatArrays(float[] thisOne, float[] thatOne)
//         {
//             if (thisOne.Length != thatOne.Length)
//             {
//                 throw new ArgumentException("Arrays must be the same length to add them");
//             }
//
//             var sum = new float[thisOne.Length];
//             for (int i = 0; i < thisOne.Length; i++)
//             {
//                 sum[i] = thisOne[i] - thatOne[i];
//             }
//
//             return sum;
//         }
//
//         public static float[] DivideFloatArray(float[] thisOne, float divisor)
//         {
//             var result = new float[thisOne.Length];
//             for (int i = 0; i < thisOne.Length; i++)
//             {
//                 result[i] = thisOne[i] / divisor;
//             }
//
//             return result;
//         }
//     }
// }
