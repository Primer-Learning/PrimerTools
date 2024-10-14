using Godot;

namespace PrimerTools.Simulation;

public delegate DataCreature[] InitialPopulationGeneratorDelegate(int numCreatures, CreatureSimSettings settings);
public static class InitialPopulationGeneration
{
    public static DataCreature[] AllDefaultsInitialPopulation(int numCreatures, CreatureSimSettings settings)
    {
        var creatures = new DataCreature[numCreatures];

        for (var i = 0; i < numCreatures; i++)
        {
			 creatures[i] = new DataCreature
			{
				AwarenessRadius = CreatureSimSettings.ReferenceAwarenessRadius,
				MaxSpeed = CreatureSimSettings.ReferenceCreatureSpeed,
				MaxAge = CreatureSimSettings.ReferenceMaxAge
			};
        }
        
        return creatures;
    }
    
    public static DataCreature[] FlatMaxAgeDistribution(int numCreatures, CreatureSimSettings settings)
    {
	    var creatures = new DataCreature[numCreatures];

	    for (var i = 0; i < numCreatures; i++)
	    {
		    creatures[i] = new DataCreature
		    {
			    AwarenessRadius = CreatureSimSettings.ReferenceAwarenessRadius,
			    MaxSpeed = CreatureSimSettings.ReferenceCreatureSpeed,
			    MaxAge = 2 * i
		    };
	    }
        
	    return creatures;
    }
}