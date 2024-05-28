using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

namespace PrimerTools;

[Tool]
public abstract partial class AnimationSequence : AnimationPlayer
{
	/* Multiple animation tracks to work around cache max issue
	- Make RegisterAnimation create the players and libraries
	- Verify that they are being created
	- Add a track to the CombinedAnimation
	- Add an animation to the second track
	- Test combined and non-combined in editor
	- Test in play mode
	
	 */
	
	private const string MainAnimationName = "CombinedAnimation";
	private const string MainLibraryName = "main";
	private const string ReferenceLibraryBaseName = "ref";
	private List<AnimationPlayer> _referenceAnimationPlayers = new();
	private List<AnimationLibrary> _referenceAnimationLibraries = new();
	
	// This also tracks how many animations have been made
	// Now, per track!
	private readonly List<List<float>> _startTimes = new();

	protected bool TooBigAndHaveToUseSeparateAnimations;
	
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
				
				// Rewind(timeToRewindTo);
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

	public static AnimationSequence Instance { get; private set; }

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;
		if (Engine.GetWriteMoviePath() != "") // We recordin
		{
			// If there's more than one fps viewer, you messed up
			var fpsViewer = GetParent().GetChildren().OfType<FPSViewer>().FirstOrDefault();
			
			if (fpsViewer != null) fpsViewer.Visible = false;
		}
		
		// Redo everything on play
		// The reason for this is that non-serialized data is lost when rebuilding to play
		// But just re-doing everything is a simple way to avoid that problem
		// If it gets heavy we could think of other ways
		// The proximate cause at time of writing is that CurveData loses track of its points.
		// An alternate approach could create nodes for each point.
		Reset();
		Define();

		if (!TooBigAndHaveToUseSeparateAnimations)
		{
			CreateTopLevelAnimation(singleClip: true);
			var mainAnimName = MainLibraryName + "/" + MainAnimationName;
			var anim = GetAnimation(mainAnimName);
			anim.RemoveTrack(0);
			CurrentAnimation = mainAnimName;
			Pause(); // Setting current animation automatically plays. We need to pause so seeking works.
			Seek(timeToRewindTo);
			Play();
			
			// TODO: Loop through ref players
			
			// You can't start playing an animation playback track from the middle of an animation
			// I guess since the key is only at the beginning, so the middle is beyond the key.
			_referenceAnimationPlayers[0].CurrentAnimation = ReferenceLibraryBaseName + "/final_combined";
			_referenceAnimationPlayers[0].Seek(timeToRewindTo);
			_referenceAnimationPlayers[0].Play();
		}
		else
		{
			CreateTopLevelAnimation(singleClip: false);
			Rewind(timeToRewindTo);
			var mainAnimName = MainLibraryName + "/" + MainAnimationName;
			// var anim = GetAnimation(mainAnimName);
			// anim.RemoveTrack(0);
			CurrentAnimation = mainAnimName;
			Pause(); // Setting current animation automatically plays. We need to pause so seeking works.
			Seek(timeToRewindTo);
			Play();
			
			// You can't start playing an animation playback track from the middle of an animation
			// I guess since the key is only at the beginning, so the middle is beyond the key.
			// _referenceAnimationPlayers.CurrentAnimation = ReferenceLibraryBaseName + "/final_combined";
			// _referenceAnimationPlayers.Seek(timeToRewindTo);
			// _referenceAnimationPlayers.Play();
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
		
		_referenceAnimationPlayers.Clear();
		_referenceAnimationLibraries.Clear();
		_startTimes.Clear();
		
		Instance = this;
	}

	#region Animation Methods

	/// <summary>
	/// Registers an animation to be included in the top-level animation.
	/// </summary>
	/// <param name="animation"></param> The animation to register.
	/// <param name="time"></param> The time at which this animation should play.
	/// <param name="playbackTrackIndex"></param> Specifies playback track the animation will be registered to.
	/// <param name="log"></param> Print extra information. Useful if the paths seem wrong. But it's been tested pretty thoroughly at this point.
	protected void RegisterAnimation(Animation animation, float time = -1, int playbackTrackIndex = 0, bool log = false)
	{
		// Correct paths
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
		
		for (var i = 0; i <= playbackTrackIndex; i++)
		{
			if (_referenceAnimationPlayers.Count > i) continue; // Don't remake ones that exist
			
			_referenceAnimationPlayers.Add(MakeReferenceAnimationPlayer(i));
			_referenceAnimationLibraries.Add(MakeOrGetAnimationLibrary(_referenceAnimationPlayers[i], ReferenceLibraryBaseName + i));
			_startTimes.Add(new List<float>());
		}
		
		// Put the library in the animation player
		AddAnimationToLibrary(animation, $"anim{_startTimes[playbackTrackIndex].Count}", _referenceAnimationLibraries[playbackTrackIndex]);
		_startTimes[playbackTrackIndex].Add(time);
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
		var topLevelAnimation = new Animation();
	
		// If the animation already exists, remove the playback tracks
		// This is so the audio track will stay.
		if (library.HasAnimation(MainAnimationName))
		{
			topLevelAnimation = library.GetAnimation(MainAnimationName);
			for (var i = topLevelAnimation.GetTrackCount() - 1; i >= 0; i--)
			{
				if (topLevelAnimation.TrackGetType(i) == Animation.TrackType.Animation)
				{
					topLevelAnimation.RemoveTrack(i);
					GD.Print("Removing track");
				} 
			}
		}

		var latestTime = 0f; // For determining the length of the combined animation that could have multiple tracks
		for (var i = 0; i < _referenceAnimationPlayers.Count; i++)
		{
			// With the playback track removed (if it ever existed), we can add the new ones
			var trackIndex = topLevelAnimation.AddTrack(Animation.TrackType.Animation);
			topLevelAnimation.TrackSetPath(trackIndex, $"{Name}/ReferenceAnimationPlayer{i}:animation");
			// animation.TrackMoveTo(trackIndex, i);
			if (singleClip) topLevelAnimation.TrackInsertKey(trackIndex, 0, $"{ReferenceLibraryBaseName}{i}/final_combined");
			
			var animationsWithDelays = new List<Animation>();
			var time = 0.0f;
		
			for (var j = 0; j < _referenceAnimationPlayers[i].GetAnimationList().Length; j++)
			{
				var animationName = $"{ReferenceLibraryBaseName}{i}/anim{j}";
				// Handle start time
				if (_startTimes[i][j] >= time) // If next start time is after previous end time, use it
				{
					time = _startTimes[i][j];
				}
				else if (_startTimes[i][j] > 0) // Otherwise, use the previous end time. If it the time was set, warn that it's not used.
				{
					GD.PushWarning($"Animation {j} in library {i} starts before the previous animation ends. Pushing it back.");
				}

				if (singleClip)
				{
					var nonDelayedAnimation = _referenceAnimationPlayers[i]
						.GetAnimation($"{ReferenceLibraryBaseName}{i}/anim{j}");
					animationsWithDelays.Add(nonDelayedAnimation.WithDelay(time));
				}
				else topLevelAnimation.TrackInsertKey(i, time, animationName);

				// End time for next iteration or final length
				time += _referenceAnimationPlayers[i].GetAnimation(animationName).Length;
			}
			if (singleClip) AddAnimationToLibrary(animationsWithDelays.RunInParallel(), "final_combined", _referenceAnimationLibraries[i]);

			latestTime = Mathf.Max(latestTime, time);
		}
		
		// Make the animation 100s longer than it actually is, so the editor leaves some room
		topLevelAnimation.Length = latestTime + 100;
		
		AddAnimationToLibrary(topLevelAnimation, MainAnimationName, library);
	}
	#endregion
	
	#region Animation Library Handling
	private AnimationPlayer MakeReferenceAnimationPlayer(int index)
	{
		// This animation player is used as a container for the library of animations
		// It's necessary because an animation playback track needs to reference an AnimationPlayer, not just a library
		var newPlayer = new AnimationPlayer();
		newPlayer.Name = $"ReferenceAnimationPlayer{index}";
		AddChild(newPlayer);
		MoveChild(newPlayer, 0);
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
		var file = Path.Combine(sceneDirectory, "movie.avi");
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
			// TODO: Loop through ref anim players. The trackId in the loop definition below will increment
			var mainAnimation = GetAnimation(MainLibraryName + "/" + MainAnimationName);
			for (var i = mainAnimation.TrackGetKeyCount(0) - 1; i >= 0; i--)
			{
				// Track is zero, because that's the animation track. The key is the name of the animation.
				var name = mainAnimation.AnimationTrackGetKeyAnimation(0, i);
				var individualAnimation = _referenceAnimationPlayers[0].GetAnimation(name);
				SetMethodCallTracksEnabledState(individualAnimation, false);
				_referenceAnimationPlayers[0].CurrentAnimation = name;
				_referenceAnimationPlayers[0].Seek(0, update: true);
				SetMethodCallTracksEnabledState(individualAnimation, true);

				if (mainAnimation.TrackGetKeyTime(0, i) < time)
				{
					break;
				}
			}
		}
		_referenceAnimationPlayers[0].Pause();
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