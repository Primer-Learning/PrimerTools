using System.Collections.Generic;
using Godot;

namespace TimelineTool;

[Tool]
public partial class AnimationCreator : AnimationPlayer
{
	private double lastAnimationPosition = -1;
	private AnimationPlayer _lowLevelAnimationPlayer;
	// List<AnimationPlayer> animationPlayers = new();
	
	int animationsMade = 0;
	
	private bool triggerAnimationCreation = false;
	[Export]
	public bool TriggerAnimationCreation
	{
		get => triggerAnimationCreation;
		set
		{
			triggerAnimationCreation = value;
			if (triggerAnimationCreation)
			{
				Reset();
				Define();
				CreateTopLevelAnimation();
			}
		}
	}

	public void Define()
	{
		for (var i = 0; i < 2; i++)
		{
			DoubleMoveCube(new Vector3(0, 0, - 2 * i));
		}
		
		for (var i = 0; i < 2; i++)
		{
			DoubleMoveCube(new Vector3(2, 0, - 2 * i));
		}
	}

	public void DoubleMoveCube(Vector3 initialPosition)
	{
		var cube = new MeshInstance3D();
		cube.Mesh = new BoxMesh();
		cube.Position = initialPosition;
		AddChild(cube);
		CreateAnimation(cube, initialPosition + Vector3.Right);
		CreateAnimation(cube, initialPosition);
	}
	
	private void CreateAnimation(Node3D node3D, Vector3 to, Vector3? from = null)
	{
		// Make the node show up in the editor
		node3D.Owner = GetTree().EditedSceneRoot;
		
		// Todo: Check whether there's already an animation player
		var animationPlayer = _lowLevelAnimationPlayer;
		
		// Make or get the AnimationLibrary
		var library = MakeOrGetAnimationLibrary(animationPlayer, "code_generated");
		
		var animation = new Animation();
		animation.Length = 1.0f; // Set the length of the animation
		var trackIndex = animation.AddTrack(Animation.TrackType.Value);
		animation.TrackSetPath(trackIndex, $"{node3D.GetPath()}:position");
		
		// Put the library in the animation player
		AddAnimationToLibrary(animation, $"anim{animationsMade}", library);
		animationsMade++;
		
		// Keyframe at time 0: node at the origin
		if (from.HasValue)
		{
			animation.TrackInsertKey(trackIndex, 0.0f, from.Value);
		}
		else
		{
			animation.TrackInsertKey(trackIndex, 0.0f, node3D.Position);
		}
		
		animation.TrackInsertKey(trackIndex, 1.0f, to);
		node3D.Position = to;
	}

	private void Reset()
	{
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}
		_lowLevelAnimationPlayer = new AnimationPlayer();
		AddChild(_lowLevelAnimationPlayer);
		_lowLevelAnimationPlayer.Owner = GetTree().EditedSceneRoot;
	}
	private void CreateTopLevelAnimation()
	{
		var animation = new Animation();
		animation.Length = 2.0f;
		
		var trackIndex = animation.AddTrack(Animation.TrackType.Animation);
		animation.TrackSetPath(trackIndex, $"{_lowLevelAnimationPlayer.GetPath()}:animation");
		
		var time = 0.0f;
		foreach (var animationName in _lowLevelAnimationPlayer.GetAnimationList())
		{
			animation.TrackInsertKey(trackIndex, time, animationName);
			time += 1;
		}
		// var animationName = animationPlayers[0].GetAnimationList()[0];
		// animation.TrackInsertKey(trackIndex, 0, animationName);
		
		var library = MakeOrGetAnimationLibrary(this, "code_generated");
		AddAnimationToLibrary(animation, "CombinedAnimation", library);
	}

	
	
	private AnimationLibrary MakeOrGetAnimationLibrary(AnimationPlayer animationPlayer, string libraryName)
	{
		// Find animation library if it exists
		if (animationPlayer.HasAnimationLibrary(libraryName))
		{
			return animationPlayer.GetAnimationLibrary(libraryName);
		}
		
		var library = new AnimationLibrary();
		animationPlayer.AddAnimationLibrary(libraryName, library);
		return library;
	}
	
	private void AddAnimationToLibrary(Animation animation, string animationName, AnimationLibrary library)
	{
		if (library.HasAnimation(animationName))
		{
			library.RemoveAnimation(animationName);
		}
		library.AddAnimation(animationName, animation);
	}
}