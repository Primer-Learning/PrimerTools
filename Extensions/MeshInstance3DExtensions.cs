using Godot;

namespace PrimerTools;

public static class MeshInstance3DExtensions
{
    public static StandardMaterial3D GetOrCreateOverrideMaterial(this MeshInstance3D meshInstance3D, int index = 0)
    {
        // Currently, this creates a new material if there's no existing override. I'm not certain whether this
        // is terribly inefficient. I know Godot has inherited materials or something like that, which should be 
        // more efficient, but I don't know if this uses that under the hood or what.
        
        // Use the surface material override if it's there and a StandardMaterial3D
        var currentOverride = meshInstance3D.GetSurfaceOverrideMaterial(index);
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
        var meshMaterial = meshInstance3D.Mesh.SurfaceGetMaterial(index);
        var newOverrideMaterial = meshMaterial != null
            ? (StandardMaterial3D)meshMaterial.Duplicate()
            : new StandardMaterial3D();
            
        meshInstance3D.SetSurfaceOverrideMaterial(0, newOverrideMaterial);
        return newOverrideMaterial;
    }

    public static void SetColorOfAllMaterials(this MeshInstance3D meshInstance3D, Color color)
    {
        for (var i = 0; i < meshInstance3D.GetSurfaceOverrideMaterialCount(); i++)
        {
            meshInstance3D.GetOrCreateOverrideMaterial(i).AlbedoColor = color;
        }
    }
}