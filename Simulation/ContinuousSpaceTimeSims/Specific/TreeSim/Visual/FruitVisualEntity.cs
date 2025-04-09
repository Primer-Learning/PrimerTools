using System.Linq;
using Godot;
using PrimerTools.Simulation.Components;

namespace PrimerTools.Simulation.Visual;

public partial class FruitVisualEntity : VisualEntity
{
    private static readonly PackedScene MangoScene = 
        ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Simulation/Models/Mango/mango.scn");
    
    private Node3D _fruitModel;
    
    public override void _Ready()
    {
        base._Ready();
        
        // Create fruit model
        _fruitModel = MangoScene.Instantiate<Node3D>();
        AddChild(_fruitModel);
        
        // Start with zero scale
        _fruitModel.Scale = Vector3.Zero;
        
        Name = "Fruit";
    }
    
    public override void Initialize(EntityRegistry registry, EntityId entityId)
    {
        base.Initialize(registry, entityId);
        
        if (registry.TryGetComponent<FruitComponent>(entityId, out var fruit))
        {
            Transform = fruit.Body.Transform;
            _fruitModel.Scale = Vector3.One * fruit.GrowthProgress;
        }
        
        AddDebugNodes(fruit.Body);
    }
    
    public override void Update(EntityRegistry registry)
    {
        if (!registry.TryGetComponent<FruitComponent>(EntityId, out var fruit))
            return;
        
        // Update transform from physics body
        Transform = fruit.Body.Transform;
        
        // Update scale based on growth progress
        _fruitModel.Scale = Vector3.One * fruit.GrowthProgress;
    }
    
    // public void HandleRipened()
    // {
    //     // Visual effect for ripening (could change color or material)
    //     var material = new StandardMaterial3D();
    //     material.AlbedoColor = new Color(1.0f, 0.8f, 0.0f); // More yellow/golden when ripe
    //     
    //     // Apply to mesh instance if available
    //     if (_fruitModel.GetChild<MeshInstance3D>(0) is MeshInstance3D meshInstance)
    //     {
    //         meshInstance.MaterialOverride = material;
    //     }
    // }
    
    // public void HandleDetached()
    // {
    //     // Visual effect for detachment (could play animation or particle effect)
    //     // For now, just rotate slightly to show it's loose
    //     var tween = CreateTween();
    //     tween.TweenProperty(
    //         _fruitModel,
    //         "rotation",
    //         new Vector3(Mathf.DegToRad(15), 0, 0),
    //         0.3f
    //     );
    // }
    
    public void HandleDecayed()
    {
        // Do this first because the areas don't like having zero scale
        foreach (var areaNode in GetChildren().OfType<Area3D>())
        {
            areaNode.QueueFree();
        }
        // Visual effect for decay (fade out and remove)
        var tween = CreateTween();
        tween.TweenProperty(
            this,
            "scale", // Alpha channel
            Vector3.Zero,
            0.5f
        );
        tween.TweenCallback(Callable.From(() => QueueFree()));
    }
}
