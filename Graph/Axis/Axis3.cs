// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Security.Cryptography;
// using System.Threading.Tasks;
// using Codice.CM.Common.Tree;
// using Primer.Animation;
// using Primer.Latex;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// namespace Primer.Graph
// {
//     public class Axis3 : MonoBehaviour
//     {
//         public float min = 0;
//         public float max = 10;
//         public float rangeSize => Mathf.Max(0.001f, max - min);
//         
//         public int autoTicCount = 0;
//         public Transform ticsContainer => transform.Find("Tics container");
//         
//         
//         public float length = 1;
//         [SerializeField]
//         private Vector2 _padding = new (0.2f, 0.2f);
//         public Vector2 padding {
//             get
//             {
//                 if (_padding.x + _padding.y > length)
//                     return new Vector2(length / 2, length / 2);
//                 return _padding;
//             }
//             set => _padding = value;
//         }
//         
//         public float scale => lengthMinusPadding / rangeSize;
//
//         public float lengthMinusPadding => length - padding.x - padding.y;
//         float thickness = 1;
//
//         internal (Tween removeTween, Tween updateTween, Tween addTween) PrepareTransitionParts()
//         {
//             var (removeTics, updateTics, addTics) = TransitionTics();
//             var (removeLabel, updateLabel, addLabel) = TransitionLabel();
//
//             var removeTween = Tween.Parallel(
//                 removeTics,
//                 removeLabel
//             );
//             // Rod, arrow, label transitions
//             var updateTween = Tween.Parallel(
//                 updateTics,
//                 TransitionRod(),
//                 TransitionArrows(),
//                 updateLabel
//             );
//             var addTween = Tween.Parallel(
//                 addTics,
//                 addLabel
//             );
//
//             return
//             (
//                 removeTween,
//                 updateTween,
//                 addTween
//             );
//         }
//
//         private Tween Transition(bool allowZeroLength = false)
//         {
//             var (removeTween, updateTween, addTween) = PrepareTransitionParts();
//             if (length == 0 && !allowZeroLength) return Tween.noop;
//             return Tween.Series(
//                 removeTween,
//                 updateTween,
//                 addTween
//             );
//         }
//
//         internal Tween Appear()
//         {
//             var targetLength = length;
//             length = 0;
//             Transition(allowZeroLength: true).Apply();
//             ScaleDownTics().Apply();
//             ScaleDownArrows().Apply();
//             ScaleDownLabel().Apply();
//             transform.Find("Rod").localScale = Vector3.zero;
//
//             if (targetLength == 0)
//                 return Tween.noop;
//
//             length = targetLength;
//             var (_, updateTweens, addTweens) = PrepareTransitionParts();
//             return Tween.Series(
//                 updateTweens,
//                 addTweens
//             );
//         }
//         internal Tween Disappear()
//         {
//             if (length == 0) return Tween.noop;
//             length = 0;
//             var (removeTweens, updateTweens, _) = PrepareTransitionParts();
//         
//             return Tween.Series(
//                 Tween.Parallel(
//                     removeTweens,
//                     ScaleDownTics(),
//                     ScaleDownLabel()
//                 ),
//                 Tween.Parallel(
//                     updateTweens,
//                     ScaleDownArrows()
//                 )
//             );
//         }
//
//         #region Rod
//         private Tween TransitionRod()
//         {
//             var rod = new Primer.SimpleGnome("Rod", parent: transform);
//             var position = new Vector3(-padding.x, 0f, 0f);
//             var rodScale = length == 0 
//                 ? Vector3.zero
//                 : new Vector3(length, thickness, thickness);
//
//             DrawBar(rod);
//
//             return Tween.Parallel(
//                 rod.transform.localPosition == position ? Tween.noop : rod.MoveTo(position),
//                 rod.transform.localScale == rodScale ? Tween.noop : rod.ScaleTo(rodScale)
//             );
//         }
//
//         private static void DrawBar(Primer.SimpleGnome rod)
//         {
//             var cylinder = rod.Add<Transform>("AxisRod", "AxisRod");
//             cylinder.localPosition = new Vector3(0.5f, 0, 0);
//             cylinder.localRotation = Quaternion.Euler(0, 0, -90);
//             cylinder.localScale = new Vector3(0.0375f, 0.5f, 0.0375f);
//         }
//         #endregion
//
//         #region Arrows
//         [Title("Arrows")]
//         public ArrowPresence arrowPresence = ArrowPresence.Both;
//         public GameObject arrowPrefab;
//
//         private Tween TransitionArrows()
//         {
//             var gnome = new Primer.SimpleGnome(transform);
//             
//             var endArrow = gnome.Add<Transform>(arrowPrefab, "End Arrow");
//             endArrow.localRotation = Quaternion.Euler(0f, 90f, 0f);
//             var endArrowPos = new Vector3(length - padding.x, 0f, 0f);
//             var endArrowMove = endArrowPos == endArrow.localPosition ? Tween.noop : endArrow.MoveTo(endArrowPos);
//             var endArrowScale = arrowPresence == ArrowPresence.Neither
//                 ? endArrow.localScale == Vector3.zero ? Tween.noop : endArrow.ScaleTo(0)
//                 : endArrow.localScale == Vector3.one * 0.07f ? Tween.noop : endArrow.ScaleTo(0.07f);
//             var endArrowTween = Tween.Parallel(endArrowMove, endArrowScale);
//
//             var originArrow = gnome.Add<Transform>(arrowPrefab, "Origin Arrow");
//             originArrow.localRotation = Quaternion.Euler(0f, -90f, 0f);
//             var originArrowPos = new Vector3(-padding.x, 0f, 0f);
//             var originArrowMove = originArrowPos == originArrow.localPosition ? Tween.noop : originArrow.MoveTo(originArrowPos);
//             var originArrowScale = arrowPresence != ArrowPresence.Both
//                 ? originArrow.localScale == Vector3.zero ? Tween.noop : originArrow.ScaleTo(0)
//                 : originArrow.localScale == Vector3.one * 0.07f ? Tween.noop : originArrow.ScaleTo(0.07f);
//             var originArrowTween = Tween.Parallel(originArrowMove, originArrowScale);
//
//             return Tween.Parallel(endArrowTween, originArrowTween);
//         }
//
//         private Tween ScaleDownArrows()
//         {
//             // Find object named "End Arrow" and scale it down if it is found.
//             var endArrow = transform.Find("End Arrow");
//             var endArrowTween = transform.Find("End Arrow") is null ? Tween.noop : endArrow.ScaleTo(0);
//             var originArrow = transform.Find("Origin Arrow");
//             var originArrowTween = transform.Find("Origin Arrow") is null ? Tween.noop : originArrow.ScaleTo(0);
//
//             return Tween.Parallel(endArrowTween, originArrowTween);
//         }
//
//         #endregion
//
//         #region Label
//
//         public const float X_OFFSET = 0.4f;
//
//         [Title("Label")]
//         public bool showLabel = true;
//
//         [EnableIf(nameof(showLabel))]
//         public string label = "Label";
//
//         public float labelSize = 1;
//         
//         [EnableIf(nameof(showLabel))]
//         public Vector3 labelOffset = Vector3.zero;
//
//         [EnableIf(nameof(showLabel))]
//         public Quaternion labelRotation = Quaternion.identity;
//
//         [EnableIf(nameof(showLabel))]
//         public AxisLabelPosition labelPosition = AxisLabelPosition.End;
//
//         private (Tween removeLabel, Tween updateLabel, Tween addLabel) TransitionLabel()
//         {
//             var labelTransform = transform.Find("Label");
//             var newLabel = false;
//             
//             if (labelTransform == null)
//             {
//                 newLabel = true;
//                 var gnome = new SimpleGnome(transform);
//                 labelTransform = gnome.AddLatex(label, "Label").transform;
//             }
//
//             var pos = labelOffset + (labelPosition switch {
//                 AxisLabelPosition.Along => new Vector3(length / 2, -0.65f, 0f),
//                 AxisLabelPosition.End => new Vector3(length - padding.x + X_OFFSET, 0f, 0f),
//                 _ => Vector3.zero,
//             });
//
//             var updateLabel = Tween.Parallel(
//                 pos == labelTransform.localPosition ? Tween.noop : labelTransform.MoveTo(pos),
//                 labelRotation == labelTransform.localRotation ? Tween.noop : labelTransform.RotateTo(labelRotation)
//             );
//
//             if (newLabel)
//             {
//                 updateLabel.Apply();
//                 labelTransform.localScale = Vector3.zero;
//             }
//
//             var addLabel = showLabel 
//                 ? labelTransform.localScale == 0.2f * labelSize * Vector3.one ? Tween.noop : labelTransform.ScaleTo(0.2f * labelSize) 
//                 : Tween.noop;
//             var removeLabel = showLabel 
//                 ? Tween.noop 
//                 : labelTransform.localScale == Vector3.zero ? Tween.noop : labelTransform.ScaleTo(0f);
//
//             return (removeLabel, updateLabel, addLabel);
//         }
//         private Tween ScaleDownLabel()
//         {
//             var gnome = new SimpleGnome(transform);
//             var labelTransform = gnome.AddLatex(label, "Label").transform;
//             return labelTransform.ScaleTo(0);
//         }
//
//         #endregion
//
//         #region Tics
//
//         [DisableIf("@manualTicks.Count != 0")]
//         internal bool transitionTicsAllTogether = false;
//         public float ticStep = 2;
//         public bool showZero;
//         public int labelNumberOffset;
//         public AxisTic ticPrefab;
//         
//         public List<TicData> manualTicks;
//         
//         private List<TicData> PrepareTics()
//         {
//             if (manualTicks.Count > 0)
//                 return manualTicks;
//             
//             if (ticStep <= 0)
//                 return new List<TicData>();
//
//             return CalculateTics();
//         }
//
//         private void UpdateTicStep()
//         {
//             if (autoTicCount <= 0)
//                 return;
//             
//             // This looks at the existing tic step because it's meant to avoid destroying existing tics 
//             // as much as possible.
//             while (max / ticStep > autoTicCount)
//             {
//                 switch (ticStep.ToString()[0])
//                 {
//                     case '1':
//                         ticStep *= 2;
//                         break;
//                     case '2':
//                         ticStep = Mathf.RoundToInt(ticStep * 2.5f);
//                         break;
//                     case '5':
//                         ticStep *= 2;
//                         break;
//                 }
//             }
//         }
//
//         
//         private List<TicData> CalculateTics()
//         {
//             UpdateTicStep(); // Looks at autoTicCount and adjusts ticStep accordingly.
//             
//             var domain = this;
//             var calculated = new List<TicData>();
//
//             if (showZero)
//                 calculated.Add(new TicData(0, labelNumberOffset));
//
//             for (var i = Mathf.Max(ticStep, domain.min); i <= domain.max; i += ticStep)
//                 calculated.Add(new TicData(i, labelNumberOffset));
//
//             for (var i = Mathf.Min(-ticStep, domain.max); i >= domain.min; i -= ticStep)
//                 calculated.Add(new TicData(i, labelNumberOffset));
//
//             return calculated;
//         }
//         private (Tween remove, Tween update, Tween add) TransitionTics()
//         {
//             var parentGnome = new Primer.SimpleGnome(transform);
//             var gnomeTransform = parentGnome
//                 .Add("Tics container");
//             gnomeTransform.localRotation = Quaternion.identity;
//             var gnome = new Primer.SimpleGnome(gnomeTransform);
//
//             var addTweens = new List<Tween>();
//             var updateTweens = new List<Tween>();
//
//             Vector3 GetPosition(AxisTic tic) => new(tic.value * scale, 0, 0);
//
//             var ticsToRemove = new List<Transform>(gnome.activeChildren.ToList());
//             
//             foreach (var data in PrepareTics()) {
//                 var ticName = $"Tic {data.label}";
//                 var existingTic = gnome.transform.Find(ticName);
//                 AxisTic tic;
//
//                 // If the tic exists and is active already
//                 if (existingTic is not null && existingTic.localScale.x > 0)
//                 {
//                     ticsToRemove.Remove(existingTic);
//                     tic = existingTic.GetComponent<AxisTic>();
//                     var t = tic.transform;
//                     t.localRotation = Quaternion.identity;
//                     updateTweens.Add(t.localPosition == GetPosition(tic) ? Tween.noop : tic.MoveTo(GetPosition(tic)));
//                     // Probably already scale 1, but not always. For example, if we are calling Appear.
//                     addTweens.Add(t.localScale == Vector3.one ? Tween.noop : tic.ScaleTo(1));
//                 }
//                 else // The tic should exist but doesn't
//                 {
//                     tic = gnome.Add<AxisTic>(ticPrefab.gameObject, ticName);
//                     tic.value = data.value;
//                     tic.label = data.label;
//                     var t = tic.transform;
//                     t.localPosition = GetPosition(tic);
//                     t.localRotation = Quaternion.identity;
//                     t.SetScale(0);
//                     addTweens.Add(t.localScale == Vector3.one ? Tween.noop : tic.ScaleTo(1));
//                 }
//             }
//
//             bool disappearEarly =
//                 ticsToRemove.Select(x => x.GetComponent<AxisTic>().value).Any(x => x < min || x > max);
//
//             var removeTweens = ticsToRemove
//                 .Select(x => x.GetComponent<AxisTic>())
//                 .Select(
//                     tic => {
//                         if (disappearEarly) return tic.transform.localScale == Vector3.zero ? Tween.noop : tic.ScaleTo(0);
//                         updateTweens.Add(
//                             Tween.Parallel(
//                                 tic.transform.localPosition == GetPosition(tic) ? Tween.noop : tic.MoveTo(GetPosition(tic)),
//                                 tic.transform.localScale == Vector3.zero ? Tween.noop : tic.ScaleTo(0)
//                             )
//                         );
//                         return Tween.noop;
//                     }
//                 );
//
//             if (transitionTicsAllTogether) {
//                 return (
//                     Tween.noop,
//                     Tween.Parallel(
//                         removeTweens.RunInParallel(),
//                         updateTweens.RunInParallel(),
//                         addTweens.RunInParallel()
//                     ),
//                     Tween.noop
//                 );
//             }
//             
//             return (
//                 remove: removeTweens.RunInParallel(),
//                 update: updateTweens.RunInParallel(),
//                 add: addTweens.RunInParallel()
//             );  
//         }
//
//         private Tween ScaleDownTics()
//         {
//             if (ticsContainer is null)
//                 return Tween.noop;
//             
//             var tics = ticsContainer.GetChildren().Select(x => x.GetComponent<AxisTic>());
//             return tics.Select(x => x.ScaleTo(0)).RunInParallel();
//         }
//         
//         [Serializable]
//         public class TicData
//         {
//             public float value;
//             public string label;
//
//             public TicData(float value, int labelOffset) {
//                 this.value = value;
//                 label = (value + labelOffset).FormatNumberWithDecimals();
//             }
//             public TicData(float value, string label) {
//                 this.value = value;
//                 this.label = label;
//             }
//         }
//
//         #endregion
//         
//         public void UpdateChildren()
//         {
//             using var tween = Transition();
//             tween?.Apply();
//         }
//     }
// }