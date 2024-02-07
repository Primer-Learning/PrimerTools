using System.Collections.Generic;
using Godot;

namespace PrimerTools.Graph;

[Tool]
public partial class Axis : Node3D
{
	private MemberChangeChecker memberChangeChecker;
	
	[Export] public float min = 0;
	[Export] public float max = 10;
	internal float length = 1;
	[Export] public float Length {
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
		memberChangeChecker ??= new MemberChangeChecker(this);
		
		// This needs to work when played, so don't check for Engine.IsEditorHint()
		// I don't remember why that was checked for in the first place.
		// So documenting this here so I remember why I undid it.
		if (/*Engine.IsEditorHint() &&*/ memberChangeChecker.CheckForChanges())
		{
			UpdateChildren();
		}
	}

	public override void _Ready()
	{
		UpdateChildren();
	}

	private void UpdateRod()
	{
		var rod = GetNode<Node3D>("Rod");
		var position = new Vector3(-padding.X, 0f, 0f);
		var rodScale = length == 0 
			? Vector3.Zero
			: new Vector3(length, thickness, thickness);

		rod.Position = position;
		rod.Scale = rodScale;
	}

	private void UpdateArrows()
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
	
	internal void UpdateChildren()
	{
		if (min != 0)
		{
			GD.PrintErr("Idk how to deal with non-zero min yet.");
			return;
		}
		UpdateRod();
		UpdateArrows();
		UpdateTics();
	}

	#region Tics
	internal bool transitionTicsAllTogether = false;
	[Export] public float ticStep = 2;
	public bool showZero;
	public int labelNumberOffset;
	[Export] public PackedScene ticScene;
	
	public int autoTicCount = 0;
	public List<TicData> manualTicks = new();

	private void UpdateTics()
	{
		foreach (var tic in GetChildren())
		{
			if (tic is AxisTic axisTic)
				axisTic.Free();
		}

		Vector3 GetPosition(AxisTic tic) => new(tic.data.value * scale, 0, 0);

		foreach (var data in PrepareTics())
		{
			var tic = ticScene.Instantiate<AxisTic>();
			tic.data = data;
			tic.Name = $"Tic {data.label}";
			tic.SetLabel();
			AddChild(tic);
			tic.Position = GetPosition(tic);
			
			if (length == 0)
			{
				tic.Scale = Vector3.Zero;
			}
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