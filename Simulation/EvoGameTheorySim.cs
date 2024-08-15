using System;
using System.Collections.Generic;
using Godot;
using PrimerTools;
using System.Linq;
using PrimerTools.Graph;
using EntityID = System.Int32;
using ParentID = System.Int32;
using StrategyID = System.Int32;

[Tool]
public partial class EvoGameTheorySim : Node
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
			if (_run && !oldRun && Engine.IsEditorHint())
			{
				// Avoids running on build
				RunSim();
				PrintResults();
			}
		}
	}

	#endregion

	public void RunSim()
	{
		Initialize();
		Simulate();
	}

	#region Entity Registry

	public EntityRegistry Registry = new();

	public class EntityRegistry
	{
		private EntityID _nextId;
		public readonly List<StrategyID[]> Strategies = new();
		public readonly List<ParentID> Parents = new();

		public EntityID CreateBlob(StrategyID[] strategy, EntityID parent)
		{
			var id = _nextId++;
			Strategies.Add(strategy);
			Parents.Add(parent);
			return id;
		}
	}

	#endregion

	public class RPSGame
	{
		public RPSGame(float winMagnitude, float tieCost)
		{
			this.winMagnitude = winMagnitude;
			this.tieCost = tieCost;
		}

		public RPSGame()
		{
			GD.PushWarning("Creating default RPSGame.");
		}

		public enum Strategy
		{
			Rock,
			Paper,
			Scissors
		}

		public readonly Strategy[] StrategyOptions = Enum.GetValues<Strategy>();

		public (float reward1, float reward2) GetRewards(int strategy1, int strategy2)
		{
			return (
				RewardMatrix[strategy1, strategy2] + RewardModifiers[strategy1, strategy2] - GlobalCost,
				RewardMatrix[strategy2, strategy1] + RewardModifiers[strategy2, strategy1] - GlobalCost
			);
		}

		private const float GlobalCost = 0.0f;

		private readonly float winMagnitude = 1f;
		private readonly float tieCost = 0.0f;

		public float[,] RewardModifiers => new float[3, 3]
		{
			{ 0, 0, 0 },
			{ 0, 0, 0 },
			{ 0, 0, 0 }
		};

		private float[,] RewardMatrix => new float[3, 3]
		{
			{ 1 - tieCost, 1 - winMagnitude, 1 + 1 * winMagnitude }, // Rock rewards
			{ 1 + winMagnitude, 1 - tieCost, 1 - winMagnitude }, // Paper rewards   
			{ 1 - winMagnitude, 1 + winMagnitude, 1 - tieCost } // Scissors rewards
		};
	}

	#region Parameters

	private Rng _rng;
	[Export] public int Seed = -1;
	public int NumDays = 20;
	public int InitialBlobCount = 32;
	public int NumTrees = 50;
	public float MutationRate = 0;
	public bool InitializeByAlleleFrequency = true;
	public float[] InitialAlleleFrequencies;
	public Dictionary<int[], float> InitialMixedStrategyDistribution;
	public int NumAllelesPerBlob = 1;

	#endregion

	#region Simulation

	public List<EntityID>[] EntitiesByDay; // = new List<EntityID>[21];
	public List<EntityID>[] ShuffledParents;
	public RPSGame RpsGame;

	private void Initialize()
	{
		_rng = new Rng(Seed == -1 ? System.Environment.TickCount : Seed);
		RpsGame ??= new RPSGame();
		
		EntitiesByDay = new List<EntityID>[NumDays + 1];

		if (InitializeByAlleleFrequency)
		{
			// Check for valid initial frequencies
			if (InitialAlleleFrequencies is not { Length: 3 })
			{
				GD.PushWarning("No valid initial frequencies defined. Using even distribution.");
				InitialAlleleFrequencies = new[] { 1 / 3f, 1 / 3f, 1 / 3f };
			}

			// Normalize
			var sum = InitialAlleleFrequencies.Sum();
			if (Mathf.Abs(sum - 1) > 0.001f) GD.PushWarning("Initial allele frequencies don't add to 1. Normalizing.");
			InitialAlleleFrequencies = InitialAlleleFrequencies.Select(x => x / sum).ToArray();

			
			var initialAlleleCounts =
				InitialAlleleFrequencies.Select(
					x => Mathf.RoundToInt(x * InitialBlobCount * NumAllelesPerBlob)
				).ToArray();

			var blobIDs = new List<EntityID>();
			for (var i = 0; i < InitialBlobCount; i++)
			{
				// Figure out the strategy alleles
				var strategyAlleles = new int[NumAllelesPerBlob];
				for (var j = 0; j < NumAllelesPerBlob; j++)
				{
					// First, choose an allele type at random
					var stratIndex = _rng.RangeInt(3);
					// If there are alleles left to pass out, make sure we picked a type that remains
					if (initialAlleleCounts.Sum() > 0)
					{
						while (initialAlleleCounts[stratIndex] == 0)
						{
							stratIndex = _rng.RangeInt(3);
						}
					}

					// Assign the allele and decrement the number remaining of that allele
					strategyAlleles[j] = stratIndex;
					initialAlleleCounts[stratIndex] -= 1;
				}

				// Create the blob and assign the strategy alleles
				blobIDs.Add(Registry.CreateBlob(
					strategyAlleles,
					-1
				));
			}
			EntitiesByDay[0] = blobIDs;
		}
		else
		{
			// Check that initial strategy distribution is valid
			var sum = 0f;
			foreach (var pair in InitialMixedStrategyDistribution)
			{
				sum += pair.Value;

				if (pair.Key.Sum() != NumAllelesPerBlob)
				{
					GD.PrintErr($"{string.Join(", ", pair.Key)} is not a valid mixed strategy, since there should be {NumAllelesPerBlob} alleles per blob.");
				}
				if (pair.Key.Length != RpsGame.StrategyOptions.Length)
				{
					GD.PrintErr($"{string.Join(", ", pair.Key)} is not a valid mixed strategy. There should be {RpsGame.StrategyOptions.Length} strategies specified.");
				}
			}

			var normalizedInitialMixedStrategyDistribution = 
				InitialMixedStrategyDistribution.ToDictionary(pair => pair.Key, pair => pair.Value / sum);

			// Normalize it
			
			var blobIDs = new List<EntityID>();
			foreach (var pair in normalizedInitialMixedStrategyDistribution)
			{
				GD.Print($"{pair.Key[0]}, {pair.Key[1]}, {pair.Key[2]}");

				var listOfIndividualStrategies = new List<int>();
				var stratIndex = 0;
				foreach (var alleleCount in pair.Key)
				{
					listOfIndividualStrategies.AddRange(Enumerable.Repeat(stratIndex, alleleCount));
					stratIndex++;
				}
				
				for (var i = 0; i < Mathf.CeilToInt(pair.Value * InitialBlobCount); i++)
				{
					blobIDs.Add(
						Registry.CreateBlob(
							listOfIndividualStrategies.ToArray(),
							-1
						)
					);
				}
			}
			EntitiesByDay[0] = blobIDs;
		}
	}

	private void Simulate()
	{
		for (var i = 1; i <= NumDays; i++)
		{
			// Parents are already shuffled, since they were shuffled at the end of the last iteration of the loop
			var shuffledParents = EntitiesByDay[i - 1];

			var numGames = shuffledParents.Count - NumTrees;
			numGames = Mathf.Max(numGames, 0);
			numGames = Mathf.Min(numGames, NumTrees);

			var dailyChildren = new List<EntityID>();
			for (var j = 0; j < numGames * 2; j += 2)
			{
				var parent1Strategy = Registry.Strategies[shuffledParents[j]];
				var parent2Strategy = Registry.Strategies[shuffledParents[j + 1]];

				var (reward1, reward2) =
					RpsGame.GetRewards(parent1Strategy.RandomItem(_rng), parent2Strategy.RandomItem(_rng));

				for (var k = 0; k < GetOffspringCount(reward1); k++)
				{
					dailyChildren.Add(Reproduce(shuffledParents[j], parent1Strategy));
				}

				for (var k = 0; k < GetOffspringCount(reward2); k++)
				{
					dailyChildren.Add(Reproduce(shuffledParents[j + 1], parent2Strategy));
				}
			}

			for (var j = numGames * 2; j < shuffledParents.Count; j += 1)
			{
				if (numGames < NumTrees)
				{
					var parentStrategy = Registry.Strategies[shuffledParents[j]];
					dailyChildren.Add(Reproduce(shuffledParents[j], parentStrategy));
					dailyChildren.Add(Reproduce(shuffledParents[j], parentStrategy));
				}
				// Else they die, which is just not reproducing, so do nothing
			}

			// Shuffle at the end of the loop so entities are sorted by tree for the next day
			EntitiesByDay[i] = dailyChildren.ShuffleToList(rng: _rng);
		}
	}

	private int Reproduce(int parentIndex, int[] strategies)
	{
		// TODO: Make mutations work for an arbitrary number of possible strategies
		// instead of just 3

		// Make a copy
		var childStrategies = strategies.ToArray();
		
		// Mutations
		for (var i = 0; i < childStrategies.Length; i++)
		{
			var roll = _rng.RangeFloat(1);
			if (roll < MutationRate / 2)
			{
				childStrategies[i] = (childStrategies[i] + 1) % RpsGame.StrategyOptions.Length;
			}
			else if (roll < MutationRate)
			{
				childStrategies[i] = (childStrategies[i] + 2) % RpsGame.StrategyOptions.Length;
			}
		}

		return Registry.CreateBlob(
			childStrategies,
			parentIndex
		);
	}

	private int GetOffspringCount(float reward)
	{
		var offspringCount = 0;
		var wholeReward = Mathf.FloorToInt(reward);
		var fractionReward = reward - wholeReward;

		for (var j = 0; j < wholeReward; j++)
		{
			offspringCount++;
		}

		if (_rng.RangeFloat(1) < fractionReward)
		{
			offspringCount++;
		}

		return offspringCount;
	}

	#endregion

	private void PrintResults()
	{
		foreach (var (day, frequencies) in GetStrategyFrequenciesByDay().WithIndexBackwardDeprecated())
		{
			GD.Print($"Day {day}");
			GD.Print($"Rock: {frequencies[0]:P1}, Paper: {frequencies[1]:P1}, Scissors: {frequencies[2]:P1}");
		}
	}

	public Vector3[] GetStrategyFrequenciesByDay()
	{
		var frequencies = new Vector3[NumDays + 1];
		foreach (var (day, entitiesToday) in EntitiesByDay.WithIndexBackwardDeprecated())
		{
			var numAllelesToday = entitiesToday.Count * NumAllelesPerBlob; 
			
			var fractionRock = (float) entitiesToday.Sum(s => Registry.Strategies[s].Count(x => x == 0)) / numAllelesToday;
			var fractionPaper = (float) entitiesToday.Sum(s => Registry.Strategies[s].Count(x => x == 1)) / numAllelesToday;
			var fractionScissors = (float) entitiesToday.Sum(s => Registry.Strategies[s].Count(x => x == 2)) / numAllelesToday;
			frequencies[day] = new Vector3(fractionRock, fractionPaper, fractionScissors);
		}

		return frequencies;
	}

	public List<List<float>> GetMixedStrategyFreqenciesByDay()
	{
		var frequenciesByDay = new List<List<float>>();
		foreach (var (day, entitiesToday) in EntitiesByDay.WithIndexBackwardDeprecated())
		{
			var frequenciesToday = new List<float>();
			// i and j are the number of increments toward paper and scissors, respectively
			for (var i = 0; i <= NumAllelesPerBlob + 1; i++)
			{
				for (var j = 0; j < NumAllelesPerBlob + 1; j++)
				{
					if (j + i >= NumAllelesPerBlob + 1) continue;
					
					frequenciesToday.Add((float)entitiesToday.Count(
						                     x => 
						                     Registry.Strategies[x].Count(y => y == 1) == i 
						                     && Registry.Strategies[x].Count(y => y == 2) == j) 
					                     / entitiesToday.Count);
				}
			}
			frequenciesByDay.Add(frequenciesToday);
		}
		return frequenciesByDay;
	}
	
}
