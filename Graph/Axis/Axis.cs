using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools.AnimationSequence;

namespace PrimerTools.Graph;

[Tool]
public partial class Axis : Node3D
{
	private ExportedMemberChangeChecker exportedMemberChangeChecker;
	// private AnimationPlayer animationPlayer;
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

	// public override void _Ready()
	// {
	// 	if (!Engine.IsEditorHint()) UpdateChildren(0);
	// }
	
	internal (Animation removeAnimation, Animation updateAnimation, Animation addAnimation) UpdateChildren(float duration = 0.5f)
	{
		if (min != 0)
		{
			GD.PrintErr("Idk how to deal with non-zero min yet.");
			return (null, null, null);
		}
		
		var (removeTics, updateTics, addTics) = UpdateTics(duration);

		return (
			AnimationUtilities.Parallel(removeTics),
			AnimationUtilities.Parallel(
				UpdateArrows(duration),
				UpdateRod(duration),
				updateTics
			),
			AnimationUtilities.Parallel(addTics)
		);
	}

	private Animation UpdateRod(float duration)
	{
		var rod = GetNode<Node3D>("Rod");

		return AnimationUtilities.Parallel(
			rod.MoveTo(new Vector3(-padding.X, 0f, 0f), duration),
			rod.ScaleTo(length == 0 
				? Vector3.Zero
				: new Vector3(length, thickness, thickness), duration)
		);
	}

	private Animation UpdateArrows(float duration)
	{
		var endArrow = GetNode<Node3D>("Head");
		var startArrow = GetNode<Node3D>("Tail");
		
		if (length == 0)
		{
			endArrow.Scale = Vector3.Zero;
			startArrow.Scale = Vector3.Zero;
		}

		return AnimationUtilities.Parallel(
			startArrow.MoveTo(new Vector3(-padding.X, 0f, 0f)),
			endArrow.MoveTo(new Vector3(length - padding.X, 0f, 0f))
		);
	}

	#region Tics
	internal bool transitionTicsAllTogether = false;
	[Export] public float ticStep = 2;
	public bool showZero;
	public int labelNumberOffset;
	[Export] public PackedScene ticScene;
	
	public int autoTicCount = 0;
	public List<TicData> manualTicks = new();

	private (Animation removeAnimation, Animation updateAnimation, Animation addAnimation) UpdateTics(float duration)
	{
		Vector3 GetPosition(AxisTic tic) => new(tic.data.value * scale, 0, 0);
		var ticsToRemove = GetChildren().Select(x => x as AxisTic).Where(x => x != null).ToList();
		var newTicAnimations = new List<Animation>();
		var ticMovementAnimations = new List<Animation>();
		
		var animation = new Animation();
		animation.Length = duration;
		
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
				tic.Owner = GetTree().EditedSceneRoot;
				tic.SceneFilePath = "";
				tic.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
				
				tic.Position = GetPosition(tic);
				tic.Scale = Vector3.Zero;
				newTicAnimations.Add(tic.ScaleTo(1, duration));
			}
			else
			{
				ticsToRemove.Remove(tic);
			}
			
			ticMovementAnimations.Add(tic.MoveTo(GetPosition(tic)));
			
			if (length == 0)
			{
				tic.Scale = Vector3.Zero;
			}
		}

		var ticRemovalAnimations = ticsToRemove.Select(tic => tic.ScaleTo(0, duration));

		return (
			AnimationUtilities.Parallel(ticRemovalAnimations.ToArray()),
			AnimationUtilities.Parallel(ticMovementAnimations.ToArray()),
			AnimationUtilities.Parallel(newTicAnimations.ToArray())
		);
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