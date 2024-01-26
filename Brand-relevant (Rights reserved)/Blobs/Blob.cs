using Godot;
using System;

namespace PrimerTools;

public partial class Blob : Node3D
{
	private static Random _classRng;
	private Rng rng;
	
	private AnimationTree _animationTree;
	public override void _Ready()
	{
		_animationTree = GetNode<AnimationTree>("AnimationTree");
		
		// Every blob should different rng, so we have a static rng object that generates seeds for the individual
		// blob rng objects.
		rng = new Rng(Rng.NextInt());
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//Random idle wiggles
		//Each blob will look different (if the rng seed is different)
		const float changeProbability = 0.003f;
		if (rng.RangeFloat(0, 1) < changeProbability)
		{
			var tween = GetTree().CreateTween();
			tween.TweenProperty(_animationTree, "parameters/turn", (rng.RangeFloat(0, 1) * 2 - 1), 0.5f);
		}
		// 	TiltHeadLeftRight((rng.RangeFloat(0, 1) * 2 - 1) * 2);
		// if (rng.RangeFloat(0, 1) < changeProbability)
		// 	TiltHeadFrontBack((rng.RangeFloat(0, 1) * 2 - 1) * 2);
		// if (rng.RangeFloat(0, 1) < changeProbability)
		// 	TurnHeadLeftRight((rng.RangeFloat(0, 1) * 2 - 1) * 2);

		// var blinkProbability = 0.002f;
		// if (rng.RangeFloat() < blinkProbability) Blink();
	}
}
