using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools.AnimationSequence;

namespace PrimerTools.Graph;

[Tool]
public partial class Axis : Node3D
{
	private ExportedMemberChangeChecker exportedMemberChangeChecker;
	private AnimationPlayer animationPlayer;
	private int animationsMade = 0;
	
	public float min = 0;
	[Export] private float Min {
		get => min;
		set => min = value;
	}
	public float max = 10;
	[Export] private float Max {
		get => max;
		set => max = value;
	}
	internal float length = 1;
	[Export] private float Length {
		get => length;
		set => length = Mathf.Max(0, value);
	}
	
	public float RangeSize => Mathf.Max(0.001f, max - min);
	
	private Vector2 padding = new (0.2f, 0.2f);
	[Export] public Vector2 Padding {
		get
		{
			if (padding.X + padding.Y > length)
				return new Vector2(length / 2, length / 2);
			return padding;
		}
		set => padding = value;
	}
	public float lengthMinusPadding => length - padding.X - padding.Y;
	float thickness = 1;
	public float scale => lengthMinusPadding / RangeSize;
	
	public override void _Process(double delta)
	{
		exportedMemberChangeChecker ??= new ExportedMemberChangeChecker(this);
		
		if (Engine.IsEditorHint() && exportedMemberChangeChecker.CheckForChanges())
		{
			UpdateChildren(0);
		}
	}

	public override void _Ready()
	{
		UpdateChildren(0);
	}
	
	internal Animation UpdateChildren(float duration = 0.5f)
	{
		if (min != 0)
		{
			GD.PrintErr("Idk how to deal with non-zero min yet.");
			return null;
		}

		UpdateArrows(duration);
		UpdateTics(duration);

		return CreateParallelAnimation(
			UpdateRod(duration)
		);
	}

	private Animation CreateParallelAnimation(params Animation[] animations)
	{
		// Make sure the animation player exists
		animationPlayer ??= GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		if (animationPlayer == null)
		{
			animationPlayer = new AnimationPlayer();
			animationPlayer.Name = "AnimationPlayer";
			AddChild(animationPlayer);
			animationPlayer.Owner = GetTree().EditedSceneRoot;
		}

		AnimationLibrary library;
		if (animationPlayer.HasAnimationLibrary("p"))
		{
			library = animationPlayer.GetAnimationLibrary("p");
		}
		else
		{
			library = new AnimationLibrary();
			animationPlayer.AddAnimationLibrary("p", library);
		}

		var combinedAnimation = new Animation();
		combinedAnimation.Length = animations.Select(x => x.Length).Max();

		// Add the animations to the library
		foreach (var animation in animations)
		{
			var name = $"anim{animationsMade}";
			library.AddAnimation(name, animation);
			
			var trackIndex = combinedAnimation.AddTrack(Animation.TrackType.Animation);
			combinedAnimation.TrackInsertKey(trackIndex, 0.0f, $"p/{name}");
			combinedAnimation.TrackSetPath(trackIndex, $"{animationPlayer.GetPath()}:animation");
		
			animationsMade++;
		}

		return combinedAnimation;
	}

	private Animation UpdateRod(float duration)
	{
		var rod = GetNode<Node3D>("Rod");
		var newPosition = new Vector3(-padding.X, 0f, 0f);
		var newRodScale = length == 0 
			? Vector3.Zero
			: new Vector3(length, thickness, thickness);

		var animation = new Animation();
		animation.Length = duration;
		
		var trackIndex = animation.AddTrack(Animation.TrackType.Scale3D);
		animation.ScaleTrackInsertKey(trackIndex, 0.0f, rod.Scale);
		animation.ScaleTrackInsertKey(trackIndex, duration, newRodScale);
		animation.TrackSetPath(trackIndex, $"Rod");
		
		trackIndex = animation.AddTrack(Animation.TrackType.Position3D);
		animation.PositionTrackInsertKey(trackIndex, 0.0f, rod.Position);
		animation.PositionTrackInsertKey(trackIndex, duration, newPosition);
		animation.TrackSetPath(trackIndex, $"Rod");
		
		rod.Position = newPosition;
		rod.Scale = newRodScale;
		return animation;
	}

	private void UpdateArrows(float duration)
	{
		var endArrow = GetNode<Node3D>("Head");
		// endArrow.localRotation = Quaternion.Euler(0f, 90f, 0f);
		endArrow.Position = new Vector3(length - padding.X, 0f, 0f);
		// var endArrowMove = endArrowPos == endArrow.localPosition ? Tween.noop : endArrow.MoveTo(endArrowPos);
		// var endArrowScale arrowPresence == ArrowPresence.Neither
		// 	? endArrow.localScale == Vector3.zero ? Tween.noop : endArrow.ScaleTo(0)
		// 	: endArrow.localScale == Vector3.one * 0.07f ? Tween.noop : endArrow.ScaleTo(0.07f);
		// var endArrowTween = Tween.Parallel(endArrowMove, endArrowScale);
		
		var startArrow = GetNode<Node3D>("Tail");
		// endArrow.localRotation = Quaternion.Euler(0f, 90f, 0f);
		startArrow.Position = new Vector3(-padding.X, 0f, 0f);
		// var endArrowMove = endArrowPos == endArrow.localPosition ? Tween.noop : endArrow.MoveTo(endArrowPos);
		// var endArrowScale arrowPresence == ArrowPresence.Neither
		// 	? endArrow.localScale == Vector3.zero ? Tween.noop : endArrow.ScaleTo(0)
		// 	: endArrow.localScale == Vector3.one * 0.07f ? Tween.noop : endArrow.ScaleTo(0.07f);
		// var endArrowTween = Tween.Parallel(endArrowMove, endArrowScale);
		
		if (length == 0)
		{
			endArrow.Scale = Vector3.Zero;
			startArrow.Scale = Vector3.Zero;
		}
	}

	#region Tics
	internal bool transitionTicsAllTogether = false;
	[Export] public float ticStep = 2;
	public bool showZero;
	public int labelNumberOffset;
	[Export] public PackedScene ticScene;
	
	public int autoTicCount = 0;
	public List<TicData> manualTicks = new();

	private void UpdateTics(float duration)
	{
		foreach (var tic in GetChildren())
		{
			if (tic is AxisTic axisTic)
				axisTic.Free();
		}

		Vector3 GetPosition(AxisTic tic) => new(tic.data.value * scale, 0, 0);

		var ticsToRemove = GetChildren().Select(x => x as AxisTic).Where(x => x != null).ToList();
		
		foreach (var data in PrepareTics())
		{
			var name = $"Tic {data.label}";
			var tic = GetNodeOrNull<AxisTic>(name);

			if (tic == null)
			{
				tic = ticScene.Instantiate<AxisTic>();
				tic.data = data;
				tic.Name = name;
				tic.SetLabel();
				AddChild(tic);
			}
			else
			{
				ticsToRemove.Remove(tic);
			}
			
			tic.Position = GetPosition(tic);
			
			if (length == 0)
			{
				tic.Scale = Vector3.Zero;
			}
		}

		foreach (var tic in ticsToRemove)
		{
			tic.Free();
		}
	}
	
	private void UpdateTicStep()
	{
		if (autoTicCount <= 0)
			return;
            
		// This looks at the existing tic step because it's meant to avoid destroying existing tics 
		// as much as possible.
		while (max / ticStep > autoTicCount)
		{
			switch (ticStep.ToString()[0])
			{
				case '1':
					ticStep *= 2;
					break;
				case '2':
					ticStep = Mathf.RoundToInt(ticStep * 2.5f);
					break;
				case '5':
					ticStep *= 2;
					break;
			}
		}
	}
	
	private List<TicData> CalculateTics()
	{
		UpdateTicStep(); // Looks at autoTicCount and adjusts ticStep accordingly.
            
		var calculated = new List<TicData>();

		if (showZero)
			calculated.Add(new TicData(0, labelNumberOffset));

		for (var i = Mathf.Max(ticStep, min); i <= max; i += ticStep)
			calculated.Add(new TicData(i, labelNumberOffset));

		for (var i = Mathf.Min(-ticStep, max); i >= min; i -= ticStep)
			calculated.Add(new TicData(i, labelNumberOffset));

		return calculated;
	}
	
	private List<TicData> PrepareTics()
	{
		if (manualTicks.Count > 0)
			return manualTicks;
            
		if (ticStep <= 0)
			return new List<TicData>();

		return CalculateTics();
	}
	
	[Tool]
	public class TicData
	{
		public float value;
		public string label;

		public TicData(float value, int labelOffset) {
			this.value = value;
			label = (value + labelOffset).FormatNumberWithDecimals();
		}
		public TicData(float value, string label) {
			this.value = value;
			this.label = label;
		}
	}
	#endregion
}