using Godot;

[Tool]
public partial class ImageDisplayMesh : MeshInstance3D
{
    private Texture2D imageTexture;
    private Vector2 size;
    public Vector2 Size => size;
    [Export] public Texture2D ImageTexture
    {
        get => imageTexture;
        set
        {
            imageTexture = value;
            size = new Vector2(value.GetWidth(), value.GetHeight());
            Update();
        }
    }

    private void Update()
    {
        if (ImageTexture == null)
        {
            GD.PrintErr("ImageTexture is not assigned.");
            return;
        }

        // Create a plane mesh with the same aspect ratio as the image
        var planeMesh = new PlaneMesh();
        planeMesh.Size = size;

        // Assign the mesh to the MeshInstance3D
        Mesh = planeMesh;

        // Create a new material and set the image texture
        var material = new StandardMaterial3D();
        material.AlbedoTexture = ImageTexture;

        // Assign the material to the mesh surface
        SetSurfaceOverrideMaterial(0, material);
    }
}