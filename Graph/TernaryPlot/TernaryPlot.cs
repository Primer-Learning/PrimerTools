// using System;
// using System.Linq;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// namespace Primer.Graph
// {
//     [ExecuteAlways]
//     public class TernaryPlot : MonoBehaviour
//     {
//         #region public float edgeThickness;
//         [SerializeField, HideInInspector]
//         private float _edgeThickness = 0.01f;
//
//         [ShowInInspector]
//         public float edgeThickness {
//             get => _edgeThickness;
//             set {
//                 _edgeThickness = value;
//                 DrawBorders();
//             }
//         }
//         #endregion
//
//         #region public bool isQuaternary;
//         [SerializeField, HideInInspector]
//         private bool _isQuaternary;
//
//         [ShowInInspector]
//         public bool isQuaternary {
//             get => _isQuaternary;
//             set {
//                 _isQuaternary = value;
//                 DrawBorders();
//             }
//         }
//         #endregion
//
//         [Button]
//         public void Reset()
//         {
//             DrawBorders();
//             Clear();
//         }
//
//         public Gnome GetContentGnome(string id = "Content")
//         {
//             return transform.FindOrCreate(id).ToGnome();
//         }
//
//         public void Clear()
//         {
//             var contentGnome = GetContentGnome();
//             contentGnome.Purge();
//         }
//
//         public Vector3 CoordinatesToLocalPosition(float a, float b, float c)
//         {
//             return transform.TransformPoint(CoordinatesToPosition(a, b, c));
//         }
//
//         public Vector3 CoordinatesToLocalPosition(float a, float b, float c, float d)
//         {
//             return transform.TransformPoint(CoordinatesToPosition(a, b, c, d));
//         }
//
//         public Vector3 CoordinatesToLocalPosition(float[] coords)
//         {
//             return transform.TransformPoint(CoordinatesToPosition(coords));
//         }
//
//         public static Vector3 CoordinatesToPosition(float[] coords)
//         {
//             return coords.Length switch {
//                 3 => CoordinatesToPosition(coords[0], coords[1], coords[2]),
//                 4 => CoordinatesToPosition(coords[0], coords[1], coords[2], coords[3]),
//                 _ => throw new ArgumentException($"Ternary plot requires 3 or 4 coordinates but received {coords.Length}"),
//             };
//         }
//
//         public static Vector3 CoordinatesToPosition(float a, float b, float c)
//         {
//             ValidateCoordinates(a, b, c);
//
//             return new Vector3(
//                 x: b + c / 2,
//                 y: c * Mathf.Sqrt(3) / 2,
//                 z: 0
//             );
//         }
//
//         public static Vector3 CoordinatesToPosition(float a, float b, float c, float d)
//         {
//             ValidateCoordinates(a, b, c, d);
//
//             return new Vector3(
//                 x: b + c / 2 + d / 2,
//                 y: c * Mathf.Sqrt(3) / 2 + d * (1 / (2 * Mathf.Sqrt(3))),
//                 z: -d * Mathf.Sqrt(2 / 3f)
//             );
//         }
//
//         private static void ValidateCoordinates(params float[] coords)
//         {
//             var sum = coords.Sum();
//
//             if (Mathf.Abs(sum - 1) > 0.00001f)
//                 Debug.LogWarning($"Sum of coordinates must be 1 but it's {sum}");
//
//             if (coords.Any(x => x < 0))
//                 Debug.LogWarning($"Coordinates must all be positive, but they are {coords.Join(", ")}");
//         }
//
//         private void DrawBorders()
//         {
//             if (gameObject.IsPreset())
//                 return;
//
//             var edgeScale = new Vector3(edgeThickness, 0.5f, edgeThickness);
//             var vertexScale = Vector3.one * (edgeThickness * 1.02f);
//             var borders = transform.FindOrCreate("Borders").ToGnome();
//
//             var bottom = borders.AddPrimitive(PrimitiveType.Cylinder, "Bottom line");
//             bottom.localPosition = CoordinatesToPosition(0.5f, 0.5f, 0);
//             bottom.localRotation = Quaternion.Euler(0, 0, 90);
//             bottom.localScale = edgeScale;
//
//             var left = borders.AddPrimitive(PrimitiveType.Cylinder, "Left line");
//             left.localPosition = CoordinatesToPosition(0.5f, 0, 0.5f);
//             left.localRotation = Quaternion.Euler(0, 0, -30);
//             left.localScale = edgeScale;
//
//             var right = borders.AddPrimitive(PrimitiveType.Cylinder, "Right line");
//             right.localPosition = CoordinatesToPosition(0, 0.5f, 0.5f);
//             right.localRotation = Quaternion.Euler(0, 0, 30);
//             right.localScale = edgeScale;
//
//             var a = borders.AddPrimitive(PrimitiveType.Sphere, "A");
//             a.localPosition = CoordinatesToPosition(1, 0, 0);
//             a.localScale = vertexScale;
//
//             var b = borders.AddPrimitive(PrimitiveType.Sphere, "B");
//             b.localPosition = CoordinatesToPosition(0, 1, 0);
//             b.localScale = vertexScale;
//
//             var c = borders.AddPrimitive(PrimitiveType.Sphere, "C");
//             c.localPosition = CoordinatesToPosition(0, 0, 1);
//             c.localScale = vertexScale;
//
//             if (isQuaternary) {
//                 var edge1 = borders.AddPrimitive(PrimitiveType.Cylinder, "3D Edge 1");
//                 edge1.localPosition = CoordinatesToPosition(0.5f, 0, 0, 0.5f);
//                 edge1.localRotation = Quaternion.Euler(109.47f, 0, 30);
//                 edge1.localScale = edgeScale;
//
//                 var edge2 = borders.AddPrimitive(PrimitiveType.Cylinder, "3D Edge 2");
//                 edge2.localPosition = CoordinatesToPosition(0, 0.5f, 0, 0.5f);
//                 edge2.localRotation = Quaternion.Euler(109.47f, 0, -30);
//                 edge2.localScale = edgeScale;
//
//                 var edge3 = borders.AddPrimitive(PrimitiveType.Cylinder, "3D Edge 3");
//                 edge3.localPosition = CoordinatesToPosition(0, 0, 0.5f, 0.5f);
//                 edge3.localRotation = Quaternion.Euler(54.73f, 0, 0);
//                 edge3.localScale = edgeScale;
//
//                 var d = borders.AddPrimitive(PrimitiveType.Sphere, "D");
//                 d.localPosition = CoordinatesToPosition(0, 0, 0, 1);
//                 d.localScale = vertexScale;
//             }
//
//             borders.Purge();
//         }
//     }
// }
