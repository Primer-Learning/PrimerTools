using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools;

public static class AnimationUtilities
{
    // TODO: Add a delay method which just pushes the timing of all keys back and returns a new animation

    #region Node animation extensions

    public static Animation MoveTo(this Node3D node, Vector3 destination, float duration = 0.5f)
    {
        var animation = new Animation();
        
        var trackIndex = animation.AddTrack(Animation.TrackType.Position3D);
        animation.PositionTrackInsertKey(trackIndex, 0.0f, node.Position);
        animation.PositionTrackInsertKey(trackIndex, duration, destination);
        animation.TrackSetPath(trackIndex, node.GetPath());
        node.Position = destination;

        return animation;
    }
    
    public static Animation ScaleTo(this Node3D node, Vector3 finalScale, float duration = 0.5f)
    {
        var animation = new Animation();
        
        var trackIndex = animation.AddTrack(Animation.TrackType.Scale3D);
        animation.ScaleTrackInsertKey(trackIndex, 0.0f, node.Scale);
        animation.ScaleTrackInsertKey(trackIndex, duration, finalScale);
        animation.TrackSetPath(trackIndex, node.GetPath());
        node.Scale = finalScale;

        return animation;
    }
    public static Animation ScaleTo(this Node3D node, float finalScale, float duration = 0.5f)
    {
        return node.ScaleTo(Vector3.One * finalScale, duration);
    }
    #endregion
    
    #region Combining animations
    public static Animation Parallel(params Animation[] animations)
    {
        return CombineAnimations(animations, true);
    } 
    public static Animation Series(params Animation[] animations)
    {
        return CombineAnimations(animations, false);
    }
    private static Animation CombineAnimations(IEnumerable<Animation> animations, bool parallel)
    {
        var trackPaths = new List<(NodePath, Animation.TrackType)>();
        var newAnimation = new Animation();
        
        var time = 0.0;
        foreach (var anim in animations)
        {
            var maxTime = 0.0;
            for (var i = 0; i < anim.GetTrackCount(); i++)
            {
                var trackType = anim.TrackGetType(i);
                if (trackType == Animation.TrackType.Animation)
                {
                    GD.PushWarning($"AnimationUtilities.CombineAnimations may not work well with animation playback tracks. Track path: {anim.TrackGetPath(i)}");
                }
                var newTrackIndex = newAnimation.AddTrack(trackType);
                for (var j = 0; j < anim.TrackGetKeyCount(i); j++)
                {
                    var keyTime = anim.TrackGetKeyTime(i, j);
                    if (!parallel)
                    {
                        maxTime = Mathf.Max(maxTime, keyTime);
                    }
                    newAnimation.TrackInsertKey(newTrackIndex, keyTime + time, anim.TrackGetKeyValue(i, j));
                }
                
                var path = anim.TrackGetPath(i);
                if (!trackPaths.Contains((path, trackType)))
                {
                    trackPaths.Add((path, trackType));
                    newAnimation.TrackSetPath(newTrackIndex, path);
                }
                else
                {
                    GD.PushWarning($"Combined animations have tracks with the same path and type. May only use last track. Track path: {path}, Track type: {trackType}");
                    newAnimation.TrackSetPath(newTrackIndex, path);
                }
            }
            time += maxTime;
        }
        
        return newAnimation;
    }
    #endregion
}