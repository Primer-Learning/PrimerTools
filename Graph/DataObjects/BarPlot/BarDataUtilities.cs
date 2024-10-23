using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Graph;

public static class BarDataUtilities
{
    public static float[] MakeHistogram(IEnumerable<float> dataToBin, float binWidth)
    {
        var toBin = dataToBin as float[] ?? dataToBin.ToArray();
        if (!toBin.Any()) return Array.Empty<float>();

        var maxValue = toBin.Max();
        var numBins = Mathf.CeilToInt(maxValue / binWidth);
        var histogram = new float[numBins];

        foreach (var value in toBin)
        {
            if (value == 0)
            {
                histogram[0]++;
                continue;
            }

            var binIndex = Mathf.CeilToInt(value / binWidth) - 1;
            histogram[binIndex]++;
        }

        return histogram;
    }
    public static BarPlot.DataFetch PropertyHistogram<T>(Func<IEnumerable<T>> dataSourceGetter, Func<T, float> propertySelector, float binWidth = 1) 
    {
        return () => MakeHistogram(dataSourceGetter().Select(propertySelector).ToArray(), binWidth);
    }
    public static BarPlot.DataFetch NormalizedPropertyHistogram<T>(Func<IEnumerable<T>> dataSourceGetter, Func<T, float> propertySelector, float binWidth = 1) 
    {
        return () =>
        {
            var histogram = MakeHistogram(dataSourceGetter().Select(propertySelector).ToArray(), binWidth);
            var sum = histogram.Sum();
            return histogram.Select(x => x / sum).ToArray();
        };
    }
}