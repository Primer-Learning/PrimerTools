// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
//
// namespace Primer.Latex
// {
//     internal static class SvgToChars
//     {
//         private static readonly VectorUtils.TessellationOptions tessellationOptions = new() {
//             StepDistance = 100.0f,
//             MaxCordDeviation = 0.5f,
//             MaxTanAngleDeviation = 0.1f,
//             SamplingStepSize = 0.01f,
//         };
//
//         public static async Task<LatexChar[]> ConvertToSprites(string svg, CancellationToken ct)
//         {
//             ct.ThrowIfCancellationRequested();
//
//             SVGParser.SceneInfo sceneInfo;
//
//             try {
//                 sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
//             }
//             catch (Exception e) {
//                 Debug.LogError($"Invalid SVG, got error: {e}");
//                 return null;
//             }
//
//             var allGeometry = VectorUtils.TessellateScene(sceneInfo.Scene, tessellationOptions);
//
// #if UNITY_EDITOR
//             // Update normally gets called only sporadically in the editor
//             if (!Application.isPlaying)
//                 EditorApplication.QueuePlayerLoopUpdate();
// #endif
//
//             // VectorUtils.BuildSprite (in LatexChar) has to be called on the Player update loop
//             // we're already out of it since we waited for LaTeX to generate the SVG
//             // so we need to force another update and hook to it
//             // we can then use VectorUtils.BuildSprite without throwing
//             // "Not allowed to override geometry on sprite"
//             await UniTask.NextFrame(ct);
//
//             ct.ThrowIfCancellationRequested();
//             return ConvertGeometryToSprites(allGeometry);
//         }
//
//         private static LatexChar[] ConvertGeometryToSprites(IReadOnlyCollection<VectorUtils.Geometry> allGeometry)
//         {
//             var allVertices = allGeometry.SelectMany(geometry => geometry.TransformVertices());
//             var center = VectorUtils.Bounds(allVertices).center;
//             return allGeometry.Select(geometry => new LatexChar(geometry, center)).ToArray();
//         }
//     }
// }
