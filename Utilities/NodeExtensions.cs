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
            // Fail loud: the helper has nothing useful to do when its target
            // is detached from the tree, and silently no-oping makes the
            // underlying bug invisible. Caller can guard with IsInsideTree()
            // if a no-op is genuinely the right behaviour.
            if (!parent.IsInsideTree())
            {
                GD.PushWarning(
                    $"MakeSelfAndChildrenLocal called on '{parent.Name}' ({parent.GetType().Name}) " +
                    "which is not inside the scene tree — skipping. The caller created or kept " +
                    "a reference to a detached node.");
                return;
            }
            ancestorWhoNodesAreLocalWithRespectTo = parent.GetTree().EditedSceneRoot;
            if (ancestorWhoNodesAreLocalWithRespectTo == null) return; // not in editor / nothing edited
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
            child.MakeSelfAndChildrenLocal(ancestorWhoNodesAreLocalWithRespectTo, depth: depth + 1);
        }
    }

    public static void MakeLocal(this Node self, Node ancestorWhoNodesAreLocalWithRespectTo = null)
    {
        if (ancestorWhoNodesAreLocalWithRespectTo == null)
        {
            ancestorWhoNodesAreLocalWithRespectTo = self.GetTree().EditedSceneRoot;
        }
        
        self.Owner = ancestorWhoNodesAreLocalWithRespectTo;
        self.SceneFilePath = "";
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
    
    public static bool SaveAsPackedScene(this Node node, string scenePath)
    {
        // First, set the owner property for all children recursively
        SetOwnersRecursive(node, node);
        
        // Create a new PackedScene and pack the node
        var packedScene = new PackedScene();
        var packResult = packedScene.Pack(node);
        
        if (packResult != Error.Ok)
        {
            GD.PrintErr($"Failed to pack scene: {packResult}");
            return false;
        }
        
        // Ensure the path has the correct extension
        if (!scenePath.EndsWith(".tscn") && !scenePath.EndsWith(".scn"))
        {
            scenePath += ".tscn";
        }
        
        // In editor, ensure the directory exists
        if (OS.HasFeature("editor"))
        {
            // Extract directory path using Godot's path handling
            var pathParts = scenePath.Split('/');
            if (pathParts.Length > 1)
            {
                // Reconstruct the directory path without the filename
                var dir = string.Join("/", pathParts[..^1]);
                
                var dirAccess = DirAccess.Open("res://");
                if (dirAccess != null && !dirAccess.DirExists(dir.Replace("res://", "")))
                {
                    dirAccess.MakeDirRecursive(dir.Replace("res://", ""));
                }
            }
        }
        
        // Save the packed scene to disk
        var saveResult = ResourceSaver.Save(packedScene, scenePath);
        
        if (saveResult != Error.Ok)
        {
            GD.PrintErr($"Failed to save scene to {scenePath}: {saveResult}");
            return false;
        }
        
        GD.Print($"Scene successfully saved to: {scenePath}");
        return true;
    }
    
    private static void SetOwnersRecursive(Node node, Node owner)
    {
        foreach (Node child in node.GetChildren())
        {
            child.Owner = owner;
            SetOwnersRecursive(child, owner);
        }
    }
    
    // Optional: Overload that automatically generates a path based on node name
    public static bool SaveAsPackedScene(this Node node)
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string safeName = node.Name.ToString().Replace(" ", "_");
        string path = $"res://saved_scenes/{safeName}_{timestamp}.tscn";
        
        return node.SaveAsPackedScene(path);
    }
}