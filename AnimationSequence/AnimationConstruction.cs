using Godot;

namespace PrimerTools.AnimationSequence;

public static class AnimationConstruction
{
    public static Animation MoveAnimation(Node3D node3D, Vector3 to, Vector3? from = null, float duration = 1f)
    {
        var animation = new Animation();
        animation.Length = duration;
		
        var trackIndex = animation.AddTrack(Animation.TrackType.Value);
		
        animation.TrackInsertKey(trackIndex, 0.0f, from ?? node3D.Position);
        animation.TrackInsertKey(trackIndex, duration, to);
        animation.TrackSetPath(trackIndex, $"{node3D.GetPath()}:position");
        node3D.Position = to;

        return animation;
    }
}