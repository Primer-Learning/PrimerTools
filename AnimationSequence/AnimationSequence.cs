using Godot;

namespace PrimerTools.AnimationSequence;

[Tool]
public abstract partial class AnimationSequence : AnimationPlayer
{
	private AnimationPlayer referenceAnimationPlayer;

	private int animationsMade = 0;

	private bool run = true;
	[Export] private bool Run {
		get => run;
		set {
			var oldRun = run;
			run = value;
			if (run && !oldRun && Engine.IsEditorHint()) { // Avoids running on build
				Reset();
				Define();
				CreateTopLevelAnimation();
			}
		}
	}
	
	public override void _Ready()
	{
		// This doesn't work!
		// if (!Engine.IsEditorHint())
		// {
		// 	Play("p/CombinedAnimation");
		// }
	}

	protected abstract void Define();
	private void Reset()
	{
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}
		
		referenceAnimationPlayer = CreateReferenceAnimationPlayer();
		// Reset the index for the library
		animationsMade = 0;
	}

	#region Animation Methods
	protected void MoveAnimation(Node3D node3D, Vector3 to, Vector3? from = null, float duration = 1f)
	{
		// node3D.Owner = GetTree().EditedSceneRoot;
		
		// referenceAnimationPlayer ??= CreateReferenceAnimationPlayer();
		
		// Make or get the AnimationLibrary
		var library = MakeOrGetAnimationLibrary(referenceAnimationPlayer, "p");
		
		var animation = new Animation();
		animation.Length = duration; // Set the length of the animation
		var trackIndex = animation.AddTrack(Animation.TrackType.Value);
		
		// Put the library in the animation player
		AddAnimationToLibrary(animation, $"anim{animationsMade}", library);
		animationsMade++;
		
		animation.TrackSetPath(trackIndex, $"{node3D.GetPath()}:position");
		animation.TrackInsertKey(trackIndex, 0.0f, from.HasValue ? from.Value : node3D.Position);
		animation.TrackInsertKey(trackIndex, 1.0f, to);
		node3D.Position = to;
	}
	#endregion
	
	#region Animation Library Handling
	private void CreateTopLevelAnimation()
	{
		var animation = new Animation();
		var trackIndex = animation.AddTrack(Animation.TrackType.Animation);
		
		// TODO: Make a dictionary of animations and start times
		// Start times can be gotten from the top-level animation player if they exist already
		animation.TrackSetPath(trackIndex, $"{referenceAnimationPlayer.GetPath()}:animation");
		
		// TODO: Make time the minimum of next start time and previous end time
		var time = 0.0f;
		foreach (var animationName in referenceAnimationPlayer.GetAnimationList())
		{
			animation.TrackInsertKey(trackIndex, time, animationName);
			time += 1;
		}
		animation.Length = time;
		
		var library = MakeOrGetAnimationLibrary(this, "p");
		AddAnimationToLibrary(animation, "CombinedAnimation", library);
	}
	
	private AnimationPlayer CreateReferenceAnimationPlayer()
	{
		// This animation player is used as a container for the library of animations
		// It's necessary because an animation playback track needs to reference an AnimationPlayer, not just a library
		referenceAnimationPlayer?.QueueFree();
		var newPlayer = new AnimationPlayer();
		AddChild(newPlayer);
		newPlayer.Owner = GetTree().EditedSceneRoot;
		return newPlayer;
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
	
	#endregion
}