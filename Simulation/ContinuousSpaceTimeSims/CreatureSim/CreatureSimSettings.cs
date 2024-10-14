using System;
using Godot;

namespace PrimerTools.Simulation;

public static class CreatureSimSettings
{
	#region Sim parameters
	// Movement
	public const float CreatureStepMaxLength = 10f;
	public const float MaxAccelerationFactor = 0.1f;
	public const float CreatureEatDistance = 2;
	public const float CreatureMateDistance = 2;
	
	// State-based pause durations
	public const float EatDuration = 1.5f;
	public const float MaturationTime = 2f;
	
	// Energy
	public const float BaseEnergySpend = 0.1f;
	public const float GlobalEnergySpendAdjustmentFactor = 0.2f;
	public const float EnergyGainFromFood = 1f;
	public const float ReproductionEnergyThreshold = 2f;
	public const float ReproductionEnergyCost = 1f;
	public const float DefaultHungerThreshold = 4;
	public const float ReproductionDuration = 1;
	
	// Initial population
	public const float InitialCreatureSpeed = 5f;
	public const float InitialAwarenessRadius = 5f;
	public const float InitialMaxAge = 20;
	
	// Mutation
	public const float MutationProbability = 0.1f;
	public const float MutationIncrement = 1f;
	#endregion
}

