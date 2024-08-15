using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimerTools;

[Tool]
public partial class BlobAnimationTree : SelfBuildingAnimationTree
{
	private int _circleRadius = 300;

	private Vector2 CalculatePosition(int index, int total)
	{
		// Starts at 180 degrees so the "first" thing is on the left.
		var angle = 2 * Mathf.Pi / total * index - Mathf.Pi;
		return _circleRadius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
	}
	
	protected override void Build()
	{
		base.Build();

		var topLevelBlendTree = new AnimationNodeBlendTree();

		// Make the BODY state machine
		var bodyStateMachine = CreateOmniconnectedStateMachine("BODY", true);
		// Make the MOUTH state machine
		var mouthStateMachine = CreateOmniconnectedStateMachine("MOUTH", false);
		
		// Unsure whether I need to clear the old state machine. Probably not?
		topLevelBlendTree.AddNode("BODY", bodyStateMachine);
		topLevelBlendTree.SetNodePosition("BODY", new Vector2(-400, -300));
		topLevelBlendTree.AddNode("MOUTH", mouthStateMachine);
		topLevelBlendTree.SetNodePosition("MOUTH", new Vector2(-200, 200));

		var addNode = new AnimationNodeAdd2();
		Set("parameters/Add/add_amount", 1);
		topLevelBlendTree.AddNode("Add", addNode);
		
		topLevelBlendTree.ConnectNode("Add", 0, "BODY");
		topLevelBlendTree.ConnectNode("Add", 1, "MOUTH");
		topLevelBlendTree.ConnectNode("output", 0, "Add");
		
		TreeRoot = topLevelBlendTree;
	}

	private AnimationNodeStateMachine CreateOmniconnectedStateMachine(string prefix, bool loadWiggles)
	{
		var stateMachine = new AnimationNodeStateMachine();
		stateMachine.SetNodePosition("Start", new Vector2(- 2 * _circleRadius, 0));
		stateMachine.SetGraphOffset(new Vector2(- 2 * _circleRadius - 100, -250));
		
		var bodyAnimations = GetNode<AnimationPlayer>(AnimPlayer).GetAnimationList().Where(x => x.StartsWith(prefix)).ToList();
		var animationNodesSoFar = new List<StringName>();
		
		// Add the basic animations
		foreach (var animName in bodyAnimations)
		{
			var animationNode = new AnimationNodeAnimation();
			animationNode.Animation = animName;
			stateMachine.AddNode(animName, animationNode);
		}
		
		// Add wiggles blend tree if needed
		if (loadWiggles)
		{
			// Set up the wiggles state, which is more complex and won't change often, so it's loaded rather than generated
			var wigglesTree = ResourceLoader.Load<AnimationNodeBlendTree>("res://addons/PrimerAssets/Organized/Blob/Blobs/blob_wiggle_tree.tres");
			stateMachine.AddNode("Wiggles", wigglesTree, CalculatePosition(0, bodyAnimations.Count + 1));
			bodyAnimations.Insert(0, "Wiggles");
		}
		
		foreach (var (animName, index) in bodyAnimations.WithIndex())
		{
			// Set position
			stateMachine.SetNodePosition(animName, CalculatePosition(index, bodyAnimations.Count)); // +1 is for the wiggles, which is loaded before this loop 

			if (animationNodesSoFar.Count == 0)
			{
				var startTransition = new AnimationNodeStateMachineTransition();
				startTransition.AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto;
				startTransition.XfadeTime = 0.25f;
				startTransition.AdvanceCondition = animName;
				stateMachine.AddTransition("Start", animName, startTransition);
				Set($"parameters/{prefix}/conditions/{animName}", true);
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
		
		return stateMachine;
	}
}
