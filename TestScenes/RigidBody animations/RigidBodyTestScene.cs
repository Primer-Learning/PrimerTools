using Godot;
using PrimerTools;

[Tool]
public partial class RigidBodyTestScene : AnimationSequence
{
	private Rng rng = new Rng(0);

	protected override void Define()
	{
		// Ground
		var ground = new MeshInstance3D();
		ground.Name = "ground";
		AddChild(ground);
		var groundMesh = new PlaneMesh();
		ground.Mesh = groundMesh;
		var groundMaterial = new StandardMaterial3D();
		groundMaterial.AlbedoColor = new Color(1, 1, 1, 0);
		// groundMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		groundMesh.Material = groundMaterial;
		groundMesh.Size = new Vector2(100, 100);
		var groundStaticBody = new StaticBody3D();
		var groundCollisionShape3D = new CollisionShape3D();
		var groundCollisionShape = new WorldBoundaryShape3D();
		groundCollisionShape.Plane = new Plane(0, 1, 0, 0);
		groundStaticBody.AddChild(groundCollisionShape3D);
		groundCollisionShape3D.Shape = groundCollisionShape;
		ground.AddChild(groundStaticBody);
		ground.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
		
		var paperParent = new Node3D();
		AddChild(paperParent);
		paperParent.Name = "PaperParent";
		paperParent.Position = Vector3.Right * 4;
		
		var cube2 = GD.Load<PackedScene>("res://Video scenes/rigid_cube.tscn").Instantiate<RigidBody3D>();
		AddChild(cube2);
		cube2.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
		cube2.Position = Vector3.Left * 2;
		
		var paper = Paper.PaperScene.Instantiate<Paper>();
		paperParent.AddChild(paper);
		paperParent.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
		paperParent.RotationDegrees = new Vector3(0, 90, 0);
		
		RegisterAnimation(
			AnimationUtilities.Series(
				AnimationUtilities.Parallel(
					paper.MoveTo(new Vector3(0, 0, 0), global: true)
					// paper.RotateTo(Quaternion.FromEuler(new Vector3(Mathf.Pi / 4, Mathf.Pi / 4, Mathf.Pi / 4))),
					// paper.ScaleTo(Vector3.One * 3)
				)
				// paper.Break()
			),
			cube2.MoveTo(Vector3.Left * 3, duration: 3)
		);
	}
}
