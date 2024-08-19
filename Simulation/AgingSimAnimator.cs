using Godot;
using System;
using System.Collections.Generic;
using PrimerTools;

[Tool]
public partial class AgingSimAnimator : Node3D
{
	#region Running button
	private bool _run = true;
	[Export]
	private bool Run
	{
		get => _run;
		set
		{
			if (!value && _run && Engine.IsEditorHint())
			{
				GD.Print("Hello");
				Reset();
				sim = new AgingSim();
				sim.Initialize();
				DrawBlobs();
				
				if (makeChildrenLocal)
				{
					this.MakeSelfAndChildrenLocal(GetTree().EditedSceneRoot);
				}
			}
			_run = true;
		}
	}

	#endregion
	
	[Export] private bool makeChildrenLocal;

	private AgingSim sim;
	private Dictionary<int, Blob> blobsByID = new();
	private void Reset()
	{
		foreach (var child in GetChildren())
		{
			child.Free();
		}

		blobsByID.Clear();
	}

	private void DrawBlobs()
	{
		foreach (var entityID in sim.CurrentEntities)
		{
			var blob = Blob.NewBlob();
			AddChild(blob);
			blob.Position = sim.Registry.Positions[entityID];
			blob.Owner = GetTree().EditedSceneRoot;
			blobsByID.Add(entityID, blob);
		}
	}
		
		
		
}
