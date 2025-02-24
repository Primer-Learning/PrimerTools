using Godot;
// using PrimerAssets.Trees;
using PrimerTools;

[Tool]
public partial class FruitTree : Node3D
{
	public Rng Rng;
	// Only the medium tree for now
	public static readonly PackedScene TreeScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Simulation/Models/Medium mango tree/mango_tree_medium.tscn");
	private static readonly PackedScene MangoScene =
		ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Simulation/Models/Mango/mango.blend");
	private static readonly Pool<Node3D> MangoPool = new (MangoScene);

	public static FruitTree CreateInstance()
	{
		return TreeScene.Instantiate<FruitTree>();
	}

	public Animation GrowFruitAnimation(int flowerIndex)
	{
		var fruit = PrepareFruitForGrowth(flowerIndex);
		
		if (!IsFruitOnFlower(fruit))
		{
			RotateFlowerRandomly(flowerIndex);
			return AnimationUtilities.Series(
				AnimationUtilities.Parallel(
					fruit.MoveTo(Vector3.Zero).WithDuration(0),
					fruit.ScaleTo(0).WithDuration(0)
				),
				fruit.ScaleTo(1)
			);
		}

		if (!IsFruitMature(fruit))
		{
			return fruit.ScaleTo(1);
		}

		return new Animation();
	}

	public Tween GrowFruitTween(int flowerIndex, double duration)
	{
		var fruit = PrepareFruitForGrowth(flowerIndex);
		
		if (!IsFruitOnFlower(fruit))
		{
			RotateFlowerRandomly(flowerIndex);
			fruit.Position = Vector3.Zero;
		}
		
		
		fruit.Scale = Vector3.Zero;
		var tween = CreateTween();
		tween.TweenProperty(
			fruit,
			"scale",
			Vector3.One,
			duration
		);
		return tween;
	}

	private Node3D PrepareFruitForGrowth(int flowerIndex)
	{
		if (flowerIndex is < 0 or > 3)
		{
			GD.PrintErr("Invalid flower index");
			return null;
		}
		var flower = GetNode<Node3D>($"Flower {flowerIndex}");

		// Handle new fruit
		if (flower.GetChildren().Count == 0)
		{
			RotateFlowerRandomly(flowerIndex);
			var mango = MangoPool.GetFromPool();
			flower.AddChild(mango);
			mango.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
			mango.Position = Vector3.Zero;
			mango.Scale = Vector3.Zero;
			return mango;
		}
		// Handle fruit that exists but has been moved/scaled
		return flower.GetChild<Node3D>(0);
	}

	private void RotateFlowerRandomly(int flowerIndex)
	{
		var flower = GetNode<Node3D>($"Flower {flowerIndex}");
		flower.Quaternion = Transform3DUtils.QuaternionFromEulerDeg(new Vector3(this.Rng.RangeFloat(0, 5), this.Rng.RangeFloat(0, 360), this.Rng.RangeFloat(0, 5)));
	}
	private bool IsFruitOnTheFlowerAndMature(Node3D fruit)
	{
		return IsFruitMature(fruit) &&
		       IsFruitOnFlower(fruit);
	}
	private static bool IsFruitMature(Node3D fruit)
	{
		return fruit.Scale == Vector3.One;
	}
	public bool IsFruitOnFlower(Node3D fruit)
	{
		return fruit.Position.LengthSquared() < 0.0001f;
	}

	public Node3D GetFruit(int index)
	{
		var flower = GetNode<Node3D>($"Flower {index}");

		return flower.GetChildren().Count == 0 ? null : flower.GetChild<Node3D>(0);
	}

	public void ClearFruit(int index)
	{
		var flower = GetNode<Node3D>($"Flower {index}");
		foreach (var fruit in flower.GetChildren())
		{
			fruit.Free();
		}
	}

	public void ClearAllFruit()
	{
		for (var i = 0; i < 4; i++)
		{
			ClearFruit(i);
		}
	}

	public Node3D FindNearestFruit(Node3D node)
	{
		Node3D nearestFruit = null;
		var nearestDistance = float.MaxValue;
		for (var i = 0; i < 4; i++)
		{
			var flower = GetNode<Node3D>($"Flower {i}");
			
			if (flower.GetChildren().Count == 0) { continue; }

			var fruit = flower.GetChild<Node3D>(0);
			if (!IsFruitOnTheFlowerAndMature(fruit)) { continue; }
			
			var distance = node.GlobalPosition.DistanceSquaredTo(fruit.GlobalPosition);
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearestFruit = fruit;
			}
		}
		return nearestFruit;
	}
}
