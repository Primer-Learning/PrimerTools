using Godot;

namespace PrimerTools;

public static class NodeExtensions
{
    // This is for scenes that instantiate packed scenes, if you want to edit the children in the editor
    // It breaks inheritance, so use with caution. Or abandon! I'm not the boss of you.
    
    // This one may not be necessary, since it's equivalent to the other one if you feed it the same node.
    public static void MakeAncestorsLocal(this Node parent)
    {
        parent.MakeChildrenLocalRecursively(parent);
    }
    public static void MakeChildrenLocalRecursively(this Node parent, Node ancestorWhoNodesAreLocalWithRespectTo, int depth = 0)
    {
        if (depth > 20)
        {
            GD.Print($"WHOA. Depth is {depth} at node", parent.Name);
            return;
        } 
        foreach (var child in parent.GetChildren())
        {
            child.Owner = ancestorWhoNodesAreLocalWithRespectTo;
            child.SceneFilePath = "";
            child.MakeChildrenLocalRecursively(ancestorWhoNodesAreLocalWithRespectTo, depth: depth + 1);
        }
    }
}