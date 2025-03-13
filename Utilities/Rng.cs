using Godot;
using System;

namespace PrimerTools
{
    public class Rng
    {
        public static Random staticRandom;
        private static int _staticCallCount;
        public static int StaticCallCount => _staticCallCount;
        public static void Initialize(int? seed = null)
        {
            if (seed is null)
            {
                // If seed is null, we don't care what the seed is.
                // So if the staticRandom object already exists, we don't need to do anything.
                // But if it doesn't, create it and set the seed to the current time.
                if (staticRandom is not null) return;
                staticRandom = new Random(System.Environment.TickCount);
            }
            else
            {
                // If we did give a seed, we should remake the staticRandom object with that seed.
                staticRandom = new Random(seed.Value);
            }
        }
        
        public static int RangeInt(int maxExclusive) => RangeInt(0, maxExclusive);

        public static int RangeInt(int minInclusive, int maxExclusive)
        {
            Initialize();
            _staticCallCount++;
            return staticRandom.Next(minInclusive, maxExclusive);
        }

        public static int NextInt()
        {
            Initialize();
            _staticCallCount++;
            return staticRandom.Next();
        }

        public static float RangeFloat(float maxExclusive) => RangeFloat(0, maxExclusive);

        public static float RangeFloat(float minInclusive, float maxExclusive)
        {
            Initialize();
            _staticCallCount++;
            return (float) staticRandom.NextDouble() * (maxExclusive - minInclusive) + minInclusive;
        }

        // instance
        public Random rand { get; }
        public int CallCount;

        public Rng(Random rand)
        {
            this.rand = rand;
            CallCount = 0;
        }

        public Rng(int seed) : this(new Random(seed))
        {
        }
    }

    // Methods are declared as extension methods so calling on null rngs falls back on static rng with a warning.
    public static class RngExtensions
    {
        private static bool hasWarned = false;
        public static int RangeInt(this Rng rng, int maxExclusive) => RangeInt(rng, 0, maxExclusive);

        public static int RangeInt(this Rng rng, int minInclusive, int maxExclusive)
        {
            var rand = rng?.rand;
            
            if (rand != null)
            {
                rng.CallCount++;
                return rand.Next(minInclusive, maxExclusive);
            }
            if (!hasWarned)
            {
                PrimerGD.PrintWithStackTrace("No Rng given, using static rng. If you did this on purpose, use Rng.RangeInt() directly. Otherwise, check that you're passing an the Rng object you want.");
                hasWarned = true;
            }
            return Rng.RangeInt(minInclusive, maxExclusive);
        }

        public static float RangeFloat(this Rng rng, float maxExclusive) => RangeFloat(rng, 0, maxExclusive);

        public static float RangeFloat(this Rng rng, float minInclusive, float maxExclusive)
        {
            var rand = rng?.rand;

            if (rand != null)
            {
                rng.CallCount++;
                return (float)(rand.NextDouble() * (maxExclusive - minInclusive) + minInclusive);
            }
            if (!hasWarned)
            {
                PrimerGD.PrintWithStackTrace("No Rng given, using static rng. If you did this on purpose, use Rng.RangeInt() directly. Otherwise, check that you're passing an the Rng object you want.");
                hasWarned = true;
            }
            return Rng.RangeFloat(minInclusive, maxExclusive);
        }
    }
}
