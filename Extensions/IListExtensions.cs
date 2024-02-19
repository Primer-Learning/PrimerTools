using System.Collections.Generic;
using PrimerTools;

namespace RockPaperScissors;

public static class IListExtensions
{
    public static void Shuffle<T>(this IList<T> list, Rng rng)  
    {
        // Same as IEnumerableExtensions.ShuffleToList, but in place
        var n = list.Count;
        while (n > 1) {  
            n--;
            var k = rng.RangeInt(0, n + 1);  
            (list[k], list[n]) = (list[n], list[k]);
        }  
    }
}