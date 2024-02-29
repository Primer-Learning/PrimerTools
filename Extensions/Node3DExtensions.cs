using Godot;

public static class Node3DExtensions
{
    public static void SetWorldPosition(this Node3D node, Vector3 newPosition)
    {
        node.GlobalTransform = new Transform3D(node.GlobalTransform.Basis, newPosition);
    }
}