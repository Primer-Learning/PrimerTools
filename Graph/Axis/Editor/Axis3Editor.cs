// using Primer.Graph;
// using UnityEditor;
//
// [CustomEditor(typeof(Axis3))]
// public class Axis3Editor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         // Start change check
//         EditorGUI.BeginChangeCheck();
//         
//         // Draw the default inspector
//         DrawDefaultInspector();
//
//         // End change check
//         if (!EditorGUI.EndChangeCheck()) return;
//         
//         // Something has changed, update children
//         var axis = (Axis3)target;
//         axis.UpdateChildren();
//     }
// }