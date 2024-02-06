using Godot;

namespace PrimerTools.AnimationSequence;

[Tool]
public partial class CombinedAnimation : AnimationPlayer
{
	[Export]
	private AnimationPlayer innerAnimationPlayer1;
	[Export]
	private AnimationPlayer innerAnimationPlayer2;

	private double lastAnimationPosition = -1;

	public override void _Process(double delta)
	{
		// GD.Print("Processssssssssssssssssssss");
		if (Engine.IsEditorHint())
		{
			if (IsPlaying() || lastAnimationPosition != CurrentAnimationPosition)
			{
				OnSeeked(CurrentAnimationPosition);
				lastAnimationPosition = CurrentAnimationPosition;
			}
		}
	}

	private void OnSeeked(double pos)
	{
		GD.Print("Seeked to position: ", pos);
		// Synchronize inner animation players when scrubbed
		SynchronizeInnerAnimationPlayers();
	}

	private void SynchronizeInnerAnimationPlayers()
	{
		var currentTime = CurrentAnimationPosition;
		innerAnimationPlayer1.Seek(currentTime, true);
		innerAnimationPlayer2.Seek(currentTime, true);
	}
}
