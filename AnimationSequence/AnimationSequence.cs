using Godot;

namespace PrimerTools.AnimationSequence;

[Tool]
public abstract partial class AnimationSequence : AnimationPlayer
{
	private const string mainAnimationName = "p/CombinedAnimation";
	private AnimationPlayer _referenceAnimationPlayer;
	private AnimationLibrary _referenceAnimationLibrary;

	private int _animationsMade = 0;

	private bool _run = true;
	[Export] private bool Run {
		get => _run;
		set {
			var oldRun = _run;
			_run = value;
			if (_run && !oldRun && Engine.IsEditorHint()) { // Avoids running on build
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
			
			// Rewind through the individual animations on the reference player
			// so the start state is correct.
			// This is needed because animation creation code sets objects to the
			// final state to prepare for the next animation. So we're undoing that.
			var mainAnimation = GetAnimation(mainAnimationName);
			for (var i = mainAnimation.TrackGetKeyCount(0) - 1; i >= 0; i--)
			{
				var name = mainAnimation.AnimationTrackGetKeyAnimation(0, i);
				_referenceAnimationPlayer.CurrentAnimation = name;
				_referenceAnimationPlayer.Seek(0, update: true);
			}
			
			CurrentAnimation = mainAnimationName;
			Play();
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
		
		_referenceAnimationPlayer = MakeReferenceAnimationPlayer();
		_referenceAnimationLibrary = MakeOrGetAnimationLibrary(_referenceAnimationPlayer, "p");
		
		// Reset the index for the library
		_animationsMade = 0;
	}

	#region Animation Methods

	protected void RegisterAnimation(Animation animation, bool log = false)
	{
		for (var i = 0; i < animation.GetTrackCount(); i++)
		{
			// This runs at edit time, so it assumes an absolute path in the context of the editor.
			// A path relative to AnimationSequence also works, though this code is unnecessary in that case.
			var path = animation.TrackGetPath(i);
			if (log) GD.Print("Path is " + path);
			var node = GetNode(path);
			// Make the path relative to AnimationSequence node so it will work in editor and player contexts
			var relativePath = GetPathTo(node) + ":" + path.GetConcatenatedSubNames();
			if (log) GD.Print("Relative path is " + relativePath);
			animation.TrackSetPath(i, relativePath);
		}
		
		// Put the library in the animation player
		AddAnimationToLibrary(animation, $"anim{_animationsMade++}", _referenceAnimationLibrary);
	}
	protected void RegisterAnimation(params Animation[] animations)
	{
		RegisterAnimation(animations.RunInParallel());
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
		for  (var i = 0; i < _referenceAnimationPlayer.GetAnimationList().Length; i++)
		{
			var animationName = $"p/anim{i}";
			animation.TrackInsertKey(trackIndex, time, animationName);
			time += _referenceAnimationPlayer.GetAnimation(animationName).Length;
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