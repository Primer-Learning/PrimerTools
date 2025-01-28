using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
	- Rewind
	- Test in play mode
	 */
	
	private const string MainAnimationName = "CombinedAnimation";
	private const string MainLibraryName = "main";
	private const string ReferenceLibraryBaseName = "ref";
	private readonly List<AnimationPlayer> _referenceAnimationPlayers = new();
	private readonly List<AnimationLibrary> _referenceAnimationLibraries = new();
	
	// This also tracks how many animations have been made
	// Now, per track!
	private readonly List<List<double>> _startTimes = new();

	protected bool TooBigAndHaveToUseSeparateAnimations;
	
	// _run is initially set to false so the Define won't be run on build
	// It is then always true so it runs when the box is clicked.
	private bool _run = false;
	[Export] private bool RunButton {
		get => _run;
		set {
			if (!value && _run && Engine.IsEditorHint())
			{
				DoneWithConstruction = false;
				Reset();
				Define();
				CreateTopLevelAnimation(makeSingleClip);
				
				Rewind(timeToRewindTo);
				if (makeChildrenLocal)
				{
					this.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
				}

				DoneWithConstruction = true;
			}
			_run = true;
		}
	}
	[Export] private bool ResetButton
	{
		get => false;
		set {
			if (value) {
				Reset();
			}
		}
	}
	
	[Export] private bool makeSingleClip;
	[Export] private bool makeChildrenLocal;
	[Export] private double timeToRewindTo = 0;

	[ExportGroup("Play options")]
	[Export] private bool _quitWhenFinished;

	#region Movie maker handling
	[ExportGroup("Recording options")]
	[Export] private bool _record;

	public enum OutputResolutionOptions
	{
		SD,
		HD,
		FHD,
		UHD
	}

	private bool _setOutputResolution;
	private OutputResolutionOptions _outputResolution;
	[Export]
	public OutputResolutionOptions OutputResolution
	{
		get => _outputResolution;
		set
		{
			if (!_setOutputResolution) // Prevents it running on build, which confuses the godot editor.
			{
				_setOutputResolution = true;
				return;
			} 
			_outputResolution = value;
			int height;
			int width;

			switch (value)
			{
				case OutputResolutionOptions.SD:
					height = 480;
					width = 854;
					break;
				case OutputResolutionOptions.HD:
					height = 720;
					width = 1280;
					break;
				case OutputResolutionOptions.FHD:
					height = 1080;
					width = 1920;
					break;
				case OutputResolutionOptions.UHD:
					height = 2160;
					width = 3840;
					break;
				default:
					GD.PrintErr("Unrecognized output resolution");
					height = 720;
					width = 1280;
					break;
			}
			
			ProjectSettings.SetSetting("display/window/size/viewport_height", height);
			ProjectSettings.SetSetting("display/window/size/viewport_width", width);
			ProjectSettings.Save();
		}
	}
	
	private string _sceneName;
	private string _baseDirectory;
	private string SceneDirectory => Path.Combine(_baseDirectory, "current_take");

	[Export]
	public string SceneName
	{
		get => _sceneName;
		set
		{
			_sceneName = value;
			_baseDirectory = Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "png", _sceneName);
			
			DebouncedCreateDirectory(); // I want run this once after no keystrokes have come in for one second
		}
	}
	
	private CancellationTokenSource _debounceCts;
	private async void DebouncedCreateDirectory()
	{
		try 
		{
			// Cancel any previous pending operation
			_debounceCts?.Cancel();
			_debounceCts?.Dispose();
			_debounceCts = new CancellationTokenSource();

			await Task.Delay(1000, _debounceCts.Token);
        
			// If we get here without being cancelled, create the directory
			CreateRecordingDirectory();
		}
		catch (OperationCanceledException) {}
	}

	private void CreateRecordingDirectory()
	{
		// Set the path for the movie maker mode
		Directory.CreateDirectory(SceneDirectory);
		var file = Path.Combine(SceneDirectory, "image.png");
		GD.Print(file);
		GetParent().SetMeta("movie_file", file);
	}
	private void MovePreviousTakeToNumberedDirectory()
	{
		if (!Directory.Exists(SceneDirectory) || !Directory.EnumerateFileSystemEntries(SceneDirectory).Any()) return;
    
		var number = 1;
		while (Directory.Exists(Path.Combine(_baseDirectory, $"take_{number}")))
		{
			number++;
		}

		var targetDirectory = Path.Combine(_baseDirectory, $"take_{number}");
		Directory.CreateDirectory(targetDirectory);

		// Move all png files
		// There's a locked .wav file, but we don't care about sounds
		foreach (string sourcePath in Directory.GetFiles(SceneDirectory, "*.png", SearchOption.AllDirectories))
		{
			var relativePath = Path.GetRelativePath(SceneDirectory, sourcePath);
			var targetPath = Path.Combine(targetDirectory, relativePath);
			File.Move(sourcePath, targetPath);
		}
	}
	#endregion
	
	public static AnimationSequence Instance { get; private set; }

	protected bool DoneWithConstruction;
	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;
		DoneWithConstruction = false;
		if (_record)
		{
			// _quitWhenFinished = true;	// Always quit when recording, since you can see the result even after it quits,
			// 							// and failing to quit when recording can to a zillion pngs for no reason.
			// Actually don't. Sometimes you want a sim to run.
			// Leaving this here in case I have that bright idea again.
			
			if (string.IsNullOrEmpty(_sceneName)) GD.PrintErr("Recording is true, but scene name is empty.");
			MovePreviousTakeToNumberedDirectory();
			
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
		CreateTopLevelAnimation(singleClip: true);
		var mainAnimName = MainLibraryName + "/" + MainAnimationName;
		
		// For seeking to work with single-animation tracks, we actually need to play the reference
		// players manually.
		// Animation playback tracks are kinda dumb and only know about their starting key,
		// so you can't play them from the middle of an animation.
		// We keep the audio track, though.
		
		// Comment long after this code was written:
		// Is the pause, seek, and play of the main AnimationPlayer (this) even needed?
		// Seems like we're just stripping that and then playing the individuals.
		
		TopLevelAnimationWithPlaybackTracksRemoved(); // We happen to not need the return value here.
		CurrentAnimation = mainAnimName;
		Pause(); // Setting current animation automatically plays. We need to pause so seeking works.
		Seek(0);
		Seek(timeToRewindTo);
		Play();
		
		for (var i = 0; i < _referenceAnimationPlayers.Count; i++)
		{
			_referenceAnimationPlayers[i].CurrentAnimation = ReferenceLibraryBaseName + i + "/final_combined";
			_referenceAnimationPlayers[i].Pause();
			_referenceAnimationPlayers[i].Seek(timeToRewindTo);
			_referenceAnimationPlayers[i].Play();
		}

		DoneWithConstruction = true;
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

	public double GetLastAnimationEndTime(int indexOfPlaybackTrack = 0)
	{
		if (_startTimes[indexOfPlaybackTrack].Count == 0) return 0;
		
		var lastDuration = _referenceAnimationLibraries[indexOfPlaybackTrack]
			.GetAnimation($"anim{_startTimes[indexOfPlaybackTrack].Count - 1}").Length;
		var lastStartTime = _startTimes[indexOfPlaybackTrack][^1];
		return lastStartTime + lastDuration;
	}
	
	/// <summary>
	/// Registers an animation to be included in the top-level animation.
	/// </summary>
	/// <param name="animation"></param> The animation to register.
	/// <param name="time"></param> The time at which this animation should play.
	/// <param name="indexOfPlaybackTrack"></param> Specifies playback track the animation will be registered to.
	/// <param name="log"></param> Print extra information. Useful if the paths seem wrong. But it's been tested pretty thoroughly at this point.
	protected void RegisterAnimation(Animation animation, double time = -1, int indexOfPlaybackTrack = 0, bool log = false)
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
		
		for (var i = 0; i <= indexOfPlaybackTrack; i++)
		{
			if (_referenceAnimationPlayers.Count > i) continue; // Don't remake ones that exist
			
			_referenceAnimationPlayers.Add(MakeReferenceAnimationPlayer(i));
			_referenceAnimationLibraries.Add(MakeOrGetAnimationLibrary(_referenceAnimationPlayers[i], ReferenceLibraryBaseName + i));
			_startTimes.Add(new List<double>());
		}
		
		// Put the library in the animation player
		AddAnimationToLibrary(animation, $"anim{_startTimes[indexOfPlaybackTrack].Count}", _referenceAnimationLibraries[indexOfPlaybackTrack]);

		// Time correction
		var lastTime = GetLastAnimationEndTime(indexOfPlaybackTrack);
		if (time < 0) // Don't warn for the default -1 time.
		{
			time = lastTime;
		}
		else if (time < lastTime) // But do warn if a time was entered but not used.
		{
			time = lastTime;
			GD.PushWarning($"Animation {_startTimes[indexOfPlaybackTrack].Count} in library {indexOfPlaybackTrack} starts before the previous animation ends. Pushing it back.");
		}
		
		_startTimes[indexOfPlaybackTrack].Add(time);
	}
	protected void RegisterAnimation(params Animation[] animations)
	{
		if (animations.Length == 0)
		{
			PrimerGD.PrintErrorWithStackTrace("Can't register an empty animation");
		}
		RegisterAnimation(animations.InParallel());
	}

	private Animation TopLevelAnimationWithPlaybackTracksRemoved()
	{
		// Somewhat jank. If the main animation exists already return it with animation playback tracks stripped
		// Otherwise, return a new animation.
		// Meant to extract logic that was used in two places, but a lil weird since one doesn't even use the return value.
		// Oh well, it works fine.
		
		var library = MakeOrGetAnimationLibrary(this, MainLibraryName);
		if (library.HasAnimation(MainAnimationName))
		{
			var anim = library.GetAnimation(MainAnimationName);
			for (var i = anim.GetTrackCount() - 1; i >= 0; i--)
			{
				if (anim.TrackGetType(i) == Animation.TrackType.Animation)
				{
					anim.RemoveTrack(i);
				} 
			}
			return anim;
		}
		
		return new Animation();
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
		// var topLevelAnimation = new Animation()?;
	
		// If the animation already exists, remove the playback tracks
		// This is so the audio track will stay.
		var topLevelAnimation = TopLevelAnimationWithPlaybackTracksRemoved();

		var latestTime = 0.0; // For determining the length of the combined animation that could have multiple tracks
		for (var i = 0; i < _referenceAnimationPlayers.Count; i++)
		{
			// With the playback track removed (if it ever existed), we can add the new ones
			var tempIndex = topLevelAnimation.AddTrack(Animation.TrackType.Animation);
			topLevelAnimation.TrackSetPath(tempIndex, $"{Name}/ReferenceAnimationPlayer{i}:animation");
			topLevelAnimation.TrackMoveTo(tempIndex, i); // Now the index is i
			if (singleClip) topLevelAnimation.TrackInsertKey(i, 0, $"{ReferenceLibraryBaseName}{i}/final_combined");
			
			var animationsWithDelays = new List<Animation>();
			var time = 0.0;
		
			for (var j = 0; j < _referenceAnimationPlayers[i].GetAnimationList().Length; j++)
			{
				var animationName = $"{ReferenceLibraryBaseName}{i}/anim{j}";
				
				time = _startTimes[i][j];
				
				// Handle start time
				if (_startTimes[i][j] >= time) // If next start time is after previous end time, use it
				{
					time = _startTimes[i][j];
				}
				else // Otherwise use the last end time and warn that an adjustment was made.
				{
					GD.PushWarning($"Animation {j} in library {i} starts before the previous animation ends. Pushing it back.");
					GD.PushWarning("Also, the previous warning should never happen because start times are now corrected in RegisterAnimation. Hrm.");
				}
				
				if (singleClip)
				{
					// In this case, we've already added the key before the loop.
					// But we need to add the current jth animation to the list of animations
					// to make the single clip out of.
					var nonDelayedAnimation = _referenceAnimationPlayers[i]
						.GetAnimation($"{ReferenceLibraryBaseName}{i}/anim{j}");
					animationsWithDelays.Add(nonDelayedAnimation.WithDelay(time));
				}
				else topLevelAnimation.TrackInsertKey(i, time, animationName);

				// End time for next iteration or final length
				time += _referenceAnimationPlayers[i].GetAnimation(animationName).Length;
			}
			if (singleClip) AddAnimationToLibrary(animationsWithDelays.InParallel(), "final_combined", _referenceAnimationLibraries[i]);

			latestTime = Mathf.Max(latestTime, time);
		}
		
		// Extend animation
		// In the editor, we want lots of room.
		// When playing, we want just a little cushion.
		var padding = 100;
		if (!Engine.IsEditorHint())
		{
			padding = 3;
		}
		topLevelAnimation.Length = (float) latestTime + padding;

		if (!Engine.IsEditorHint() && _quitWhenFinished)
		{
			// Add animation that triggers a quit when the animation is over
			topLevelAnimation.AddTrack(Animation.TrackType.Method);
			topLevelAnimation.TrackSetPath(topLevelAnimation.GetTrackCount() - 1, GetPath());
			topLevelAnimation.TrackInsertKey(topLevelAnimation.GetTrackCount() - 1, topLevelAnimation.Length - 1,
				new Godot.Collections.Dictionary()
				{
					{"method", MethodName.Quit},
					{"args", new Godot.Collections.Array()}
				}
			);
		}
		
		AddAnimationToLibrary(topLevelAnimation, MainAnimationName, library);
	}

	private void Quit()
	{
		GetTree().Quit();
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
	
	private static void AddAnimationToLibrary(Animation animation, string animationName, AnimationLibrary library)
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
	
	#region Rewinding
	private void Rewind(double time)
	{
		// If single clip, this isn't needed, and the playhead overrides the state anyway
		if (!makeSingleClip) 
		{
			// TODO: Loop through ref anim players. The trackId in the loop definition below will increment
			
			var mainAnimation = GetAnimation(MainLibraryName + "/" + MainAnimationName);
			
			for (var i = 0; i < _referenceAnimationPlayers.Count; i++)
			{
				for (var j = mainAnimation.TrackGetKeyCount(i) - 1; j >= 0; j--)
				{
					// Track is zero, because that's the animation track. The key is the name of the animation.
					var name = mainAnimation.AnimationTrackGetKeyAnimation(i, j);
					var individualAnimation = _referenceAnimationPlayers[i].GetAnimation(name);
					SetMethodCallTracksEnabledState(individualAnimation, false);
					_referenceAnimationPlayers[i].CurrentAnimation = name;
					_referenceAnimationPlayers[i].Seek(0, update: true);
					SetMethodCallTracksEnabledState(individualAnimation, true);

					if (mainAnimation.TrackGetKeyTime(i, j) < time)
					{
						break;
					}
				}
			}
		}

		foreach (var refPlayer in _referenceAnimationPlayers)
		{
			refPlayer.Pause();
		}
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