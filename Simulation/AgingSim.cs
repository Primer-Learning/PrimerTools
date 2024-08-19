using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimerTools;
using EntityID = System.Int32;
using ParentID = System.Int32;

[Tool]
public partial class AgingSim : Node
{
	#region Running toggle

	private bool _run = true;

	[Export]
	private bool Run
	{
		get => _run;
		set
		{
			var oldRun = _run;
			_run = value;
			// Avoids running on build
			if (_run && !oldRun && Engine.IsEditorHint())
			{
				RunSim();
				PrintResults();
			}
		}
	}

	#endregion
	
	public void RunSim()
	{
		Initialize();
		// Simulate();
	}

	#region Entity Registry

	public EntityRegistry Registry = new();

	public class EntityRegistry
	{
		private EntityID _nextId;
		public readonly List<Vector3> Positions = new();
		public readonly List<ParentID> Parents = new();

		public EntityID CreateBlob(Vector3 position, EntityID parent)
		{
			var id = _nextId++;
			Positions.Add(position);
			Parents.Add(parent);
			return id;
		}
	}

	#endregion

	#region Parameters

	private Rng _rng;
	[Export] public int Seed = -1;
	// public int NumDays = 20;
	public int InitialBlobCount = 32;
	// public int NumTrees = 50;
	// public float MutationRate = 0;

	// private int numAlleles = 3; // Will probably be defined by a different structure for the genetic model
	// public bool InitializeByAlleleFrequency = true;
	// public float[] InitialAlleleFrequencies;
	// public Dictionary<int[], float> InitialMixedStrategyDistribution;
	// public int NumAllelesPerBlob = 1;

	#endregion

	#region Simulation

	public List<EntityID> CurrentEntities;
	public void Initialize()
	{
		_rng = new Rng(Seed == -1 ? System.Environment.TickCount : Seed);
	
		// // Check for valid initial frequencies
		//
		// if (InitialAlleleFrequencies.Length != numAlleles)
		// {
		// 	GD.PushWarning("No valid initial frequencies defined. Using even distribution.");
		// 	InitialAlleleFrequencies = new[] { 1f / numAlleles, 1f / numAlleles, 1f / numAlleles };
		// }
		//
		// // Normalize
		// var sum = InitialAlleleFrequencies.Sum();
		// if (Mathf.Abs(sum - 1) > 0.001f) GD.PushWarning("Initial allele frequencies don't add to 1. Normalizing.");
		// InitialAlleleFrequencies = InitialAlleleFrequencies.Select(x => x / sum).ToArray();
		//
		// var initialAlleleCounts =
		// 	InitialAlleleFrequencies.Select(
		// 		x => Mathf.RoundToInt(x * InitialBlobCount * NumAllelesPerBlob)
		// 	).ToArray();

		var blobIDs = new List<EntityID>();
		for (var i = 0; i < InitialBlobCount; i++)
		{
			// // Figure out the strategy alleles
			// var strategyAlleles = new int[NumAllelesPerBlob];
			// for (var j = 0; j < NumAllelesPerBlob; j++)
			// {
			// 	// First, choose an allele type at random
			// 	var stratIndex = _rng.RangeInt(numAlleles);
			// 	// If there are alleles left to pass out, make sure we picked a type that remains
			// 	if (initialAlleleCounts.Sum() > 0)
			// 	{
			// 		while (initialAlleleCounts[stratIndex] == 0)
			// 		{
			// 			stratIndex = _rng.RangeInt(numAlleles);
			// 		}
			// 	}
			//
			// 	// Assign the allele and decrement the number remaining of that allele
			// 	strategyAlleles[j] = stratIndex;
			// 	initialAlleleCounts[stratIndex] -= 1;
			// }
			
			// Determine position. Just random for now.
			var position = new Vector3(
				_rng.RangeFloat(-10, 10),
				0,
				_rng.RangeFloat(-10, 10)
			);

			// Create the blob and assign the strategy alleles
			blobIDs.Add(Registry.CreateBlob(
				position,
				-1
			));
		}
		CurrentEntities = blobIDs;
	}

	private void Simulate()
	{
		// for (var i = 1; i <= NumDays; i++)
		// {
		// 	
		// }
	}

	private int Reproduce(int parentIndex, int[] genes)
	{
		// // TODO: Make mutations work for an arbitrary number of possible strategies
		// // instead of just 3
		//
		// // Make a copy
		// var childGenes = genes.ToArray();
		//
		// // Mutations
		// for (var i = 0; i < childGenes.Length; i++)
		// {
		// 	var roll = _rng.RangeFloat(1);
		// 	
		// 	// Handle mutations here based on the model
		// }
		//
		// // May also include a location, since the 
		// return Registry.CreateBlob(
		// 	EntityRegistry.Positions(parentIndex),
		// 	parentIndex
		// );
		
		GD.PrintErr("Reproduction not implemented in aging sim");
		return 0;
	}

	#endregion

	private void PrintResults()
	{
		
	}

	public Vector3[] GetGeneFrequenciesByDay()
	{
		GD.PrintErr("GetGeneFrequenciesByDay is not implemented.");
		return Array.Empty<Vector3>();
	}
}
