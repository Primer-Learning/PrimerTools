// using System;
// using System.Collections.Generic;
// using Godot;
//
// namespace PrimerTools.Simulation;

// public partial class NodeEntityRegistry<TEntity, TNode> : Node3D
//     where TEntity : IDataEntity
//     where TNode : NodeEntity<TEntity>, new()
// {
//     private static readonly Dictionary<Type, Type> EntityToNodeMap = new()
//     {
//         { typeof(DataTree), typeof(NodeTree) },
//         { typeof(DataCreature), typeof(NodeCreature) }
//     };
//
//     public readonly List<TNode> Entities = new();
//     // public List<TNode> Entities => GetChildren().OfType<TNode>().ToList();
//
//     public void RegisterEntity(TEntity dataEntity)
//     {
//         if (!EntityToNodeMap.TryGetValue(typeof(TEntity), out var nodeType))
//         {
//             throw new InvalidOperationException($"No corresponding Node type found for {typeof(TEntity)}");
//         }
//
//         var node = new TNode();
//         AddChild(node);
//         node.Initialize(dataEntity);
//         Entities.Add(node);
//     }
//
//     public void RemoveEntity(int index)
//     {
//         Entities.RemoveAt(index);
//         // Don't queuefree here. It can queuefree itself after an animation
//         // GetChild(index).QueueFree();
//     }
//
//     public void Reset()
//     {
//         foreach (var child in Entities)
//         {
//             child.QueueFree();
//         }
//         Entities.Clear();
//     }
// }
