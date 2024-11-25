using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Graph;

public class HistogramOptions
{
    public enum AdjustmentMethodType
    {
        None,
        Normalize,
        PerCapita,
        PerTwoCapita // Convenient for diploid allele frequencies
    }

    public AdjustmentMethodType AdjustmentMethod = AdjustmentMethodType.None;
    public float BinWidth = 1;
    public float? Min = 0; // Null means let the data determine
    public float? Max = null;
}

public static class BarDataUtilities
{
    private static float[] MakeHistogramInternal(IEnumerable<float> dataToBin, HistogramOptions options)
    {
        var toBin = dataToBin as float[] ?? dataToBin.ToArray();
        if (!toBin.Any()) return Array.Empty<float>();
        
        var min = options.Min ?? 0;
        var max = options.Max ?? toBin.Max();
        var numBins = Mathf.Max(1, Mathf.CeilToInt((max - min) / options.BinWidth));
        var histogram = new float[numBins];

        foreach (var value in toBin)
        {
            // Bins include their max
            // This means the true min needs special handling to not go in bin -1
            var binIndex = value > min ? Mathf.CeilToInt((value - min) / options.BinWidth) - 1 : 0;
            if (binIndex < numBins) // Ignore values beyond max
                histogram[binIndex]++;
        }

        return histogram;
    }

    public static float[] MakeHistogram(IEnumerable<float> dataToBin, HistogramOptions options = null)
    {
        options ??= new HistogramOptions();
        var data = dataToBin as float[] ?? dataToBin.ToArray();
        var histogram = MakeHistogramInternal(data, options);
        return ApplyAdjustment(histogram, () => data, options);
    }

    private static float[] NormalizeHistogram(float[] histogram)
    {
        var sum = histogram.Sum();
        if (sum == 0) return histogram;

        var normalized = new float[histogram.Length];
        for (var i = 0; i < histogram.Length; i++)
            normalized[i] = histogram[i] / sum;

        return normalized;
    }

    private static float[] PerCapitaAdjustment(float[] histogram, int total, bool two)
    {
        var adjusted = new float[histogram.Length];
        for (var i = 0; i < histogram.Length; i++)
            adjusted[i] = histogram[i] / (total * (two ? 2 : 1));

        return adjusted;
    }

    private static float[] ApplyAdjustment<T>(float[] histogram, Func<IEnumerable<T>> dataSourceGetter, HistogramOptions options)
    {
        switch (options.AdjustmentMethod)
        {
            case HistogramOptions.AdjustmentMethodType.Normalize:
                histogram = NormalizeHistogram(histogram);
                break;
            case HistogramOptions.AdjustmentMethodType.PerCapita:
                histogram = PerCapitaAdjustment(histogram, dataSourceGetter().Count(), false);
                break;
            case HistogramOptions.AdjustmentMethodType.PerTwoCapita:
                histogram = PerCapitaAdjustment(histogram, dataSourceGetter().Count(), true);
                break;
            case HistogramOptions.AdjustmentMethodType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return histogram;
    }

    public static BarPlot.DataFetch PropertyHistogram<T>(
        Func<IEnumerable<T>> dataSourceGetter,
        Func<T, float> propertySelector,
        HistogramOptions options = null)
    {
        options ??= new HistogramOptions();

        return () =>
        {
            var histogram = MakeHistogramInternal(
                dataSourceGetter().Select(propertySelector),
                options);

            return ApplyAdjustment(histogram, dataSourceGetter, options);
        };
    }

    public static BarPlot.DataFetch PropertyHistogram<T>(
        Func<IEnumerable<T>> dataSourceGetter,
        Func<T, IEnumerable<float>> propertySelector,
        HistogramOptions options = null)
    {
        options ??= new HistogramOptions();

        return () =>
        {
            var histogram = MakeHistogramInternal(
                dataSourceGetter().SelectMany(propertySelector),
                options);

            return ApplyAdjustment(histogram, dataSourceGetter, options);
        };
    }
}
