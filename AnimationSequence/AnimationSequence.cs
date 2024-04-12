using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
	private readonly List<float> _startTimes = new();
	
	private bool _run = true;
	[Export] private bool Run {
		get => _run;
		set {
			var oldRun = _run;
			_run = value;
			if (_run && !oldRun && Engine.IsEditorHint()) { // Avoids running on build
				Reset();
				Define();
				if (createSingleAnimation) CreateSingleClipTopLevelAnimation();
				else CreateMultiClipTopLevelAnimation();
				
				if (RewindOnRun) Rewind(timeToRewindTo);
			}
			_run = false;
		}
	}
	[Export] private bool RewindOnRun;
	[Export] private float timeToRewindTo = 0;

	[Export] private bool createSingleAnimation;
	[ExportGroup("Recording options")]
	[Export] private bool NewRecordingPath {
		get => false;
		set => SetSceneMoviePath();
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
			CreateSingleClipTopLevelAnimation();
			
			// Rewind through the individual animations on the reference player
			// so the start state is correct.
			// This is needed because animation creation code sets objects to the
			// final state to prepare for the next animation. So we're undoing that.
			Rewind(timeToRewindTo);
			
			CurrentAnimation = MainLibraryName + "/" + MainAnimationName;
			Seek(timeToRewindTo);
			Play();
		}
	}

	private void Rewind(float timeToRewindTo)
	{
		// if (Engine.IsEditorHint())
		// {
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

				if (mainAnimation.TrackGetKeyTime(0, i) < timeToRewindTo)
				{
					break;
				}
			}
		_referenceAnimationPlayer.Pause();
			
		// This section was meant to handle the case where the animations are combined into one
		// by CreateTopLevelAnimationForPlayer. But not doing that. See the comment above the method definition.
		// }
		// else
		// {
		// 	foreach (var animation in _referenceAnimationPlayer.GetAnimationList())
		// 	{
		// 		GD.Print(animation);
		// 	}
		// 	
		// 	// If we're not in the editor, the animations are already combined into one,
		// 	// So we just have to seek that one.
		// 	var mainAnimation = _referenceAnimationPlayer.GetAnimation($"{ReferenceLibraryName}/final_combined");
		// 	SetMethodCallTracksEnabledState(mainAnimation, false);
		// 	_referenceAnimationPlayer.CurrentAnimation = $"{ReferenceLibraryName}/final_combined";
		// 	_referenceAnimationPlayer.Seek(timeToRewindTo, update: true);
		// 	SetMethodCallTracksEnabledState(mainAnimation, true);
		// }
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

	/// <summary>
	/// Registers an animation to be included in the top-level animation.
	/// </summary>
	/// <param name="animation"></param>
	/// <param name="time"></param>
	/// <param name="log"></param>
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
	
	/// <summary>
	/// Create the top-level animation as a series of clips.
	/// This is useful if you want to be able to edit the individual animations in the editor.
	/// </summary>
	private void CreateMultiClipTopLevelAnimation()
	{
		var library = MakeOrGetAnimationLibrary(this, MainLibraryName);
	
		// If the animation already exists, remove the playback track
		// This is so the audio track will stay.
		var animation = new Animation();
		if (library.HasAnimation(MainAnimationName))
		{
			animation = library.GetAnimation(MainAnimationName);
			var playbackTrackIndex = animation.FindTrack($"{Name}/ReferenceAnimationPlayer:animation",
				Animation.TrackType.Animation);
			if (playbackTrackIndex != -1) animation.RemoveTrack(playbackTrackIndex);
		}
		
		// With the playback track removed (if it ever existed), we can add the new one
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
		
		// Make the animation 100s longer than it actually is, so the editor leaves some room
		animation.Length = time + 100;
		
		AddAnimationToLibrary(animation, MainAnimationName, library);
	}
	
	/// <summary>
	/// Creates the top-level animation as a single clip.
	/// This is useful for ensuring all keyframes are evaluated. Multi-clip animations may fail to evaluate
	/// the final keyframe of clips.
	/// Best for recording and scrubbing through the full scene.
	/// </summary>
	private void CreateSingleClipTopLevelAnimation()
	{
		var library = MakeOrGetAnimationLibrary(this, MainLibraryName);
	
		// If the animation already exists, remove the playback track
		// This is so the audio track will stay.
		var animation = new Animation();
		if (library.HasAnimation(MainAnimationName))
		{
			animation = library.GetAnimation(MainAnimationName);
			var playbackTrackIndex = animation.FindTrack($"{Name}/ReferenceAnimationPlayer:animation",
				Animation.TrackType.Animation);
			if (playbackTrackIndex != -1) animation.RemoveTrack(playbackTrackIndex);
		}
		
		// With the playback track removed (if it ever existed), we can add the new one
		
		// But actually let's just add all the tracks individually, I think.
		// var trackIndex = animation.AddTrack(Animation.TrackType.Animation);
		// animation.TrackSetPath(trackIndex, $"{Name}/ReferenceAnimationPlayer:animation");
		// animation.TrackMoveTo(trackIndex, 0);

		var animationsWithDelays = new List<Animation>();
		var time = 0.0f;
		for  (var i = 0; i < _referenceAnimationPlayer.GetAnimationList().Length; i++)
		{
			/* Ideas for implementation:
			 - Create a new list of animations, adding the delay according to
			 the start time or current time, same as the code below. Then use
			 CombineAnimations to create the single combined animation. And put
			 it in an animation playback track.
			 - Copy the tracks from every animation straight onto this one. Downside
			 here would be that I'm duplicating a lot of code from CombineAnimations.
			*/
			
			// Handle start time
			if (_startTimes[i] > time) // If next start time is after previous end time, use it
			{
				time = _startTimes[i];
			}
			else if (_startTimes[i] > 0) // Otherwise, use the previous end time. If it the time was set, warn that it's not used.
			{
				GD.PushWarning($"Animation {i} starts before the previous animation ends. Pushing it back.");
			}
			
			var nonDelayedAnimation = _referenceAnimationPlayer.GetAnimation($"{ReferenceLibraryName}/anim{i}");
			animationsWithDelays.Add(nonDelayedAnimation.WithDelay(time));
			// End time for next iteration or final length
			time += nonDelayedAnimation.Length;
		}
		animation.Length = time + 100;
		
		// Combine the animations with delays and put then in the reference library
		AddAnimationToLibrary(animationsWithDelays.RunInParallel(), "final_combined", _referenceAnimationLibrary);
		// Add the animation playback track to the top-level animation
		// And add the final combined animation to the top level animation
		var trackIndex = animation.AddTrack(Animation.TrackType.Animation);
		animation.TrackSetPath(trackIndex, $"{Name}/ReferenceAnimationPlayer:animation");
		animation.TrackInsertKey(trackIndex, 0, $"{ReferenceLibraryName}/final_combined");
		animation.TrackMoveTo(trackIndex, 0);
		
		AddAnimationToLibrary(animation, MainAnimationName, library);
	}
	
	#endregion
	
	#region Animation Library Handling
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
			// library.RemoveAnimation(animationName);
		}
		library.AddAnimation(animationName, animation);
	}
	
	#endregion

	#region Movie maker mode path handling

	private void SetSceneMoviePath()
	{
		// Establish base directory
		GD.Print(Directory.GetCurrentDirectory());
		var baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "png", GetTree().EditedSceneRoot.Name);
		GD.Print(baseDirectory);
		
		// Move take_current if it exists
		var sceneDirectory = Path.Combine(baseDirectory, "current_take");
		if (Directory.Exists(sceneDirectory) && Directory.EnumerateFileSystemEntries(sceneDirectory).Any())
		{
			var number = 1;
			while (Directory.Exists(Path.Combine(baseDirectory, $"take_{number}")))
			{
				number++;
			}
			Directory.Move(sceneDirectory, Path.Combine(baseDirectory, $"take_{number}"));
		}
		
		// Set the path for the movie maker mode
		Directory.CreateDirectory(sceneDirectory);
		var file = Path.Combine(sceneDirectory, "frame.png");
		GD.Print(file);
		GetTree().EditedSceneRoot.SetMeta("movie_file", file);
	}

	#endregion
}