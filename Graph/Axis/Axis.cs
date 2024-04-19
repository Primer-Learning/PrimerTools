using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools.AnimationSequence;

namespace PrimerTools.Graph;

[Tool]
public partial class Axis : Node3D
{
	private ExportedMemberChangeChecker exportedMemberChangeChecker;
	private int animationsMade = 0;

	private const float ArrowHeadScaleFactor = 0.07f;
	
	[Export] public float Min = 0;
	// [Export] private float Min {
	// 	get => min;
	// 	set => min = value;
	// }
	[Export] public float Max = 10;
	// [Export] private float Max {
	// 	get => max;
	// 	set => max = value;
	// }
	internal float length = 1;
	[Export] private float Length {
		get => length;
		set => length = Mathf.Max(0, value);
	}
	
	public float RangeSize => Mathf.Max(0.001f, Max - Min);
	
	private Vector2 padding = new (0.2f, 0.2f);
	[Export] public Vector2 Padding {
		get => padding;
		set
		{
			if (value.X + value.Y > length)
				value =  new Vector2(length / 2, length / 2);
			padding = value;
		}
	}
	public float LengthMinusPadding => length - padding.X - padding.Y;
	[Export] public float Chonk = 1;
	public float DataSpaceScale => LengthMinusPadding / RangeSize;
	
	public bool ShowArrows = true;
	public bool ShowRod = true;
	
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
		if (Min != 0)
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
		if (!ShowRod) { rod.Visible = false; }
		
		GD.Print($"Padding is {Padding}");

		return AnimationUtilities.Parallel(
			rod.MoveTo(new Vector3(-Padding.X, 0f, 0f), duration: duration),
			rod.ScaleTo(length == 0 
				? Vector3.Zero
				: new Vector3(length, Chonk, Chonk), duration)
		);
	}

	private Animation UpdateArrows(float duration)
	{
		var endArrow = GetNode<Node3D>("Head");
		var startArrow = GetNode<Node3D>("Tail");
		if (!ShowArrows)
		{
			endArrow.Visible = false;
			startArrow.Visible = false;
		}

		endArrow.Scale = Vector3.One * Chonk * ArrowHeadScaleFactor;
		startArrow.Scale = Vector3.One * Chonk * ArrowHeadScaleFactor;
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
	internal bool TransitionTicsAllTogether = false;
	[Export] public float TicStep = 2;
	public bool ShowZero;
	public int LabelNumberOffset;
	public bool ShowTicCylinders = true;
	[Export] public PackedScene TicScene;
	
	public int AutoTicCount = 0;
	public List<TicData> ManualTicks = new();

	private (Animation removeAnimation, Animation updateAnimation, Animation addAnimation) UpdateTics(float duration)
	{
		Vector3 GetPosition(AxisTic tic) => new(tic.data.value * DataSpaceScale, 0, 0);
		
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
				tic = TicScene.Instantiate<AxisTic>();
				tic.data = data;
				tic.Name = name;
				AddChild(tic);
				tic.Owner = GetTree().EditedSceneRoot;
				tic.SceneFilePath = "";
				tic.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
				
				tic.Position = GetPosition(tic);
				tic.Scale = Vector3.Zero;
				newTicAnimations.Add(tic.ScaleTo(Chonk, duration));
				tic.SetLabel();
				if (!ShowTicCylinders) tic.GetNode<MeshInstance3D>("MeshInstance3D").Visible = false;
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
		if (AutoTicCount <= 0)
			return;
            
		// This looks at the existing tic step because it's meant to avoid destroying existing tics 
		// as much as possible.
		while (Max / TicStep > AutoTicCount)
		{
			switch (TicStep.ToString()[0])
			{
				case '1':
					TicStep *= 2;
					break;
				case '2':
					TicStep = Mathf.RoundToInt(TicStep * 2.5f);
					break;
				case '5':
					TicStep *= 2;
					break;
			}
		}
	}
	
	private List<TicData> CalculateTics()
	{
		UpdateTicStep(); // Looks at autoTicCount and adjusts ticStep accordingly.
            
		var calculated = new List<TicData>();
		if (length == 0) return calculated;

		if (ShowZero)
			calculated.Add(new TicData(0, LabelNumberOffset));

		for (var i = Mathf.Max(TicStep, Min); i <= Max; i += TicStep)
			calculated.Add(new TicData(i, LabelNumberOffset));

		for (var i = Mathf.Min(-TicStep, Max); i >= Min; i -= TicStep)
			calculated.Add(new TicData(i, LabelNumberOffset));

		return calculated;
	}
	
	private List<TicData> PrepareTics()
	{
		if (ManualTicks.Count > 0)
			return ManualTicks;
            
		if (TicStep <= 0)
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