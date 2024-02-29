using System.Collections.Generic;
using Godot;

namespace PrimerTools;

public class Pool<T> : Stack<T> where T : Node3D, new()
{
    private readonly PackedScene scene;
    public Pool(PackedScene scene = null, int initialSize = 1)
    {
        this.scene = scene;
        
        for (var i = 0; i < initialSize; i++)
        {
            NewPooledObject();
        }
    }
    public void ReturnToPool(T node, bool makeInvisible = true, bool unparent = true)
    {
        if (makeInvisible) { node.Visible = false; }
        if (unparent) { node.SetParent(null); }
        Push(node);
    }

    private T NewPooledObject()
    {
        if (scene == null)
        {
            return new T();
        }
        return (T)scene.Instantiate();
    }
    
    public T GetFromPool(Node3D parent = null)
    {
        var pooledNode = Count > 0 ? Pop() : NewPooledObject();
        pooledNode.Visible = true;
        return pooledNode;
    }
}