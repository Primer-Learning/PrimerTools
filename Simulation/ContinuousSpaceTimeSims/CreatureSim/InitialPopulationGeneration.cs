using System.Collections.Generic;
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
            var genome = new Genome();
            
            genome.AddTrait(new Trait<float>("MaxSpeed", 
                new List<float> { CreatureSimSettings.ReferenceCreatureSpeed },
                alleles => alleles[0], // Haploid expression
                trait => { /* Mutation logic */ }));

            genome.AddTrait(new Trait<float>("AwarenessRadius", 
                new List<float> { CreatureSimSettings.ReferenceAwarenessRadius },
                alleles => alleles[0],
                trait => { /* Mutation logic */ }));

            genome.AddTrait(new Trait<float>("MaxAge", 
                new List<float> { CreatureSimSettings.ReferenceMaxAge },
                alleles => alleles[0],
                trait => { /* Mutation logic */ }));

            creatures[i] = new DataCreature { Genome = genome };
        }
        
        return creatures;
    }
    
    // public static DataCreature[] FlatMaxAgeDistribution(int numCreatures, CreatureSimSettings settings)
    // {
	   //  var creatures = new DataCreature[numCreatures];
    //
	   //  for (var i = 0; i < numCreatures; i++)
	   //  {
		  //   creatures[i] = new DataCreature
		  //   {
			 //    AwarenessRadius = CreatureSimSettings.ReferenceAwarenessRadius,
			 //    MaxSpeed = CreatureSimSettings.ReferenceCreatureSpeed,
			 //    MaxAge = 2 * i
		  //   };
	   //  }
    //     
	   //  return creatures;
    // }
}
