using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools.LaTeX;
using PrimerTools.TweenSystem;

namespace PrimerTools.Graph;

[Tool]
public partial class Axis : Node3D
{
	private ExportedMemberChangeChecker exportedMemberChangeChecker;
	private int animationsMade = 0;

	private const float ArrowHeadScaleFactor = 0.07f;
	
	// Axis type enum
	public enum AxisType { X, Y, Z }
	[Export] public AxisType Type { get; set; }
	
	// Label properties
	[Export] private string _label = "";
	public string Label
	{
		get => _label;
		set
		{
			_label = value;
			// Free the previous node so it will be remade on transition
			if (IsInstanceValid(_labelNode))
			{
				_labelNode.Free();
			}
		}
	}
	[Export] public float LabelOffset = 1;
	[Export] public float LabelScale = 1;
	[Export] public Graph.AxisLabelAlignmentOptions LabelAlignment = Graph.AxisLabelAlignmentOptions.End;
	
	private LatexNode _labelNode;
	
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
	
	private float? ticLabelDistance = null;
	public float? TicLabelDistance {
		get => ticLabelDistance;
		set => ticLabelDistance = value;
	}
	
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
		// if (Min != 0)
		// {
		// 	GD.PrintErr("Idk how to deal with non-zero min yet.");
		// 	return (null, null, null);
		// }
		
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

		return AnimationUtilities.Parallel(
			rod.MoveToAnimation(new Vector3(-Padding.X + Min * DataSpaceScale, 0f, 0f), duration: duration),
			rod.ScaleToAnimation(length == 0 
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
			startArrow.MoveToAnimation(new Vector3(-padding.X + Min * DataSpaceScale, 0f, 0f)),
			endArrow.MoveToAnimation(new Vector3(length - padding.X + Min * DataSpaceScale, 0f, 0f))
		);
	}
	
	internal (CompositeStateChange Remove, CompositeStateChange Update, CompositeStateChange Add) UpdateChildrenStateChange(float duration = 0.5f)
	{
	    var (removeTics, updateTics, addTics) = UpdateTicsStateChange(duration);
	    var (removeLabel, updateLabel, addLabel) = UpdateLabelStateChange(duration);

	    var removeComposite = new CompositeStateChange().WithName($"{Name} Remove");
	    removeComposite.AddStateChange(removeTics);
	    removeComposite.AddStateChangeInParallel(removeLabel);

	    var updateComposite = new CompositeStateChange().WithName($"{Name} Update");
	    updateComposite.AddStateChange(UpdateArrowsStateChange(duration));
	    updateComposite.AddStateChangeInParallel(UpdateRodStateChange(duration));
	    updateComposite.AddStateChangeInParallel(updateTics);
	    updateComposite.AddStateChangeInParallel(updateLabel);

	    var addComposite = new CompositeStateChange().WithName($"{Name} Add");
	    addComposite.AddStateChange(addTics);
	    addComposite.AddStateChangeInParallel(addLabel);

	    return (removeComposite, updateComposite, addComposite);
	}

	private CompositeStateChange UpdateRodStateChange(float duration)
	{
	    var composite = new CompositeStateChange().WithName("Update Rod");
	    var rod = GetNode<Node3D>("Rod");

	    if (!ShowRod) { rod.Visible = false; }

	    composite.AddStateChange(rod.MoveTo(new Vector3(-Padding.X + Min * DataSpaceScale, 0f, 0f)).WithDuration(duration));
	    composite.AddStateChangeInParallel(
	        rod.ScaleTo(length == 0
	            ? Vector3.Zero
	            : new Vector3(length, Chonk, Chonk))
	        .WithDuration(duration)
	    );

	    return composite;
	}

	private CompositeStateChange UpdateArrowsStateChange(float duration)
	{
	    var composite = new CompositeStateChange().WithName("Update Arrows");
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

	    composite.AddStateChange(startArrow.MoveTo(new Vector3(-padding.X + Min * DataSpaceScale, 0f, 0f)).WithDuration(duration));
	    composite.AddStateChangeInParallel(endArrow.MoveTo(new Vector3(length - padding.X + Min * DataSpaceScale, 0f, 0f)).WithDuration(duration));

	    return composite;
	}

	private (CompositeStateChange remove, CompositeStateChange update, CompositeStateChange add) UpdateTicsStateChange(float duration)
	{
	    Vector3 GetPosition(AxisTic tic) => new(tic.Data.value * DataSpaceScale, 0, 0);

	    var ticsToRemove = GetChildren().Select(x => x as AxisTic).Where(x => x != null).ToList();
	    var removeComposite = new CompositeStateChange().WithName("Remove Tics");
	    var updateComposite = new CompositeStateChange().WithName("Update Tics");
	    var addComposite = new CompositeStateChange().WithName("Add Tics");

	    foreach (var data in PrepareTics())
	    {
	        var name = $"Tic {data.label}".ToValidNodeName();
	        var tic = GetNodeOrNull<AxisTic>(name);

	        if (tic == null)
	        {
	            tic = TicScene.Instantiate<AxisTic>();
	            tic.Data = data;
	            AddChild(tic);
	            tic.Name = name;
	            tic.Owner = GetTree().EditedSceneRoot;
	            tic.SceneFilePath = "";
	            tic.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);

	            tic.Position = GetPosition(tic);
	            tic.Scale = Vector3.Zero;
	            addComposite.AddStateChangeInParallel(tic.ScaleTo(Chonk).WithDuration(duration));
	            tic.SetLabel();
	            tic.SetLabelScale(0);
	            tic.SetLabelDistance(TicLabelDistance);
	            if (!ShowTicCylinders) tic.GetNode<MeshInstance3D>("MeshInstance3D").Visible
	= false;
	        }
	        else
	        {
	            tic.Data = data;
	            ticsToRemove.Remove(tic);
	        }

	        updateComposite.AddStateChangeInParallel(tic.MoveTo(GetPosition(tic)).WithDuration(duration));
	        updateComposite.AddStateChangeInParallel(tic.ScaleTo(Chonk).WithDuration(duration));

	        updateComposite.AddStateChangeInParallel(tic.AnimateLabelScaleStateChange(0.2f).WithDuration(duration));
        
	        if (TicLabelDistance.HasValue)
	        {
	            var labelDistanceChange = tic.AnimateLabelDistanceStateChange(TicLabelDistance);
	            if (labelDistanceChange != null)
	            {
	                updateComposite.AddStateChangeInParallel(labelDistanceChange.WithDuration(duration));
	            }
	        }

	        if (length == 0)
	        {
	            tic.Scale = Vector3.Zero;
	        }
	    }

	    foreach (var tic in ticsToRemove)
	    {
	        removeComposite.AddStateChangeInParallel(tic.ScaleTo(0).WithDuration(duration));
	    }

	    return (removeComposite, updateComposite, addComposite);
	}
	
	private (CompositeStateChange remove, CompositeStateChange update, CompositeStateChange add) UpdateLabelStateChange(float duration)
	{
		var removeComposite = new CompositeStateChange().WithName("Remove Label");
		var updateComposite = new CompositeStateChange().WithName("Update Label");
		var addComposite = new CompositeStateChange().WithName("Add Label");
		
		if (_labelNode is not null && IsInstanceValid(_labelNode))
		{
			// Update existing label
			var (position, rotation) = GetLabelTransform();
			updateComposite.AddStateChange(_labelNode.MoveTo(position).WithDuration(duration));
			updateComposite.AddStateChangeInParallel(_labelNode.RotateTo(rotation).WithDuration(duration));
			updateComposite.AddStateChangeInParallel(_labelNode.ScaleTo(Vector3.One * LabelScale).WithDuration(duration));
		}
		else if (!string.IsNullOrEmpty(_label))
		{
			GD.Print($"Label for {Type} exists");
			// Create new label
			_labelNode = LatexNode.Create(_label);
			UpdateLabelAlignmentSettings();
			
			var (position, rotation) = GetLabelTransform();
			_labelNode.Position = position;
			_labelNode.RotationDegrees = rotation;
			_labelNode.Scale = Vector3.Zero;
			AddChild(_labelNode);
			
			addComposite.AddStateChange(_labelNode.ScaleTo(Vector3.One * LabelScale).WithDuration(duration));
		}
		
		return (removeComposite, updateComposite, addComposite);
	}
	
	private void UpdateLabelAlignmentSettings()
	{
		if (_labelNode == null) return;
		
		if (Type == AxisType.X)
		{
			if (LabelAlignment == Graph.AxisLabelAlignmentOptions.End)
			{
				_labelNode.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Left;
				_labelNode.VerticalAlignment = LatexNode.VerticalAlignmentOptions.Center;
			}
			else if (LabelAlignment == Graph.AxisLabelAlignmentOptions.Along)
			{
				_labelNode.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Center;
				_labelNode.VerticalAlignment = LatexNode.VerticalAlignmentOptions.Center;
			} 
		}
		if (Type == AxisType.Y) // Both cases are the same at the moment, but still toying
		{
			if (LabelAlignment == Graph.AxisLabelAlignmentOptions.End)
			{
				_labelNode.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Center;
				_labelNode.VerticalAlignment = LatexNode.VerticalAlignmentOptions.Baseline;
			} 
			else if (LabelAlignment == Graph.AxisLabelAlignmentOptions.Along)
			{
				_labelNode.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Center;
				_labelNode.VerticalAlignment = LatexNode.VerticalAlignmentOptions.Baseline;
			}
				
		}
	}
	
	private (Vector3 position, Vector3 rotation) GetLabelTransform()
	{
		switch (Type)
		{
			case AxisType.X:
				return (
					LabelAlignment == Graph.AxisLabelAlignmentOptions.Along
						? new Vector3(LengthMinusPadding / 2, -LabelOffset, 0)
						: new Vector3(LengthMinusPadding + LabelOffset, 0, 0),
					Vector3.Zero
				);
				
			case AxisType.Y: // The y-axis is rotated by 90 degrees
				return (
					LabelAlignment == Graph.AxisLabelAlignmentOptions.Along
						? new Vector3(-LabelOffset, LengthMinusPadding / 2, 0)
						: new Vector3(LengthMinusPadding + LabelOffset, 0, 0),
					LabelAlignment == Graph.AxisLabelAlignmentOptions.Along
						? new Vector3(0, 0, 90)
						: new Vector3(0, 0, -90)
				);
				
			case AxisType.Z:
				return (
					LabelAlignment == Graph.AxisLabelAlignmentOptions.Along
						? new Vector3(0, -LabelOffset, LengthMinusPadding / 2)
						: new Vector3(-12.5f, -4.5f, LengthMinusPadding),
					Vector3.Zero
				);
				
			default:
				return (Vector3.Zero, Vector3.Zero);
		}
	}

	#region Tics
	internal bool TransitionTicsAllTogether = false;
	[Export] public float TicStep = 2;
	public bool ShowZero;
	public int LabelNumberOffset;
	public bool ShowTicCylinders = true;
	[Export] public PackedScene TicScene;
	
	public int AutoTicCount = 0;
	public List<TicData> ManualTics = new();

	private (Animation removeAnimation, Animation updateAnimation, Animation addAnimation) UpdateTics(float duration)
	{
		Vector3 GetPosition(AxisTic tic) => new(tic.Data.value * DataSpaceScale, 0, 0);
		
		var ticsToRemove = GetChildren().Select(x => x as AxisTic).Where(x => x != null).ToList();
		var newTicAnimations = new List<Animation>();
		var ticMovementAnimations = new List<Animation>();
		
		var animation = new Animation();
		animation.Length = duration;
		
		foreach (var data in PrepareTics())
		{
			var name = $"Tic {data.label}".ToValidNodeName();
			var tic = GetNodeOrNull<AxisTic>(name);

			if (tic == null)
			{
				tic = TicScene.Instantiate<AxisTic>();
				tic.Data = data;
				AddChild(tic);
				tic.Name = name;
				tic.Owner = GetTree().EditedSceneRoot;
				tic.SceneFilePath = "";
				tic.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
				
				tic.Position = GetPosition(tic);
				tic.Scale = Vector3.Zero;
				newTicAnimations.Add(tic.ScaleToAnimation(Chonk, duration));
				tic.SetLabel();
				tic.SetLabelScale(0);
				tic.SetLabelDistance(TicLabelDistance);
				if (!ShowTicCylinders) tic.GetNode<MeshInstance3D>("MeshInstance3D").Visible = false;
			}
			else
			{
				tic.Data = data;
				ticsToRemove.Remove(tic);
			}
			
			ticMovementAnimations.Add(tic.MoveToAnimation(GetPosition(tic)));
			ticMovementAnimations.Add(tic.ScaleToAnimation(Chonk, duration));
			ticMovementAnimations.Add(tic.AnimateLabelScale(0.2f));
			if (TicLabelDistance.HasValue)
			{
				ticMovementAnimations.Add(tic.AnimateLabelDistance(TicLabelDistance));
			}
			
			if (length == 0)
			{
				tic.Scale = Vector3.Zero;
			}
		}

		var ticRemovalAnimations = ticsToRemove.Select(tic => tic.ScaleToAnimation(0, duration));

		return (
			AnimationUtilities.Parallel(ticRemovalAnimations.ToArray()),
			AnimationUtilities.Parallel(ticMovementAnimations.ToArray()),
			AnimationUtilities.Parallel(newTicAnimations.ToArray())
		);
	}

	private int GetLeadingDigit(float number)
	{
		var magnitude = Math.Pow(10, Math.Floor(Math.Log10(TicStep)));
		return (int)(number / magnitude);
	}
	
	private void UpdateTicStep()
	{
		if (AutoTicCount <= 0)
			return;
            
		// This looks at the existing tic step because it's meant to avoid destroying existing tics 
		// as much as possible.
		if (Max / TicStep > AutoTicCount)
		{
			while (Max / TicStep > AutoTicCount)
			{
				switch (GetLeadingDigit(TicStep))
				{
					case 1:
						TicStep *= 2;
						break;
					case 2:
						TicStep = Mathf.RoundToInt(TicStep * 2.5f);
						break;
					case 5:
						TicStep *= 2;
						break;
				}
			}
		}
		else
		{
			// Then check if we need to decrease the tic step (when Max is decreasing)
			while (Max / TicStep * 2.5 < AutoTicCount && TicStep > 1)
			{
				switch (GetLeadingDigit(TicStep))
				{
					case 1:
						TicStep = Mathf.Round(TicStep / 2);
						break;
					case 2:
						TicStep = Mathf.Round(TicStep / 2);
						break;
					case 5:
						TicStep = Mathf.Round(TicStep / 2.5f);
						break;
				}
				
				// Safety check to prevent going below 1
				if (TicStep < 1)
					TicStep = 1;
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
		if (ManualTics.Count > 0)
			return ManualTics;
            
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
