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
	
	// _run is initially set to false so the Define won't be run on build
	// It is then always true so it runs when the box is clicked.
	private bool _run = false;
	[Export] private bool RunButton {
		get => _run;
		set {
			if (!value && _run && Engine.IsEditorHint()) {
				GD.Print("running");
				Reset();
				Define();
				CreateTopLevelAnimation(makeSingleClip);
				
				Rewind(timeToRewindTo);
				if (makeChildrenLocal)
				{
					this.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
				}
			}
			_run = true;
		}
	}
	
	[Export] private bool makeSingleClip;
	[Export] private bool makeChildrenLocal;
	[Export] private float timeToRewindTo = 0;

	private bool _shouldActuallyUpdatePath = false;
	[ExportGroup("Recording options")] 
	[Export] private bool NewRecordingPathButton
	{
		get => _shouldActuallyUpdatePath;
		set
		{
			if (!value && _shouldActuallyUpdatePath)
			{
				_shouldActuallyUpdatePath = true;
				SetSceneMoviePath();
			}
			_shouldActuallyUpdatePath = true;
		}
	}

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;
		
		// Redo everything on play
		// The reason for this is that non-serialized data is lost when rebuilding to play
		// But just re-doing everything is a simple way to avoid that problem
		// If it gets heavy we could think of other ways
		// The proximate cause at time of writing is that CurveData loses track of its points.
		// An alternate approach could create nodes for each point.
		Reset();
		Define();
		CreateTopLevelAnimation(singleClip: true);
		
		CurrentAnimation = MainLibraryName + "/" + MainAnimationName;
		Pause();
		Seek(timeToRewindTo, update: true);
		Play();
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
		if (animations.Length == 0)
		{
			PrimerGD.PrintErrorWithStackTrace("Can't register an empty animation");
		}
		RegisterAnimation(animations.RunInParallel());
	}
	
	/// <summary>
	/// Create the main animation for the scene.
	/// </summary>
	/// <param name="singleClip">
	/// By default, each registered animation has its own clip, which is useful for moving them around
	/// while building the scene.
	/// If singleClip is true, all animations are combined into a single animation,
	/// which is more reliable for scrubbing and recording.
	/// </param>
	private void CreateTopLevelAnimation(bool singleClip)
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
		
		if (singleClip) animation.TrackInsertKey(0, 0, $"{ReferenceLibraryName}/final_combined");
		
		var animationsWithDelays = new List<Animation>();
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

			if (singleClip)
			{
				var nonDelayedAnimation = _referenceAnimationPlayer.GetAnimation($"{ReferenceLibraryName}/anim{i}");
				animationsWithDelays.Add(nonDelayedAnimation.WithDelay(time));
			}
			else animation.TrackInsertKey(0, time, animationName);
			
			// End time for next iteration or final length
			time += _referenceAnimationPlayer.GetAnimation(animationName).Length;
		}
		
		// Make the animation 100s longer than it actually is, so the editor leaves some room
		animation.Length = time + 100;
		
		if (singleClip) AddAnimationToLibrary(animationsWithDelays.RunInParallel(), "final_combined", _referenceAnimationLibrary);
		
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

	#region Movie Maker Mode Path Handling

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
	
	#region Rewinding
	private void Rewind(float time)
	{
		// If single clip, this doesn't really make sense, and the playhead overrides the state anyway
		if (!makeSingleClip) 
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

				if (mainAnimation.TrackGetKeyTime(0, i) < time)
				{
					break;
				}
			}
		}
		_referenceAnimationPlayer.Pause();
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
	#endregion
}