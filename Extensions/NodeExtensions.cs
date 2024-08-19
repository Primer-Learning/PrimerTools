using Godot;

namespace PrimerTools;

public static class NodeExtensions
{
    // This is for scenes that instantiate packed scenes, if you want to edit the children in the editor
    // It breaks inheritance, so use with caution. Or abandon! I'm not the boss of you.
    public static void MakeSelfAndChildrenLocal(this Node parent, Node ancestorWhoNodesAreLocalWithRespectTo = null, int depth = 0)
    {
        if (ancestorWhoNodesAreLocalWithRespectTo == null)
        {
            ancestorWhoNodesAreLocalWithRespectTo = parent.GetTree().EditedSceneRoot;
        }
        
        parent.Owner = ancestorWhoNodesAreLocalWithRespectTo;
        parent.SceneFilePath = "";
        if (depth > 100)
        {
            GD.Print($"WHOA. Depth is {depth} at node", parent.Name);
            return;
        }
        foreach (var child in parent.GetChildren())
        {
            GD.Print("Child");
            child.MakeSelfAndChildrenLocal(ancestorWhoNodesAreLocalWithRespectTo, depth: depth + 1);
        }
    }
    
    public static void SetParent(this Node node, Node parent)
    {
        // AddChild doesn't work if there is already a parent. So we gotsta do this. DUMB IMO, but whatever.
        if (node.GetParent() != null)
        {
            node.GetParent().RemoveChild(node);
        }

        parent?.AddChild(node);
    }
}