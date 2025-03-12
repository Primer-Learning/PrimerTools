using System.Collections.Generic;
using Godot;
using RockPaperScissors;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public delegate CreatureComponent[] InitialPopulationGeneratorDelegate(int numCreatures, Rng rng);
public static class InitialPopulationGeneration
{
    public static CreatureComponent[] WorkingInitialPopulationThatChangesALot(int numCreatures, Rng rng = null)
    {
        var creatures = new CreatureComponent[numCreatures];

        for (var i = 0; i < numCreatures; i++)
        {
            var genome = new Genome();
            
            genome.AddTrait(
                new Trait<float>(
                    "MaxSpeed", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceCreatureSpeed, CreatureSimSettings.Instance.ReferenceCreatureSpeed },
                    ExpressionMechanisms.Float.AverageCodominant,
                    0
                )
            );

            genome.AddTrait(
                new Trait<float>(
                    "AwarenessRadius", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceAwarenessRadius, CreatureSimSettings.Instance.ReferenceAwarenessRadius},
                    ExpressionMechanisms.Float.AverageCodominant,
                    0
                )
            );

            // var maxAgeOptions = new float[] {20f, 40f, float.MaxValue};
            // genome.AddTrait(
            //     new Trait<float>(
            //         "MaxAge", 
            //         new List<float> { maxAgeOptions.RandomItem(rng) },
            //         ExpressionMechanisms.Float.AverageCodominant,
            //         0
            //     )
            // );
            
            // genome.AddTrait(
            //     new Trait<float>(
            //         "MaxReproductionAge", 
            //         new List<float> { 40, 40 },
            //         ExpressionMechanisms.Float.Codominant,
            //         0
            //     )
            // );

            // genome.AddTrait(
            //     new Trait<bool>(
            //         "Antagonistic Pleiotropy Speed", 
            //         new List<bool> { true, false },
            //         alleles => alleles.RandomItem(rng),
            //         mutationIncrement: false
            //     )
            // );

            creatures[i] = new CreatureComponent() { Genome = genome };
        }
        
        return creatures;
    }
    public static CreatureComponent[] DefaultSpeedAndAwarenessDiploid(int numCreatures, Rng rng = null)
    {
        var creatures = new CreatureComponent[numCreatures];

        for (var i = 0; i < numCreatures; i++)
        {
            var genome = new Genome();
            
            genome.AddTrait(
                new Trait<float>(
                    "MaxSpeed", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceCreatureSpeed, CreatureSimSettings.Instance.ReferenceCreatureSpeed },
                    ExpressionMechanisms.Float.AverageCodominant,
                    0
                )
            );

            genome.AddTrait(
                new Trait<float>(
                    "AwarenessRadius", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceAwarenessRadius, CreatureSimSettings.Instance.ReferenceAwarenessRadius},
                    ExpressionMechanisms.Float.AverageCodominant,
                    0
                )
            );

            creatures[i] = new CreatureComponent { Genome = genome };
        }
        
        return creatures;
    }
}
