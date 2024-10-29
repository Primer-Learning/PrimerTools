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

            // genome.AddTrait(
            //     new Trait<float>(
            //         "MaxAge", 
            //         new List<float> { i % 40, i % 40 },
            //         ExpressionMechanisms.Float.Codominant,
            //         0
            //     )
            // );
            
            // genome.AddTrait(
            //     new Trait<float>(
            //         "MaxReproductionAge", 
            //         new List<float> { 20, 20 },
            //         ExpressionMechanisms.Float.Codominant,
            //         0
            //     )
            // );

            genome.AddTrait(
                new Trait<bool>(
                    "Antagonistic Pleiotropy Speed", 
                    new List<bool> { true, false },
                    ExpressionMechanisms.Bool.Codominant,
                    mutationIncrement: false
                )
            );

            creatures[i] = new DataCreature { Genome = genome };
        }
        
        return creatures;
    }
}
