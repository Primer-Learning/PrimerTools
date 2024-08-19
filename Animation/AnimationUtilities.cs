using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools;

public static class AnimationUtilities
{
    
    public const double TimeEpsilon = 0.005; // Smaller than this, and keyframes can merge when combining animations.
                                             // I don't know where exactly.
    public const float LengthEpsilon = 0.000001f; // Tolerance is different for length.
    public const double DefaultDuration = 0.5;
    
    #region Node animation extensions
    public static Animation AnimateValue<TNode, TValue>(this TNode node, TValue value, string propertyPath, double duration = DefaultDuration) where TNode : Node
    {
        // One day, make an enum of the different interpolation types and use a switch statement to choose the handles.
        // For now, just use smooth step.
        
        // Bezier handle notes
        // Desmos toy: https://www.desmos.com/calculator/beccajalrw
        // For 0,0 to 1,1, handles should be...
        // Smooth step: (1/3, 0) and (2/3, 1) [Default]
        // Quadratic in (approximate): (0.56, 0) and (1, 1) [Constant acceleration up to a max speed]
        // Quadratic out (approximate): (0, 0) and (0.44, 1) [Constant deceleration down from a max speed]
        // Linear: (0, 0) and (1, 1)
        // Quadratic in-out (approximate): (0.44, 0) and (0.56, 1)
        // Cubic in-out (approximate): (2/3, 0) and (1/3, 1)
        
        // Godot handles are relative to the anchor
        // x values are in seconds
        // y values in the unit of the property being animated
        
        return node.AnimateValue(value, propertyPath, new Vector2((float)duration / 3, 0), new Vector2(- (float)duration / 3, 0), duration);
    }
    public static Animation AnimateValue<TNode, TValue>(this TNode node, TValue value, string propertyPath, Vector2 outHandle, Vector2 inHandle, double duration = DefaultDuration) where TNode : Node
    {
        if (duration == 0) duration = TimeEpsilon;
        var animation = new Animation();
        
        switch (value)
        {
            case double doubleValue:
                node.AnimateValue( (float) doubleValue, propertyPath, outHandle, inHandle, duration);
                break;
            case int intValue:
                node.AnimateValue( (float) intValue, propertyPath, outHandle, inHandle, duration);
                break;
            case float floatValue:
                var trackIndex = animation.AddTrack(Animation.TrackType.Bezier);
                animation.TrackSetPath(trackIndex, node.GetPath()+":" + propertyPath);
                
                // First key
                animation.BezierTrackInsertKey(trackIndex, 0.0, node.GetIndexed(propertyPath).AsSingle());
                animation.BezierTrackSetKeyOutHandle(trackIndex, 0, outHandle);
                // Second key
                animation.BezierTrackInsertKey(trackIndex, duration, floatValue);
                animation.BezierTrackSetKeyInHandle(trackIndex, 1, inHandle);
                
                node.SetIndexed(propertyPath, floatValue);
                break;
            case Vector3 vectorValue:
                string[] propertyNames = {"x", "y", "z"};
                
                for (var i = 0; i < 3; i++)
                {
                    trackIndex = animation.AddTrack(Animation.TrackType.Bezier);
                    animation.TrackSetPath(trackIndex, node.GetPath()+":" + propertyPath + ":" + propertyNames[i]);
                    // First key
                    animation.BezierTrackInsertKey(trackIndex, 0.0f, node.GetIndexed(propertyPath).AsVector3()[i]);
                    animation.BezierTrackSetKeyOutHandle(trackIndex, 0, outHandle);
                    // Second key
                    animation.BezierTrackInsertKey(trackIndex, duration, vectorValue[i]);
                    animation.BezierTrackSetKeyInHandle(trackIndex, 1, inHandle);
                }

                node.SetIndexed(propertyPath, vectorValue);
                break;
            case Color colorValue:
                GD.PushWarning("Animating color with AnimateValue is untested. So you know. <3");
                string[] colorPropertyNames = {"r", "g", "b", "a"};
                
                for (var i = 0; i < 3; i++)
                {
                    trackIndex = animation.AddTrack(Animation.TrackType.Bezier);
                    animation.TrackSetPath(trackIndex, node.GetPath()+":" + propertyPath + ":" + colorPropertyNames[i]);
                    // First key
                    animation.BezierTrackInsertKey(trackIndex, 0.0f, node.GetIndexed(propertyPath).AsVector3()[i]);
                    animation.BezierTrackSetKeyOutHandle(trackIndex, 0, outHandle);
                    // Second key
                    animation.BezierTrackInsertKey(trackIndex, duration, colorValue[i]);
                    animation.BezierTrackSetKeyInHandle(trackIndex, 1, inHandle);
                }

                node.SetIndexed(propertyPath, colorValue);
                break;
            case Quaternion quaternionValue:
                trackIndex = animation.AddTrack(Animation.TrackType.Value);
                animation.TrackSetPath(trackIndex, node.GetPath()+":" + propertyPath);
                // First key
                animation.TrackInsertKey(trackIndex, 0.0f, node.GetIndexed(propertyPath).AsQuaternion());
                // Second key
                animation.TrackInsertKey(trackIndex, duration, quaternionValue);
                animation.TrackSetInterpolationType(trackIndex, Animation.InterpolationType.CubicAngle);

                node.SetIndexed(propertyPath, quaternionValue);
                break;
            default:
                GD.PrintErr("Unsupported type for AnimateValue");
                break;
        }

        animation.Length = (float) duration;
        return animation;
    }
    public static Animation AnimateBool<TNode>(this TNode node, bool value, string propertyPath, bool resetAtEnd = false, double duration = DefaultDuration) where TNode : Node
    {
        var originalValue = node.Get(propertyPath);
        
        if (duration == 0) duration = resetAtEnd 
            ? TimeEpsilon * 3
            : TimeEpsilon;
        
        var animation = new Animation();
        
        var trackIndex = animation.AddTrack(Animation.TrackType.Value);
        
        animation.TrackSetPath(trackIndex, node.GetPath() + ":" + propertyPath);

        animation.TrackInsertKey(trackIndex, 0, originalValue);
        animation.TrackInsertKey(trackIndex, TimeEpsilon, value);

        if (resetAtEnd)
        {
            animation.TrackInsertKey(trackIndex, duration - TimeEpsilon, value);
            animation.TrackInsertKey(trackIndex, duration, originalValue);
        }
        
        node.Set(propertyPath, resetAtEnd ? originalValue : value);
        animation.Length = (float)duration;
        return animation;
    }
    public static Animation MoveTo(this Node3D node, Vector3 destination, float stopDistance = 0, double duration = DefaultDuration, bool global = false)
    {
        var difference = global
            ? destination - node.GlobalPosition
            : destination - node.Position;
         
        destination -= difference.Normalized() * stopDistance;
        
        var propertyPath = global ? "global_position" : "position";
        
        return node.AnimateValue(destination, propertyPath, duration);
    }

    public static Animation MoveBy(this Node3D node, Vector3 displacement, double duration = DefaultDuration,
        bool global = false)
    {
        var finalPos = global
            ? node.GlobalPosition + displacement
            : node.Position + displacement;

        return node.MoveTo(finalPos, duration: duration, global: global);
    } 
    
    public static Animation RotateTo(this Node3D node, float xDeg, float yDeg, float zDeg, bool global = false, double duration = DefaultDuration)
    {
        return node.RotateTo(new Vector3(xDeg, yDeg, zDeg), global: global, duration);
    }
    public static Animation RotateTo(this Node3D node, Vector3 eulerAnglesInDegrees, bool global = false, double duration = DefaultDuration)
    {
        var eulerAnglesInRadians = new Vector3(
            Mathf.DegToRad(eulerAnglesInDegrees.X),
            Mathf.DegToRad(eulerAnglesInDegrees.Y),
            Mathf.DegToRad(eulerAnglesInDegrees.Z)
        );
        return node.RotateTo(Quaternion.FromEuler(eulerAnglesInRadians), global: global, duration);
    }
    public static Animation RotateTo(this Node3D node, Quaternion destination, bool global = false, double duration = DefaultDuration)
    {
        if (duration == 0) duration = TimeEpsilon;
        // var animation = new Animation();

        // Quaternion breaks if scale is zero.
        // Animated rotation is usually useless for zero-scale objects, but can be used
        // alongside scaling animations.
        // Also, animation methods are sometimes used to change values in a non-animated way.
        if (node.Scale.X < TimeEpsilon) node.Scale = Vector3.One * LengthEpsilon;
        
        node.Quaternion = node.Quaternion.Normalized();

        if (global)
        {
            var parent = node.GetParent();
            if (parent is not Node3D node3DParent) return node.AnimateValue(destination, "quaternion", duration);

            destination = Quaternion.FromEuler(node3DParent.GlobalRotation).Inverse() * destination;
        }
        
        return node.AnimateValue(destination, "quaternion", duration);
    }
    public static Animation WalkTo(this Node3D node, Vector3 destination, float stopDistance = 0, double duration = DefaultDuration, double prepTurnDuration = 0.1)
    {
        var difference = destination - node.Position;
        
        var prepRotation = node.RotateTo(new Quaternion(Vector3.Back, difference.Normalized()), global: false, prepTurnDuration);
        var move = node.MoveTo(destination, stopDistance, duration);

        return Parallel(
            prepRotation,
            move
        );
    }
    public static Animation ScaleTo(this Node3D node, Vector3 finalScale, double duration = DefaultDuration)
    {
        // True zero scale causes the rotation to be set to identity. So we'll use a small value instead.
        if (finalScale == Vector3.Zero) finalScale = Vector3.One * LengthEpsilon;
        if(node.Scale == Vector3.Zero) node.Scale = Vector3.One * LengthEpsilon;
        
        return node.AnimateValue(finalScale, "scale", duration);
    }
    public static Animation ScaleTo(this Node3D node, float finalScale, double duration = DefaultDuration)
    {
        return node.ScaleTo(Vector3.One * finalScale, duration);
    }

    public static Animation Pulse(this Node3D node, float scaleFactor = 1.2f, double attack = 0.5, double hold = 0, double decay = 0.5)
    {
        var originalScale = node.Scale; 
        return Series(
            node.ScaleTo(originalScale * 1.2f).WithDuration(attack),
            new Animation().WithDuration(hold),
            node.ScaleTo(originalScale).WithDuration(decay)
        );
    }
    
    // Animating the parent of a node presents a challenge.
    // It's not a property that can be animated directly.
    // So we need to either use a method track or a property with a setter.
    // The method track doesn't work in the editor, so it's not an option.
    // The setter option might work, but I think it would require a new class inheriting from Node3D.
    // And every other custom class deriving from Node3D would need to be updated to use that class.
    // May have to make PrimerNode 
    
    public static Animation AnimateParentChange(this Node node, Node newParent)
    {
        var animation = new Animation();
        var trackIndex = animation.AddTrack(Animation.TrackType.Method);
        
        animation.TrackSetPath(trackIndex, node.GetPath());
        animation.TrackInsertKey(trackIndex, 0, new Godot.Collections.Dictionary
        {
            {"method", "SetParent"},
            {"args", new Godot.Collections.Array { node, node.GetParent()}}
        });
        animation.TrackInsertKey(trackIndex, 0.5f, new Godot.Collections.Dictionary
        {
            {"method", "SetParent"},
            {"args", new Godot.Collections.Array {node, newParent}}
        });
        
        return animation;
    }
    #endregion

    #region RigidBody animation extensions

    private static MeshInstance3D GetMeshInstanceForRigidBody(this RigidBody3D rigidBody)
    {
        var meshes = rigidBody.GetChildren().OfType<MeshInstance3D>().ToArray();
        switch (meshes.Length)
        {
            case > 1:
                PrimerGD.PrintErrorWithStackTrace("RigidBody has more than one child of type MeshInstance3D. This could be fine, but current code assumes just one.");
                break;
            case < 1:
                PrimerGD.PrintErrorWithStackTrace("RigidBody no child of type MeshInstance3D.");
                break;
        }

        return meshes.First();
    }
    
    public static Animation AnimateFreeze(this RigidBody3D rigidBody, bool value, bool resetAtEnd = false, double duration = DefaultDuration)
    {
        return rigidBody.AnimateBool(value, "freeze", resetAtEnd, duration);
    }
    
    public static Animation MoveTo(this RigidBody3D rigidBody,
        Vector3 destination,
        float stopDistance = 0,
        double duration = DefaultDuration,
        bool global = false)
    {
        // When moving a rigid body, we need to keyframe the children instead of the  rigid body itself,
        // since keyframes on the rigidbody will override the physics updates.
        // Also, the keyframes on the children need to be local, since global keyframes
        // will also effectively override the physics updates.
        
        // If the movement was given as local to the rigid body, we need to convert it to local to the children.
        if (!global)
        {
            var intendedLocalRotationTransform = new Transform3D(
                new Basis(1, 0, 0, 0, 1, 0, 0, 0, 1),
                destination - rigidBody.Position
            );
            var finalLocalTransformation = intendedLocalRotationTransform * rigidBody.Transform;
            var localTransformationOfChildren = rigidBody.Transform.Inverse() * finalLocalTransformation;
			         
            destination = localTransformationOfChildren.Origin;
        }
        // Same conversion but for global movement
        if (global)
        {
            var intendedGlobalMovementTransform = new Transform3D(
                new Basis(1, 0, 0, 0, 1, 0, 0, 0, 1),
                destination - rigidBody.GlobalPosition
            );
            var finalGlobalTransformation = intendedGlobalMovementTransform * rigidBody.GlobalTransform;
            var localTransformationOfChildren = rigidBody.GlobalTransform.Inverse() * finalGlobalTransformation;
			         
            destination = localTransformationOfChildren.Origin;
            global = false; // Not used, but just to make it clear that the destination is now local
        }

        return Parallel(
            rigidBody.GetMeshInstanceForRigidBody().MoveTo(destination, stopDistance: stopDistance, duration: duration, global: false),
            rigidBody.GetNode<Node3D>("CollisionShape3D").MoveTo(destination, stopDistance: stopDistance, duration: duration, global: false)
        );
    }
    
    public static Animation RotateTo(this RigidBody3D rigidBody, float xDeg, float yDeg, float zDeg, double duration = DefaultDuration)
    {
        return rigidBody.RotateTo(new Vector3(xDeg, yDeg, zDeg), duration);
    }
    public static Animation RotateTo(this RigidBody3D rigidBody, Vector3 eulerAnglesInDegrees, double duration = DefaultDuration)
    {
        var eulerAnglesInRadians = new Vector3(
            Mathf.DegToRad(eulerAnglesInDegrees.X),
            Mathf.DegToRad(eulerAnglesInDegrees.Y),
            Mathf.DegToRad(eulerAnglesInDegrees.Z)
        );
        return rigidBody.RotateTo(Quaternion.FromEuler(eulerAnglesInRadians), duration);
    }
    
    public static Animation RotateTo(this RigidBody3D rigidBody,
        Quaternion destination,
        double duration = DefaultDuration,
        bool global = false)
    {
        // When moving a rigid body, we need to keyframe the children instead of the  rigid body itself,
        // since keyframes on the rigidbody will override the physics updates.
        // Also, the keyframes on the children need to be local, since global keyframes
        // will also effectively override the physics updates.
        
        // If the movement was given as local to the rigid body, we need to convert it to local to the children.
        if (!global)
        {
            var intendedLocalRotationTransform = new Transform3D(
                new Basis(rigidBody.Quaternion.Inverse() * destination),
                rigidBody.Position
            );
            var finalLocalTransformation = intendedLocalRotationTransform * rigidBody.Transform;
            var localTransformationOfChildren = rigidBody.Transform.Inverse() * finalLocalTransformation;
			         
            destination = new Quaternion(localTransformationOfChildren.Basis);
        }
        // Same conversion but for global movement
        if (global)
        {
            var intendedGlobalRotationTransform = new Transform3D(
                new Basis(Quaternion.FromEuler(rigidBody.GlobalRotation).Inverse() * destination),
                rigidBody.Position
            );
            var finalGlobalTransformation = intendedGlobalRotationTransform * rigidBody.GlobalTransform;
            var localTransformationOfChildren = rigidBody.GlobalTransform.Inverse() * finalGlobalTransformation;
			         
            destination = new Quaternion(localTransformationOfChildren.Basis);
        }
        
        return Parallel(
            rigidBody.GetMeshInstanceForRigidBody().RotateTo(destination, duration: duration),
            rigidBody.GetNode<Node3D>("CollisionShape3D").RotateTo(destination, duration: duration)
        );
    }
    public static Animation ScaleTo(this RigidBody3D rigidBody,
        Vector3 destination,
        double duration = DefaultDuration)
    {
        // When moving a rigid body, we need to keyframe the children instead of the  rigid body itself,
        // since keyframes on the rigidbody will override the physics updates.
        // Also, the keyframes on the children need to be local, since global keyframes
        // will also effectively override the physics updates.

        return Parallel(
            rigidBody.GetMeshInstanceForRigidBody().ScaleTo(destination, duration: duration),
            rigidBody.GetNode<Node3D>("CollisionShape3D").ScaleTo(destination, duration: duration)
        );
    }

    // public static Animation UnfreezeForDuration(this RigidBody3D rigidBody, double duration)
    // {
    //     var animation = new Animation();
    //     var trackIndex = animation.AddTrack(Animation.TrackType.Value);
    //     animation.TrackSetPath(trackIndex, rigidBody.GetPath() + ":freeze");
    //     animation.TrackInsertKey(trackIndex, 0, false);
    //     animation.TrackInsertKey(trackIndex, duration, true);
    //     animation.Length = duration;
    //     return animation;
    // } 

    #endregion
    
    #region Animation modifiers

    public static Animation WithDelay(this Animation animation, double delay = DefaultDuration)
    {
        var newAnimation = new Animation();
        for (var i = 0; i < animation.GetTrackCount(); i++)
        {
            // Add a new track of the same type to newAnimation
            newAnimation.AddTrack(animation.TrackGetType(i));
            newAnimation.TrackSetPath(i, animation.TrackGetPath(i));
            newAnimation.TrackSetInterpolationType(i, animation.TrackGetInterpolationType(i));
            
            for (var j = 0; j < animation.TrackGetKeyCount(i); j++)
            {
                // Set keys
                newAnimation.TrackInsertKey(i, animation.TrackGetKeyTime(i, j) + delay,
                    animation.TrackGetKeyValue(i, j));
            }
        }

        newAnimation.Length = animation.Length + (float)delay;
        return newAnimation;
    }
    
    public static Animation WithDuration(this Animation animation, double duration)
    {
        if (duration == 0) duration = TimeEpsilon;
        var newAnimation = new Animation();
        var lastKeyTime = 0.0;
        for (var i = 0; i < animation.GetTrackCount(); i++)
        {
            var time = animation.TrackGetKeyTime(i, animation.TrackGetKeyCount(i) - 1);
            lastKeyTime = Mathf.Max(lastKeyTime, time);
        }
        var timeScale = duration / lastKeyTime;
        
        for (var i = 0; i < animation.GetTrackCount(); i++)
        {
            // Add a new track of the same type to newAnimation
            newAnimation.AddTrack(animation.TrackGetType(i));
            newAnimation.TrackSetPath(i, animation.TrackGetPath(i));
            newAnimation.TrackSetInterpolationType(i, animation.TrackGetInterpolationType(i));
            
            var prevTime = -TimeEpsilon; // Allows comparison between key times, preventing a space smaller than epsilon
                                             // It's -TimeEpsilon so the first key will land on 0 if incremented by TimeEpsilon
            
            for (var j = 0; j < animation.TrackGetKeyCount(i); j++)
            {
                var time = Mathf.Max(animation.TrackGetKeyTime(i, j) * timeScale, prevTime + TimeEpsilon);
                // Set keys
                newAnimation.TrackInsertKey(i, time, animation.TrackGetKeyValue(i, j));
                prevTime = time;
            }
        }

        newAnimation.Length = (float)duration;
        return newAnimation;
    }

    public static Animation WithSpeedFactor(this Animation animation, double speedFactor)
    {
        if (speedFactor == 0) PrimerGD.PrintErrorWithStackTrace("Can't have an animation with speed factor zero. It would be infinity long lmao.");
        
        return animation.WithDuration(animation.Length / speedFactor);
    }
    public static Animation WithDurationMultiplier(this Animation animation, double multiplier)
    {
        return animation.WithDuration(animation.Length * multiplier);
    }

    public static Animation WithClampedDuration(this Animation animation, double maxDuration)
    {
        GD.Print($"Duration before clamp: {animation.Length}");
        return animation.WithDuration(Mathf.Min(animation.Length, maxDuration));
    }
    
    #endregion

    #region Material animation
    public static Animation AnimateColorHsv(this MeshInstance3D meshInstance3D, Color finalColor, double duration = DefaultDuration)
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
                animation.TrackInsertKey(trackIndex, duration * (1 - material.AlbedoColor.H) / hueDiff + TimeEpsilon, 0f);
                animation.TrackInsertKey(trackIndex, duration, finalColor.H);
                animation.TrackSetPath(trackIndex, meshInstance3D.GetPath()+":surface_material_override/0:albedo_color:h");
            }
            else // If the final hue is greater than the initial hue, we need to wrap around 0
            {
                hueDiff = 1 - MathF.Abs(hueDiff);
                animation.TrackInsertKey(trackIndex, 0f, material.AlbedoColor.H);
                animation.TrackInsertKey(trackIndex, duration * material.AlbedoColor.H / hueDiff, 0f);
                animation.TrackInsertKey(trackIndex, duration * material.AlbedoColor.H / hueDiff + TimeEpsilon, 1f);
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

        animation.Length = (float)duration;
        return animation;
    }
    
    public static Animation AnimateColorRgb(this MeshInstance3D meshInstance3D, Color finalColor, double duration = DefaultDuration)
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
            if (memberAnimation == null)
            {
                // TODO: Should I just allow nulls and replace them here?
                // Might even be better because I can always have them with duration zero here
                // and I might forget to use the correct duration elsewhere.
                
                GD.PrintErr("Can't combine a null animation. Use an empty animation instead.");
                var stackTrace = new System.Diagnostics.StackTrace(true);
                throw new Exception("Can't combine a null animation. Use an empty animation instead. \n" + stackTrace);
            }
            
            var animLength = 0.0;
            var trackCount = memberAnimation.GetTrackCount();
            
            // This is for handling "null" animations with nonzero length.
            // which are used as delays when combining animations in series.
            // Perhaps other situations as well.
            // The current situation at time of writing is that I want an animation to be 
            // keyframed if in edit mode for previews, but handled by physics if in play mode.
            // But we want to time taken the by the animation to be the same in both cases.
            if (trackCount == 0) { animLength = memberAnimation.Length; }
            
            // Loop through the tracks in each animation
            for (var i = 0; i < trackCount; i++)
            {
                var path = memberAnimation.TrackGetPath(i);
                var trackType = memberAnimation.TrackGetType(i);
                var trackInterpolationType = memberAnimation.TrackGetInterpolationType(i);
                if (trackType == Animation.TrackType.Animation)
                {
                    GD.PushWarning($"AnimationUtilities.CombineAnimations may not work well with animation playback tracks. Track path: {memberAnimation.TrackGetPath(i)}");
                }

                int trackIndex;
                if (trackPaths.Contains((path, trackType)) && trackType != Animation.TrackType.Method)
                {
                    trackIndex = trackPaths.IndexOf((path, trackType));
                    if (trackInterpolationType != newAnimation.TrackGetInterpolationType(trackIndex)) 
                    {
                        GD.PushWarning($"AnimationUtilities.CombineAnimations: Interpolation type mismatch for path {path}. " +
                                        $"Using first interpolation type {newAnimation.TrackGetType(trackIndex)} instead of {trackType}.");
                    }
                }
                else
                {
                    trackIndex = newAnimation.AddTrack(trackType);
                    newAnimation.TrackSetInterpolationType(trackIndex, trackInterpolationType);
                    newAnimation.TrackSetPath(trackIndex, path);
                    trackPaths.Add((path, trackType));
                }

                var prevKeyTime = -1.0;
                // Loop through the keys in each track
                for (var j = 0; j < memberAnimation.TrackGetKeyCount(i); j++)
                {
                    var keyTime = memberAnimation.TrackGetKeyTime(i, j);
                    animLength = Mathf.Max(animLength, keyTime);
                    newAnimation.TrackInsertKey(trackIndex, keyTime + (parallel ? 0 : finalAnimationLength), memberAnimation.TrackGetKeyValue(i, j));

                    if (keyTime - prevKeyTime < TimeEpsilon)
                        PrimerGD.PrintErrorWithStackTrace(
                            $"Keys {j - 1} and {j} are too close on track {memberAnimation.TrackGetPath(i)}." +
                            $"\n Separation: {keyTime - prevKeyTime}"
                        );
                }
            }

            if (memberAnimation.Length > 0 && memberAnimation.Length + TimeEpsilon < animLength)
            {
                GD.PushWarning($"Animation has length {memberAnimation.Length} less than the time of the latest keyframe {animLength}. Usually this is a mistake.");
            }
            // If the memberAnimation.Length was set to a nonzero value, we use animLength,
            // which is the time of the latest keyframe. Otherwise, we respect memberAnimation.Length.
            animLength = memberAnimation.Length == 0 ? animLength : memberAnimation.Length;
            if (animLength == 0) GD.PushWarning($"Animation has length of zero. Idk which one, lmao.");
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