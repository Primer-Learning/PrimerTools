using System.Linq;
using Godot;
using PrimerTools;

/// <summary>
/// Meant for objects that need both keyframed movements AND physics movement via rigidbody.
/// RigidBodyEnsemble should have children that are RigidBodies.
/// The class overrides Node3D animation extensions, passing the animations to the RigidBody children instead,
/// allowing us to treat the ensembles as if they were being keyframed without actually keyframing them,
/// which would break the physics calculations (somehow) when the RigidBodies are unfrozen.
/// </summary>
public partial class RigidBodyEnsemble : Node3D
{
	public Animation MoveTo(Vector3 destination, float duration = 0.5f, bool global = false)
	{
		var rigidChildren = GetChildren().OfType<RigidBody3D>().ToArray();
		var childDestinations = new Vector3[rigidChildren.Length];
		
		var finalGlobalTransformation = new Transform3D(
			GlobalTransform.Basis, // Same rotation
			destination // New position
		);
		if (!global)
		{
			GD.PushWarning("Local rigidbody movement is untested.");
			var finalLocalTransformation = new Transform3D(
				Transform.Basis, // Same rotation
				destination // New position
			);
			finalGlobalTransformation = GetParent<Node3D>().GlobalTransform * finalLocalTransformation;
			global = true;
		}
		
		for (var i = 0; i < rigidChildren.Length; i++)
		{
			childDestinations[i] = (finalGlobalTransformation * rigidChildren[i].Transform).Origin;
		}

		return rigidChildren.Select((x, i) => x.MoveTo(childDestinations[i], duration: duration, global: global))
			.InParallel();
	}
	
	public Animation ScaleTo(Vector3 finalScale, float duration = 0.5f)
	{
		var rigidChildren = GetChildren().OfType<RigidBody3D>();
		return rigidChildren.Select(x => x.ScaleTo(finalScale, duration: duration)).InParallel();
	}
	public Animation ScaleTo(float finalScale, float duration = 0.5f)
	{
		return ScaleTo(Vector3.One * finalScale, duration);
	}

	public virtual Animation Break(float duration = 0.5f, bool forever = false)
	{
		var rigidChildren = GetChildren().OfType<RigidBody3D>();
		// This should always reset to ensure the rigidbody is frozen when the animation starts playing.
		// But if 'forever', just re-freeze after 1000s but correct the animation's Length property.
		var animation = rigidChildren.Select(x => x.AnimateFreeze(false, resetAtEnd: true, duration: forever ? 1000 : duration)).InParallel();
		animation.Length = duration;
		return animation;
	}
}
