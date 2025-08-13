using Godot;
using PrimerTools;

public partial class CanvasImitator : Node3D
{
	// The purpose of this class is to make a 2D space that appears in the camera preview.
	// This is useful for composing scenes that involve both 2D and 3D objects.

	protected int SubViewPortWidth = 1920;
	protected int SubViewPortHeight = 1080;
    
	protected float CamFov => GetParent<Camera3D>().Fov;
	protected float CamNearPlane => GetParent<Camera3D>().Near;
	protected float DoubleCamNearPlane => 2 * GetParent<Camera3D>().Near;
	protected MeshInstance3D DisplayMeshInstance;
	protected SubViewport SubViewPort;

	public void CreateDisplayMesh()
	{
		var subViewPortContainer = new SubViewportContainer();
		AddChild(subViewPortContainer);
		subViewPortContainer.Owner = GetTree().EditedSceneRoot;
        
		SubViewPort = new SubViewport();
		subViewPortContainer.AddChild(SubViewPort);
		SubViewPort.Owner = GetTree().EditedSceneRoot;
		SubViewPort.Size = new Vector2I(SubViewPortWidth, SubViewPortHeight);
		SubViewPort.TransparentBg = true;
        
		DisplayMeshInstance = new MeshInstance3D();
		var viewPortRendererMesh = new PlaneMesh();
		viewPortRendererMesh.Size = new Vector2(16f/9, 1);
		DisplayMeshInstance.Mesh = viewPortRendererMesh;
		AddChild(DisplayMeshInstance);
		DisplayMeshInstance.Owner = GetTree().EditedSceneRoot;
        
		var mat = new StandardMaterial3D();
		mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		mat.AlbedoTexture = SubViewPort.GetTexture();
		DisplayMeshInstance.Mesh.SurfaceSetMaterial(0, mat);
        
		// Transformation
		DisplayMeshInstance.RotationDegrees = new Vector3(90, 0, 0);
		DisplayMeshInstance.Position = new Vector3(0, 0, -DoubleCamNearPlane);
		var scale = 2 * DoubleCamNearPlane * Mathf.Tan(CamFov / 2 * Mathf.Pi / 180);
		DisplayMeshInstance.Scale = Vector3.One * scale;
	}
}
	
