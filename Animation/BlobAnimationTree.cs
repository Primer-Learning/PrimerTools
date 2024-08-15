using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimerTools;

[Tool]
public partial class BlobAnimationTree : SelfBuildingAnimationTree
{
	private int _circleRadius = 300;
	
	protected override void Build()
	{
		base.Build();

		var stateMachine = new AnimationNodeStateMachine();
		stateMachine.SetNodePosition("Start", new Vector2(- 2 * _circleRadius, 0));
		stateMachine.SetGraphOffset(new Vector2(- 2 * _circleRadius - 100, -250));
		
		var allAnimations = GetNode<AnimationPlayer>(AnimPlayer).GetAnimationList();
		var animationNodesSoFar = new List<StringName>();
		
		foreach (var (animName, index) in allAnimations.WithIndex())
		{
			var animationNode = new AnimationNodeAnimation();
			animationNode.Animation = animName;
			GD.Print(stateMachine.GetNodePosition(animName));
			stateMachine.AddNode(animName, animationNode);
			
			// Set position
			var angle = 2 * Mathf.Pi / allAnimations.Length * index - Mathf.Pi;
			stateMachine.SetNodePosition(animName, _circleRadius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
			
			// If it's the first one, connect it to start
			if (animationNodesSoFar.Count == 0)
			{
				var startTransition = new AnimationNodeStateMachineTransition();
				startTransition.AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto;
				startTransition.XfadeTime = 0.25f;
				startTransition.AdvanceCondition = animName;
				stateMachine.AddTransition("Start", animName, startTransition);
			}

			// Connect to previously placed nodes
			foreach (var existingNodeName in animationNodesSoFar)
			{
				var toTransition = new AnimationNodeStateMachineTransition();
				toTransition.AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto;
				toTransition.XfadeTime = 0.25f;
				toTransition.AdvanceCondition = animName;
				stateMachine.AddTransition(existingNodeName, animName, toTransition);
				
				var fromTransition = new AnimationNodeStateMachineTransition();
				fromTransition.AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto;
				fromTransition.XfadeTime = 0.25f;
				fromTransition.AdvanceCondition = existingNodeName;
				stateMachine.AddTransition(animName, existingNodeName, fromTransition);
			}
			
			animationNodesSoFar.Add(animName);
		}
		
		// Unsure whether I need to clear the old state machine. Probably not?
		TreeRoot = stateMachine;
	}
}
