using Godot;
using PrimerTools;

namespace PrimerTools.TweenSystem;

public static class Node3DStateChangeExtensions
{
    private const float LengthEpsilon = 0.000001f;
    public const double DefaultDuration = 0.5;
    public static PropertyStateChange MoveTo(this Node3D node, Vector3 destination, float stopDistance = 0, bool global = false)
    {
        // If destination was given in global space, figure out the equivalent point expressed in local space.
        if (global)
        {
            destination = node.ToLocal(destination);
        }

        var difference = destination - node.Position;
        destination -= difference.Normalized() * stopDistance;

        return new PropertyStateChange(node, "position", destination);
    }

    public static PropertyStateChange MoveBy(this Node3D node, Vector3 displacement, bool
        global = false)
    {
        var finalPos = global
            ? node.ToLocal(node.GlobalPosition + displacement)
            : node.Position + displacement;

        return new PropertyStateChange(node, "position", finalPos);
    }

    public static PropertyStateChange ScaleTo(this Node3D node, Vector3 finalScale)
    {
        // True zero scale causes the rotation to be set to identity. So we'll use a small value instead.
        if (finalScale == Vector3.Zero) finalScale = Vector3.One * LengthEpsilon;
        if (node.Scale == Vector3.Zero) node.Scale = Vector3.One * LengthEpsilon;

        return new PropertyStateChange(node, "scale", finalScale);
    }

    public static PropertyStateChange ScaleTo(this Node3D node, float finalScale)
    {
        return node.ScaleTo(Vector3.One * finalScale);
    }
    
    public static PropertyStateChange RotateTo(this Node3D node, float xDeg, float yDeg, float zDeg, bool global = false)
    {
        return node.RotateTo(new Vector3(xDeg, yDeg, zDeg), global);
    }
    
    public static PropertyStateChange RotateTo(this Node3D node, Vector3 eulerAnglesInDegrees, bool global = false)
    {
        var eulerAnglesInRadians = new Vector3(
            Mathf.DegToRad(eulerAnglesInDegrees.X),
            Mathf.DegToRad(eulerAnglesInDegrees.Y),
            Mathf.DegToRad(eulerAnglesInDegrees.Z)
        );
        return node.RotateTo(Quaternion.FromEuler(eulerAnglesInRadians), global);
    }
    
    public static PropertyStateChange RotateTo(this Node3D node, Quaternion destination, bool global = false)
    {
        // Quaternion breaks if scale is zero.
        if (node.Scale.X < LengthEpsilon) node.Scale = Vector3.One * LengthEpsilon;
        
        if (global)
        {
            var parent = node.GetParent();
            if (parent is Node3D node3DParent)
            {
                destination = Quaternion.FromEuler(node3DParent.GlobalRotation).Inverse() * destination;
            }
        }

        destination = destination.Normalized();
        
        // For now, we'll use rotation property which uses Euler angles
        // This isn't ideal for all rotations but matches the current PropertyStateChange capabilities
        return new PropertyStateChange(node, "quaternion", destination);
    }
    
    public static CompositeStateChange WalkTo(this Node3D node, Vector3 destination, float stopDistance = 0, double prepTurnDuration = 0.1)
    {
        var difference = destination - node.Position;
        
        var composite = new CompositeStateChange().WithName($"{node.Name}.WalkTo({destination})");
        
        // Add the prep rotation
        var prepRotation = node.RotateTo(new Quaternion(Vector3.Back, difference.Normalized()), global: false)
            .WithDuration(prepTurnDuration);
        composite.AddStateChange(prepRotation);
        
        // Add the movement in parallel with the rotation
        var move = node.MoveTo(destination, stopDistance);
        composite.AddStateChangeInParallel(move);
        
        return composite;
    }
    
    public static CompositeStateChange Pulse(this Node3D node, float scaleFactor = 1.2f, double attack = 0.5, double hold = 0, double decay = 0.5)
    {
        var originalScale = node.Scale;
        
        var composite = new CompositeStateChange().WithName($"{node.Name}.Pulse()");
        
        // Scale up
        composite.AddStateChange(node.ScaleTo(originalScale * scaleFactor).WithDuration(attack));
        
        // Hold (if any)
        if (hold > 0)
        {
            // Add a delay by using a dummy state change
            // We could also add a DelayStateChange class for this purpose
            composite.AddStateChange(new PropertyStateChange(node, "scale", originalScale * scaleFactor).WithDuration(hold));
        }
        
        // Scale back down
        composite.AddStateChange(node.ScaleTo(originalScale).WithDuration(decay));
        
        return composite;
    }
}
