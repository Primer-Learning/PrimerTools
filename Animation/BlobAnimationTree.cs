using Godot;
using System.Collections.Generic;
using System.Linq;
using PrimerTools;

[Tool]
public partial class BlobAnimationTree : SelfBuildingAnimationTree
{
	// TODO: Separate the builder code into a builder class. No need for the functioning tree to carry this around.
	
	#region Build
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
		
		topLevelBlendTree.AddNode("BODY", bodyStateMachine);
		topLevelBlendTree.SetNodePosition("BODY", new Vector2(-400, -300));
		topLevelBlendTree.AddNode("MOUTH", mouthStateMachine);
		topLevelBlendTree.SetNodePosition("MOUTH", new Vector2(-200, 200));

		var addNode = new AnimationNodeAdd2();
		topLevelBlendTree.AddNode("Add", addNode);
		addNode.FilterEnabled = true;
		// TODO: Figure out how to do this in code? Not sure what the path is supposed to be relative to.
		// addNode.SetFilterPath("blob_rig/Skeleton3D/bone_torso/bone_neck/bone_mouth", true);
		GD.PushWarning("Make sure to set the add filter so the mouth state machine only affects the mouth.");
		Set("parameters/Add/add_amount", 1);
		
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

		var animationNames = GetAnimationNamesForStateMachine(prefix);
		var animationNodesSoFar = new List<StringName>();
		
		// Add the basic animations
		foreach (var animName in animationNames)
		{
			var animationNode = new AnimationNodeAnimation();
			animationNode.Animation = animName;
			stateMachine.AddNode(animName, animationNode);
		}
		
		// Add wiggles blend tree if needed
		if (loadWiggles)
		{
			// Set up the wiggles state, which is more complex and won't change often, so it's loaded rather than generated
			var wigglesTree = ResourceLoader.Load<AnimationNodeBlendTree>("res://addons/PrimerAssets/Organized/Blob/Old Blobs/blob_wiggle_tree.tres");
			stateMachine.AddNode("Wiggles", wigglesTree, CalculatePosition(animationNames.Count, animationNames.Count + 1));
			animationNames.Add("Wiggles");
		}
		
		foreach (var (animName, index) in animationNames.WithIndex())
		{
			// Set position
			stateMachine.SetNodePosition(animName, CalculatePosition(index, animationNames.Count));

			if (animationNodesSoFar.Count == 0)
			{
				var startTransition = new AnimationNodeStateMachineTransition();
				startTransition.AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto;
				startTransition.XfadeTime = 0f;
				// No advance condition, since we want it to advance immediately from the start state with no delay.
				// startTransition.AdvanceCondition = animName; // Actually, no condition, since we never want it si
				stateMachine.AddTransition("Start", animName, startTransition);
				Set($"parameters/{prefix}/conditions/{animName}", true);
				GD.Print();
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
	#endregion

	public void TransitionAnimationState(string stateMachineName, AnimationEnum animationEnum)
	{
		// Set them all to false
		foreach (var animationName in GetAnimationNamesForStateMachine(stateMachineName, includeWiggles: true))
		{
			Set($"parameters/{stateMachineName}/conditions/{animationName}", false);
		}
		// Set this specific one to true
		Set($"parameters/{stateMachineName}/conditions/{_animationNames[animationEnum]}", true);

		switch (stateMachineName)
		{
			case "BODY":
				CurrentBodyState = animationEnum;
				break;
			case "MOUTH":
				CurrentMouthState = animationEnum;
				break;
			default:
				GD.PrintErr("You have passed an invalid state machine name to BlobAnimationTree");
				break;
		}
	}

	public Animation AnimateTransitionAnimationState(string stateMachineName, AnimationEnum animationEnum)
	{
		var animations = new List<Animation>();
		// Set them all to false
		foreach (var animationName in GetAnimationNamesForStateMachine(stateMachineName, includeWiggles: true))
		{
			animations.Add(
				this.AnimateBool(
					animationName == _animationNames[animationEnum],
					$"parameters/{stateMachineName}/conditions/{animationName}"
				)
			);
		}

		if (stateMachineName == "BODY") CurrentBodyState = animationEnum;
		if (stateMachineName == "MOUTH") CurrentMouthState = animationEnum;

		return animations.InParallel();
	}
	
	// TODO: Consider eliminating this. It's meant as a convenience for executing an animation and then going back 
	// to the previous state, but it requires all transition methods update it. Probably easier to just use the
	// animation tree's state directly in cases where we want to also create a backward transition. 
	public AnimationEnum CurrentBodyState = AnimationEnum.Keying;
	public AnimationEnum CurrentMouthState = AnimationEnum.Closed;

	private List<string> GetAnimationNamesForStateMachine(string stateMachineName, bool includeWiggles = false)
	{
		var list = GetNode<AnimationPlayer>(AnimPlayer).GetAnimationList().Where(x => x.StartsWith(stateMachineName)).ToList();
		if (includeWiggles && stateMachineName == "BODY") list.Add("Wiggles");
		return list;
	}
	public enum AnimationEnum
	{
		Keying,
		Hello,
		HelloOneShot,
		Scoop,
		CastSpell,
		Jam,
		EvilPose,
		Fight,
		Bongos,
		Wiggles,
		
		Closed,
		Smile,
		OpenWide
	}
	private Dictionary<AnimationEnum, string> _animationNames = new()
	{
		{ AnimationEnum.Keying, "BODY_00_keying" },
		{ AnimationEnum.Hello, "BODY_01_hello" },
		{ AnimationEnum.HelloOneShot, "BODY_02_hello_one_shot" },
		{ AnimationEnum.Scoop, "BODY_03_scoop" },
		{ AnimationEnum.CastSpell, "BODY_04_cast_spell" },
		{ AnimationEnum.Jam, "BODY_07_JAM" },
		{ AnimationEnum.EvilPose, "BODY_08_evil_pose" },
		{ AnimationEnum.Fight, "BODY_09_fight" },
		{ AnimationEnum.Bongos, "BODY_10_bongos" },
		{ AnimationEnum.Wiggles, "Wiggles" },
		
		{ AnimationEnum.Closed, "MOUTH_closed" },
		{ AnimationEnum.Smile, "MOUTH_smile" },
		{ AnimationEnum.OpenWide, "MOUTH_open_wide" }
	};

}
