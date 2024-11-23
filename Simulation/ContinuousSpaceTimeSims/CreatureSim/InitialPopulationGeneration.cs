using System.Collections.Generic;
using Godot;
using RockPaperScissors;

namespace PrimerTools.Simulation;

public delegate DataCreature[] InitialPopulationGeneratorDelegate(int numCreatures, Rng rng);
public static class InitialPopulationGeneration
{
    public static DataCreature[] WorkingInitialPopulationThatChangesALot(int numCreatures, Rng rng = null)
    {
        var creatures = new DataCreature[numCreatures];

        for (var i = 0; i < numCreatures; i++)
        {
            var genome = new Genome();
            
            genome.AddTrait(
                new Trait<float>(
                    "MaxSpeed", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceCreatureSpeed, CreatureSimSettings.Instance.ReferenceCreatureSpeed },
                    ExpressionMechanisms.Float.AverageCodominant,
                    1
                )
            );

            genome.AddTrait(
                new Trait<float>(
                    "AwarenessRadius", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceAwarenessRadius, CreatureSimSettings.Instance.ReferenceAwarenessRadius},
                    ExpressionMechanisms.Float.AverageCodominant,
                    1
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

            creatures[i] = new DataCreature { Genome = genome };
        }
        
        return creatures;
    }
    
    public static DataCreature[] MaxAgeGenesWithMatureCreaturesFromTheBeginning(int numCreatures, Rng rng = null)
    {
        var creatures = new DataCreature[numCreatures];

        var maxAgeGenes = new List<float>();
        for (var i = 0; i < numCreatures * 2; i++)
        {
            // On every rotation of 20, the first 19 are 1-19, and the 20th is 10,000, for effectively infinite
            if (i % 20 != 19)
            {
                maxAgeGenes.Add(i % 19 + 1);
            }
            else
            {
                maxAgeGenes.Add(10000);
            }
        }
        maxAgeGenes.Shuffle(rng);

        for (var i = 0; i < numCreatures; i++)
        {
            var genome = new Genome();
            
            genome.AddTrait(
                new Trait<float>(
                    "MaxSpeed", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceCreatureSpeed, CreatureSimSettings.Instance.ReferenceCreatureSpeed },
                    ExpressionMechanisms.Float.AverageCodominant,
                    1
                )
            );

            genome.AddTrait(
                new Trait<float>(
                    "AwarenessRadius", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceAwarenessRadius, CreatureSimSettings.Instance.ReferenceAwarenessRadius},
                    ExpressionMechanisms.Float.AverageCodominant,
                    1
                )
            );

            genome.AddTrait(
                new Trait<float>(
                    "MaxAge",
                    new List<float> { maxAgeGenes[i] * 5, maxAgeGenes[i + 1] * 5 },
                    // ExpressionMechanisms.Float.RandomCodominant(rng), // Random codominance
                    ExpressionMechanisms.Float.AverageCodominant, // Random codominance
                    0
                )
            );

            creatures[i] = new DataCreature
            {
                Genome = genome,
                Age = CreatureSimSettings.Instance.MaturationTime
            };
        }
        
        return creatures;
    }
    public static DataCreature[] DeleteriousMutations(int numCreatures, Rng rng = null)
    {
        var creatures = new DataCreature[numCreatures];

        for (var i = 0; i < numCreatures; i++)
        {
            var genome = new Genome();
            
            genome.AddTrait(
                new Trait<float>(
                    "MaxSpeed", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceCreatureSpeed, CreatureSimSettings.Instance.ReferenceCreatureSpeed },
                    ExpressionMechanisms.Float.AverageCodominant,
                    1
                )
            );

            genome.AddTrait(
                new Trait<float>(
                    "AwarenessRadius", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceAwarenessRadius, CreatureSimSettings.Instance.ReferenceAwarenessRadius},
                    ExpressionMechanisms.Float.AverageCodominant,
                    1
                )
            );
            
            for (var j = 0; j < 20; j++)
            for (var k = 0; k < 20; k++)
            {
                genome.AddTrait(
                    DeleteriousTrait.CreateNew(
                        j,
                        k
                    )
                );
            }

            creatures[i] = new DataCreature { Genome = genome };
            // GD.Print($"Creature {i} has {creatures[i].Genome.Traits.Count} traits");
        }
        
        return creatures;
    }
    public static DataCreature[] DefaultSpeedAndAwarenessDiploid(int numCreatures, Rng rng = null)
    {
        var creatures = new DataCreature[numCreatures];

        for (var i = 0; i < numCreatures; i++)
        {
            var genome = new Genome();
            
            genome.AddTrait(
                new Trait<float>(
                    "MaxSpeed", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceCreatureSpeed, CreatureSimSettings.Instance.ReferenceCreatureSpeed },
                    ExpressionMechanisms.Float.AverageCodominant,
                    1
                )
            );

            genome.AddTrait(
                new Trait<float>(
                    "AwarenessRadius", 
                    new List<float> { CreatureSimSettings.Instance.ReferenceAwarenessRadius, CreatureSimSettings.Instance.ReferenceAwarenessRadius},
                    ExpressionMechanisms.Float.AverageCodominant,
                    1
                )
            );

            creatures[i] = new DataCreature { Genome = genome };
        }
        
        return creatures;
    }
}
