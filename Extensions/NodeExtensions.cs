using Godot;

namespace PrimerTools;

public static class NodeExtensions
{
    // This is for scenes that instantiate packed scenes, if you want to edit the children in the editor
    // It breaks inheritance, so use with caution. Or abandon! I'm not the boss of you.
    public static void MakeAncestorsLocal(this Node parent)
    {
        MakeChildrenLocalRecursively(parent, parent);
    }
    private static void MakeChildrenLocalRecursively(Node parent, Node ancestorWhoNodesAreLocalWithRespectTo)
    {
        foreach (var child in parent.GetChildren())
        {
            child.Owner = ancestorWhoNodesAreLocalWithRespectTo;
            child.SceneFilePath = "";
            MakeChildrenLocalRecursively(parent, ancestorWhoNodesAreLocalWithRespectTo);
        }
    }
}