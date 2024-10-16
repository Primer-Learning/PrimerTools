using System.Collections.Generic;

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
            
            genome.AddTrait(
                new Trait<float>(
                    "MaxSpeed", 
                    new List<float> { CreatureSimSettings.ReferenceCreatureSpeed, CreatureSimSettings.ReferenceCreatureSpeed },
                    ExpressionMechanisms.Float.Codominant,
                    1
                )
            );

            genome.AddTrait(
                new Trait<float>(
                    "AwarenessRadius", 
                    new List<float> { CreatureSimSettings.ReferenceAwarenessRadius, CreatureSimSettings.ReferenceAwarenessRadius},
                    ExpressionMechanisms.Float.Codominant,
                    1
                )
            );

            genome.AddTrait(
                new Trait<float>(
                    "MaxAge", 
                    new List<float> { float.MaxValue, float.MaxValue },
                    ExpressionMechanisms.Float.Codominant,
                    1
                )
            );

            creatures[i] = new DataCreature { Genome = genome };
        }
        
        return creatures;
    }
}
