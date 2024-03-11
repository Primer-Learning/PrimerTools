using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools;

public static class AnimationUtilities
{
    // TODO: Add a delay method which just pushes the timing of all keys back and returns a new animation
    
    private const float Epsilon = 0.0001f;
    
    #region Node animation extensions
    public static Animation MoveTo(this Node3D node, Vector3 destination, float stopDistance = 0, float duration = 0.5f)
    {
        if (duration == 0) duration = Epsilon;
        
        var animation = new Animation();
        
        // It turns out the specialized track is busted? The graph tics jump around, but I'm too lazy to
        // make a test case to report it. I'll just use the generic track for now.
        
        // var trackIndex = animation.AddTrack(Animation.TrackType.Position3D);
        // animation.PositionTrackInsertKey(trackIndex, 0.0f, node.Position);
        // animation.PositionTrackInsertKey(trackIndex, duration, destination);
        // animation.TrackSetPath(trackIndex, node.GetPath());
        
        var difference = destination - node.Position;
        destination -= difference.Normalized() * stopDistance;

        var trackIndex = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackSetInterpolationType(trackIndex, Animation.InterpolationType.Cubic);
        animation.TrackInsertKey(trackIndex, 0.0f, node.Position);
        animation.TrackInsertKey(trackIndex, duration, destination);
        animation.TrackSetPath(trackIndex, node.GetPath()+":position");
        
        node.Position = destination;

        return animation;
    }
    public static Animation RotateTo(this Node3D node, Vector3 eulerAnglesInDegrees, float duration = 0.5f)
    {
        var eulerAnglesInRadians = new Vector3(
            Mathf.DegToRad(eulerAnglesInDegrees.X),
            Mathf.DegToRad(eulerAnglesInDegrees.Y),
            Mathf.DegToRad(eulerAnglesInDegrees.Z)
        );
        return node.RotateTo(Quaternion.FromEuler(eulerAnglesInRadians), duration);
    }
    public static Animation RotateTo(this Node3D node, Quaternion destination, float duration = 0.5f)
    {
        if (duration == 0) duration = Epsilon;
        
        var animation = new Animation();
        
        var trackIndex = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackSetInterpolationType(trackIndex, Animation.InterpolationType.Cubic);
        animation.TrackInsertKey(trackIndex, 0.0f, node.Quaternion);
        animation.TrackInsertKey(trackIndex, duration, destination);
        animation.TrackSetPath(trackIndex, node.GetPath()+":quaternion");
        
        node.Quaternion = destination;

        return animation;
    }
    public static Animation WalkTo(this Node3D node, Vector3 destination, float stopDistance = 0, float duration = 0.5f, float prepTurnDuration = 0.1f)
    {
        var difference = destination - node.Position;
        
        var prepRotation = node.RotateTo(new Quaternion(Vector3.Back, difference.Normalized()), prepTurnDuration);
        var move = node.MoveTo(destination, stopDistance, duration);

        return Parallel(
            prepRotation,
            move
        );
    }
    public static Animation ScaleTo(this Node3D node, Vector3 finalScale, float duration = 0.5f)
    {
        if (duration == 0) duration = Epsilon;
        
        var animation = new Animation();

        // if (finalScale == node.Scale) return null;
        
        // var trackIndex = animation.AddTrack(Animation.TrackType.Scale3D);
        // animation.ScaleTrackInsertKey(trackIndex, 0.0f, node.Scale);
        // animation.ScaleTrackInsertKey(trackIndex, duration, finalScale);
        // animation.TrackSetPath(trackIndex, node.GetPath());
        
        var trackIndex = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackSetInterpolationType(trackIndex, Animation.InterpolationType.Cubic);
        animation.TrackInsertKey(trackIndex, 0.0f, node.Scale);
        animation.TrackInsertKey(trackIndex, duration, finalScale);
        animation.TrackSetPath(trackIndex, node.GetPath()+":scale");
        
        node.Scale = finalScale;

        return animation;
    }
    public static Animation ScaleTo(this Node3D node, float finalScale, float duration = 0.5f)
    {
        return node.ScaleTo(Vector3.One * finalScale, duration);
    }
    #endregion

    #region Material animation
    public static Animation AnimateColorHsv(this MeshInstance3D meshInstance3D, Color finalColor, float duration = 0.5f)
    {
        var material = meshInstance3D.GetOrCreateOverrideMaterial();
        
        var animation = new Animation();
        
        var trackIndex = animation.AddTrack(Animation.TrackType.Value);
        var hueDiff = finalColor.H - material.AlbedoColor.H;
        if (Mathf.Abs(hueDiff) < 0.5f)
        {
            // Hues are closer than half the color wheel, so interpolate normally 
            animation.TrackInsertKey(trackIndex, 0.0f, material.AlbedoColor.H);
            animation.TrackInsertKey(trackIndex, duration, finalColor.H);
            animation.TrackSetPath(trackIndex, meshInstance3D.GetPath()+":surface_material_override/0:albedo_color:h");
        }
        else
        { 
            // Hues are further than half the color wheel.
            // There might be some elegant way to handle both of these cases together, but not sure it would be faster
            // or less confusing.
            if (hueDiff < 0) // If the final hue is less than the initial hue, we need to wrap around 1 
            {
                hueDiff = 1 - MathF.Abs(hueDiff); // -0.9 goes to 0.1 for example. We now care about the magnitude of the shorter path.
                animation.TrackInsertKey(trackIndex, 0f, material.AlbedoColor.H);
                animation.TrackInsertKey(trackIndex, duration * (1 - material.AlbedoColor.H) / hueDiff, 1f);
                animation.TrackInsertKey(trackIndex, duration * (1 - material.AlbedoColor.H) / hueDiff + Epsilon, 0f);
                animation.TrackInsertKey(trackIndex, duration, finalColor.H);
                animation.TrackSetPath(trackIndex, meshInstance3D.GetPath()+":surface_material_override/0:albedo_color:h");
            }
            else // If the final hue is greater than the initial hue, we need to wrap around 0
            {
                hueDiff = 1 - MathF.Abs(hueDiff);
                animation.TrackInsertKey(trackIndex, 0f, material.AlbedoColor.H);
                animation.TrackInsertKey(trackIndex, duration * material.AlbedoColor.H / hueDiff, 0f);
                animation.TrackInsertKey(trackIndex, duration * material.AlbedoColor.H / hueDiff + Epsilon, 1f);
                animation.TrackInsertKey(trackIndex, duration, finalColor.H);
                animation.TrackSetPath(trackIndex, meshInstance3D.GetPath()+":surface_material_override/0:albedo_color:h");
            }
        }
        
        trackIndex = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackInsertKey(trackIndex, 0f, material.AlbedoColor.S);
        animation.TrackInsertKey(trackIndex, duration, finalColor.S);
        animation.TrackSetPath(trackIndex, meshInstance3D.GetPath()+":surface_material_override/0:albedo_color:s");
        
        trackIndex = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackInsertKey(trackIndex, 0f, material.AlbedoColor.V);
        animation.TrackInsertKey(trackIndex, duration, finalColor.V);
        animation.TrackSetPath(trackIndex, meshInstance3D.GetPath()+":surface_material_override/0:albedo_color:v");
        
        material.AlbedoColor = finalColor;

        return animation;
    }
    
    public static Animation AnimateColorRgb(this MeshInstance3D meshInstance3D, Color finalColor, float duration = 0.5f)
    {
        var material = meshInstance3D.GetOrCreateOverrideMaterial();
        
        var animation = new Animation();
        var trackIndex = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackInsertKey(trackIndex, 0.0f, material.AlbedoColor);
        animation.TrackInsertKey(trackIndex, duration, finalColor);
        animation.TrackSetPath(trackIndex, meshInstance3D.GetPath()+":surface_material_override/0:albedo_color");
        material.AlbedoColor = finalColor;

        return animation;
    }
    
    private static StandardMaterial3D GetOrCreateOverrideMaterial(this MeshInstance3D meshInstance3D)
    {
        // Currently, this creates a new material if there's no existing override. I'm not certain whether this
        // is terribly inefficient. I know Godot has inherited materials or something like that, which should be 
        // more efficient, but I don't know if this uses that under the hood or what.
        
        // Use the surface material override if it's there and a StandardMaterial3D
        var currentOverride = meshInstance3D.GetSurfaceOverrideMaterial(0);
        if (currentOverride is StandardMaterial3D material)
        {
            return material;
        }
        if (currentOverride != null)
        {
            // There may be other material types this works for, but just working with StandardMaterial3D for now.
            GD.PushWarning($"Surface override material of {meshInstance3D.Name} is not a StandardMaterial3D. " +
                        $"Haven't handled that case for animations," +
                        $"so replacing with a new StandardMaterial3D.");
        }
        
        // If not, copy the mesh's material and put it in the override slot to avoid changing the color of all the objects that share the mesh.
        // If neither exist, just create a new standard material and put it in the override slot.
        var meshMaterial = meshInstance3D.Mesh.SurfaceGetMaterial(0);
        var newOverrideMaterial = meshMaterial != null
            ? (StandardMaterial3D)meshMaterial.Duplicate()
            : new StandardMaterial3D();
            
        meshInstance3D.SetSurfaceOverrideMaterial(0, newOverrideMaterial);
        return newOverrideMaterial;
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
        
        var finalAnimationLength = 0.0;
        // Loop through the animations
        foreach (var memberAnimation in animations)
        {
            var animLength = 0.0;
            // Loop through the tracks in each animation
            for (var i = 0; i < memberAnimation.GetTrackCount(); i++)
            {
                var path = memberAnimation.TrackGetPath(i);
                var trackType = memberAnimation.TrackGetType(i);
                if (trackType == Animation.TrackType.Animation)
                {
                    GD.PushWarning($"AnimationUtilities.CombineAnimations may not work well with animation playback tracks. Track path: {memberAnimation.TrackGetPath(i)}");
                }

                int trackIndex;
                if (trackPaths.Contains((path, trackType)))
                {
                    trackIndex = trackPaths.IndexOf((path, trackType));
                }
                else
                {
                    trackIndex = newAnimation.AddTrack(trackType);
                    newAnimation.TrackSetPath(trackIndex, path);
                    trackPaths.Add((path, trackType));
                }
                
                // Loop through they keys in each track
                for (var j = 0; j < memberAnimation.TrackGetKeyCount(i); j++)
                {
                    var keyTime = memberAnimation.TrackGetKeyTime(i, j);
                    animLength = Mathf.Max(animLength, keyTime);
                    newAnimation.TrackInsertKey(trackIndex, keyTime + (parallel ? 0 : finalAnimationLength), memberAnimation.TrackGetKeyValue(i, j));
                }
            }

            if (parallel)
            {
                finalAnimationLength = Mathf.Max(finalAnimationLength, animLength);
            }
            else
            {
                finalAnimationLength += animLength;
            }
        }
        
        newAnimation.Length = (float)finalAnimationLength;
        
        return newAnimation;
    }
    
    public static Animation RunInParallel(this IEnumerable<Animation> animations)
    {
        return Parallel(animations.ToArray());
    }
    
    #endregion
    
    #region AnimationPlayer extensions

    public static Animation CopyAnimationAndLocalizeTrackPaths(this AnimationPlayer animationPlayer, StringName name)
    {
        var original = animationPlayer.GetAnimation(name);
        if (original == null) throw new Exception($"Animation {name} not found in {animationPlayer.Name}");
        var anim = original.Duplicate() as Animation;

        return animationPlayer.LocalizeTrackPaths(anim);
    }

    public static Animation LocalizeTrackPaths(this AnimationPlayer animationPlayer, Animation anim)
    {
        // Get the path to the animation player
        var rootOfRelativePath = animationPlayer.GetParent().GetPath();
        // Loop through the tracks in the animation
        // For each track, get the path to the node and make it absolute
        for (var i = 0; i < anim.GetTrackCount(); i++)
        {
            anim.TrackSetPath(i, rootOfRelativePath + "/" + anim.TrackGetPath(i));
        }
        return anim;
    }
    
    #endregion
}