using System.Collections.Generic;
using Godot;

namespace PrimerTools.AnimationSequence;

[Tool]
public abstract partial class AnimationSequence : AnimationPlayer
{
	private const string MainAnimationName = "CombinedAnimation";
	private const string MainLibraryName = "q";
	private const string ReferenceLibraryName = "r";
	private AnimationPlayer _referenceAnimationPlayer;
	private AnimationLibrary _referenceAnimationLibrary;
	
	// This also tracks how many animations have been made
	private List<float> _startTimes = new();
	
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
				if (RewindOnRun) Rewind();
			}
		}
	}
	[Export] public bool RewindOnRun;
	
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
			Rewind();
			
			CurrentAnimation = MainLibraryName + "/" + MainAnimationName;
			Play();
		}
	}

	private void Rewind()
	{
		var mainAnimation = GetAnimation(MainLibraryName + "/" + MainAnimationName);
		for (var i = mainAnimation.TrackGetKeyCount(0) - 1; i >= 0; i--)
		{
			// Track is zero, because that's the animation track. The key is the name of the animation.
			var name = mainAnimation.AnimationTrackGetKeyAnimation(0, i);
			var individualAnimation = _referenceAnimationPlayer.GetAnimation(name);
			SetMethodCallTracksEnabledState(individualAnimation, false);
			_referenceAnimationPlayer.CurrentAnimation = name;
			_referenceAnimationPlayer.Seek(0, update: true);
			SetMethodCallTracksEnabledState(individualAnimation, true);
		}
		if (Engine.IsEditorHint()) _referenceAnimationPlayer.Pause();
	}

	private void SetMethodCallTracksEnabledState(Animation animation, bool enabled)
	{
		for (var i = 0; i < animation.GetTrackCount(); i++)
		{
			if (animation.TrackGetType(i) == Animation.TrackType.Method)
			{
				animation.TrackSetEnabled(i, enabled);
			}
		}
	}

	protected abstract void Define();
	private void Reset()
	{
		// All children are dynamically created and should be removed when the sequence is reset
		// It's good to keep track of these as children since they are in the editor, rather than a list of references
		// that may be lost.
		
		foreach (var child in GetChildren())
		{
			child.Free();
		}
		
		_referenceAnimationPlayer = MakeReferenceAnimationPlayer();
		_referenceAnimationLibrary = MakeOrGetAnimationLibrary(_referenceAnimationPlayer, ReferenceLibraryName);
		
		// Reset times list
		_startTimes.Clear();
	}

	#region Animation Methods

	protected void RegisterAnimation(Animation animation, float time = -1, bool log = false)
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
		AddAnimationToLibrary(animation, $"anim{_startTimes.Count}", _referenceAnimationLibrary);
		_startTimes.Add(time);
	}
	protected void RegisterAnimation(params Animation[] animations)
	{
		RegisterAnimation(animations.RunInParallel());
	}
	#endregion
	
	#region Animation Library Handling
	private void CreateTopLevelAnimation()
	{
		var library = MakeOrGetAnimationLibrary(this, MainLibraryName);
		
		var animation = new Animation();
		if (library.HasAnimation(MainAnimationName))
		{
			animation = library.GetAnimation(MainAnimationName);
			var playbackTrackIndex = animation.FindTrack($"{Name}/ReferenceAnimationPlayer:animation",
				Animation.TrackType.Animation);
			if (playbackTrackIndex != -1) animation.RemoveTrack(playbackTrackIndex);
		}
		
		var trackIndex = animation.AddTrack(Animation.TrackType.Animation);
		animation.TrackSetPath(trackIndex, $"{Name}/ReferenceAnimationPlayer:animation");
		animation.TrackMoveTo(trackIndex, 0);
		
		var time = 0.0f;
		for  (var i = 0; i < _referenceAnimationPlayer.GetAnimationList().Length; i++)
		{
			var animationName = $"{ReferenceLibraryName}/anim{i}";
			// Handle start time
			if (_startTimes[i] > time) // If next start time is after previous end time, use it
			{
				time = _startTimes[i];
			}
			else if (_startTimes[i] > 0) // Otherwise, use the previous end time. If it the time was set, warn that it's not used.
			{
				GD.PushWarning($"Animation {i} starts before the previous animation ends. Pushing it back.");
			}
			
			// Use track index zero because the animation playback track is moved to 0 above.
			animation.TrackInsertKey(0, time, animationName);
			// End time for next iteration or final length
			time += _referenceAnimationPlayer.GetAnimation(animationName).Length;
		}
		animation.Length = time;
		
		AddAnimationToLibrary(animation, MainAnimationName, library);
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
			return;
			
			// No longer replacing the whole animation
			library.RemoveAnimation(animationName);
		}
		library.AddAnimation(animationName, animation);
	}
	
	#endregion
}