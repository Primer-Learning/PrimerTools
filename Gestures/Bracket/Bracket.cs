using Godot;
using PrimerTools;
using PrimerTools.TweenSystem;

[Tool]
public partial class Bracket : Node3D
{
	private MeshInstance3D LBar => GetNode<MeshInstance3D>("bracket/bracketBarMesh_Left");
	private MeshInstance3D RBar => GetNode<MeshInstance3D>("bracket/bracketBarMesh_Right");
	private MeshInstance3D LTip => GetNode<MeshInstance3D>("bracket/bracketTipMesh_Left");
	private MeshInstance3D RTip => GetNode<MeshInstance3D>("bracket/bracketTipMesh_Right");
	// Turns out these are never manipulated, but leaving them here in case we need to access them at some point. 
	// private MeshInstance3D LStem => GetNode<MeshInstance3D>("bracket/bracketCenterMesh_Left");
	// private MeshInstance3D RStem => GetNode<MeshInstance3D>("bracket/bracketCenterMesh_Right");

	// This one is just the local position. But we have this separate value so we can set it without updating position
	// immediately. Then the position can update along with the other values in transition.
	public Vector3 StemPosition;
	[Export]
	public Vector3 LeftTipPosition = new Vector3(-1, 0, 1);
	[Export]
	public Vector3 RightTipPosition = new Vector3(1, 0, 1);

	private ExportedMemberChangeChecker _exportedMemberChangeChecker;

	[Export]
	public bool EnableImmediateUpdates;
	public override void _Process(double delta)
	{
		if (!EnableImmediateUpdates) return;
		_exportedMemberChangeChecker ??= new ExportedMemberChangeChecker(this);
        
		if (Engine.IsEditorHint() && _exportedMemberChangeChecker.CheckForChanges())
		{
			UpdateImmediate();
		}
	}

	private (Quaternion rotation, float scale, float lLength, float rLength, float lBarLength, float rBarLength)
		CalculateUpdateParameters()
	{
		// PrimerGD.PrintWithStackTrace("Calculating update parameters");
		var toLPoint = LeftTipPosition - StemPosition;
		var toRPoint = RightTipPosition - StemPosition;
		var lToR = RightTipPosition - LeftTipPosition;
		var lHorizontal = (-toLPoint).Project(lToR);
		
		var stemToBaseline = toLPoint + lHorizontal;

		if (LeftTipPosition == RightTipPosition || stemToBaseline.LengthSquared() == 0)
		{
			return (Quaternion.Identity, 0, 0, 0, 0, 0);
		}
		
		var rotation = Basis.LookingAt(-stemToBaseline, LeftTipPosition.Cross(RightTipPosition)).GetRotationQuaternion();
		var scale = stemToBaseline.Length();
		var rLength = (toRPoint - stemToBaseline).Length() / scale;
		var lLength = (toLPoint - stemToBaseline).Length() / scale;
		
		// The numbers are weird here because I applied the bar's scale in blender.
		// Turns out it had a weird scale there because it was the actual dimensions.
		// So now scale 1 is weird dimensions, and we need to adjust. Or something. But it works great.
		var lBarLength = 1 + (lLength - 1) * 4.545f;
		var rBarLength = 1 + (rLength - 1) * 4.545f;
		if (lBarLength < 0 || rBarLength < 0) GD.PushWarning("One of the bracket bars has negative length. Probably going to look goofed.");

		return (rotation, scale, lLength, rLength, lBarLength, rBarLength);
	}
	
	public override void _Ready()
	{
		base._Ready();
		StemPosition = Position;
		LeftTipPosition = LTip.Position;
		RightTipPosition = RTip.Position;
	}

	public static Bracket CreateInstance()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Gestures/Bracket/bracket.tscn")
			.Instantiate<Bracket>();
	}

	public void UpdateImmediate()
	{
		var parameters = CalculateUpdateParameters();
		
		Position = StemPosition;
		Quaternion = parameters.rotation;
		Scale = Vector3.One * parameters.scale;
		
		LTip.Position = new Vector3(-parameters.lLength, 0, 1);
		RTip.Position = new Vector3(parameters.rLength, 0, 1);
		
		LBar.Scale = new Vector3(parameters.lBarLength, LBar.Scale.Y, LBar.Scale.Z);
		RBar.Scale = new Vector3(parameters.rBarLength, RBar.Scale.Y, RBar.Scale.Z);
	}

	public Animation Transition()
	{
		var parameters = CalculateUpdateParameters();

		return AnimationUtilities.Parallel(
			this.MoveToAnimation(StemPosition),
			this.RotateToAnimation(parameters.rotation),
			this.ScaleToAnimation(parameters.scale),
			LTip.MoveToAnimation(new Vector3(-parameters.lLength, 0, 1)),
			RTip.MoveToAnimation(new Vector3(parameters.rLength, 0, 1)),
			LBar.ScaleToAnimation(new Vector3(parameters.lBarLength, LBar.Scale.Y, LBar.Scale.Z)),
			RBar.ScaleToAnimation(new Vector3(parameters.rBarLength, RBar.Scale.Y, RBar.Scale.Z))
		);
	}
	
	public CompositeStateChange TransitionStateChange()
	{
		var parameters = CalculateUpdateParameters();

		var change = new CompositeStateChange();
		
		change.AddStateChangeInParallel(this.MoveTo(StemPosition));
		change.AddStateChangeInParallel(this.RotateTo(parameters.rotation));
		change.AddStateChangeInParallel(this.ScaleTo(parameters.scale));
		change.AddStateChangeInParallel(LTip.MoveTo(new Vector3(-parameters.lLength, 0, 1)));
		change.AddStateChangeInParallel(RTip.MoveTo(new Vector3(parameters.rLength, 0, 1)));
		change.AddStateChangeInParallel(LBar.ScaleTo(new Vector3(parameters.lBarLength, LBar.Scale.Y, LBar.Scale.Z)));
		change.AddStateChangeInParallel(RBar.ScaleTo(new Vector3(parameters.rBarLength, RBar.Scale.Y, RBar.Scale.Z)));

		return change;
	}

	public Animation ScaleUpFromZero(double duration = AnimationUtilities.DefaultDuration)
	{
		Transition();
		var scale = Scale;
		Scale = Vector3.Zero;
		return this.ScaleToAnimation(scale);
	}

	public Tween TweenTransition(double duration = AnimationUtilities.DefaultDuration)
	{
		if (Scale == Vector3.Zero) Scale = Vector3.One * 0.001f;
		var parameters = CalculateUpdateParameters();
		
		var tween = CreateTween();
		tween.SetParallel();
		tween.TweenProperty(
			this,
			"position",
			StemPosition,
			duration
		);
		tween.TweenProperty(
			this,
			"quaternion",
			parameters.rotation,
			duration
		);
		tween.TweenProperty(
			this,
			"scale",
			Vector3.One * parameters.scale,
			duration
		);
		tween.TweenProperty(
			LTip,
			"position",
			new Vector3(-parameters.lLength, 0, 1),
			duration
		);
		tween.TweenProperty(
			RTip,
			"position",
			new Vector3(parameters.rLength, 0, 1),
			duration
		);
		tween.TweenProperty(
			LBar,
			"scale",
			new Vector3(parameters.lBarLength, LBar.Scale.Y, LBar.Scale.Z),
			duration
		);
		tween.TweenProperty(
			RBar,
			"scale",
			new Vector3(parameters.rBarLength, RBar.Scale.Y, RBar.Scale.Z),
			duration
		);
		
		// Could probably just paste the above except Tween the changes in parallel.
		return tween;
	}
}