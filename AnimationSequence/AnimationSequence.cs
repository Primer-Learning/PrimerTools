using Godot;

namespace PrimerTools.AnimationSequence;

[Tool]
public abstract partial class AnimationSequence : AnimationPlayer
{
	private AnimationPlayer referenceAnimationPlayer;
	private AnimationLibrary referenceAnimationLibrary;

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
		if (!Engine.IsEditorHint())
		{
			// Redo everything on play
			// The reason for this is that non-serialized data is lost when rebuilding to play
			// But just re-doing everything is a simple way to avoid that problem
			// If it gets heavy we could think of other ways
			// The proximate cause at time of writing is that CurveData loses track of its points.
			// An alternate approach could create nodes for each point.
			Reset();
			Define();
			CreateTopLevelAnimation();
			
			// Rewind through the animations so the start state is correct
			// This is needed because animation creation code sets objects to the final state to 
			// prepare for the next animation. So we're undoing that.
			var mainAnimation = GetAnimation("p/CombinedAnimation");
			for (var i = mainAnimation.TrackGetKeyCount(0) - 1; i >= 0; i--)
			{
				var time = mainAnimation.TrackGetKeyTime(0, i);
				Seek(time, update: true);
			}
			
			Play("p/CombinedAnimation");
		}
	}

	protected abstract void Define();
	private void Reset()
	{
		// All children are dynamically created and should be removed when the sequence is reset
		// It's good to keep track of these as children since they are in the editor, rather than a list of references
		// that may be lost.
		// TODO: Potential other cases
		// - Children that need to be dynamically created as children of another object. Possible solution
		//	 could be to intentionally create those as objects that have a known location in the scene tree,
		//	 and then include their children in this loop.
		// - Objects that are made in the editor and shouldn't be removed. This approach might be good for complex
		//   objects that are difficult to capture in code. Possible approach is to just remake those too, but just
		//   use packed scenes.
		
		foreach (var child in GetChildren())
		{
			child.Free();
		}
		
		referenceAnimationPlayer = MakeReferenceAnimationPlayer();
		referenceAnimationLibrary = MakeOrGetAnimationLibrary(referenceAnimationPlayer, "p");
		
		// Reset the index for the library
		animationsMade = 0;
	}

	#region Animation Methods

	protected void RegisterAnimation(Animation animation)
	{
		for (var i = 0; i < animation.GetTrackCount(); i++)
		{
			// This runs at edit time, so it assumes an absolute path in the context of the editor.
			// A path relative to AnimationSequence also works, though this code is unnecessary in that case.
			var path = animation.TrackGetPath(i);
			// GD.Print("Path is " + path);
			var node = GetNode(path);
			// Make the path relative to AnimationSequence node so it will work in editor and player contexts
			var relativePath = GetPathTo(node) + ":" + path.GetConcatenatedSubNames();
			animation.TrackSetPath(i, relativePath);
		}
		
		// Put the library in the animation player
		AddAnimationToLibrary(animation, $"anim{animationsMade++}", referenceAnimationLibrary);
	}
	#endregion
	
	#region Animation Library Handling
	private void CreateTopLevelAnimation()
	{
		var animation = new Animation();
		var trackIndex = animation.AddTrack(Animation.TrackType.Animation);
		
		// TODO: Make a dictionary of animations and start times
		// Start times can be gotten from the top-level animation player if they exist already
		animation.TrackSetPath(trackIndex, $"{Name}/ReferenceAnimationPlayer:animation");
		
		// TODO: Make time the minimum of next start time and previous end time
		var time = 0.0f;
		foreach (var animationName in referenceAnimationPlayer.GetAnimationList())
		{
			animation.TrackInsertKey(trackIndex, time, animationName);
			time += referenceAnimationPlayer.GetAnimation(animationName).Length;
		}
		animation.Length = time;
		
		var library = MakeOrGetAnimationLibrary(this, "p");
		AddAnimationToLibrary(animation, "CombinedAnimation", library);
	}
	
	private AnimationPlayer MakeReferenceAnimationPlayer()
	{
		// This animation player is used as a container for the library of animations
		// It's necessary because an animation playback track needs to reference an AnimationPlayer, not just a library
		var newPlayer = new AnimationPlayer();
		newPlayer.Name = "ReferenceAnimationPlayer";
		AddChild(newPlayer);
		newPlayer.Owner = GetParent();
		return newPlayer;
	}
	
	private static AnimationLibrary MakeOrGetAnimationLibrary(AnimationPlayer animationPlayer, string libraryName)
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