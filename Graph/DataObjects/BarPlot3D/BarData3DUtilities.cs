using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Graph;

public class Histogram2DOptions
{
    public enum AdjustmentMethodType
    {
        None,
        Normalize,
        PerCapita,
        PerTwoCapita // Convenient for diploid allele frequencies
    }

    public AdjustmentMethodType AdjustmentMethod = AdjustmentMethodType.None;
    public float BinWidthX = 1;
    public float? MinX = 0; // Null means let the data determine
    public float? MaxX = null;
    public float BinWidthY = 1;
    public float? MinY = 0;
    public float? MaxY = null;
}

public static class BarData3DUtilities
{
    public static float[,] MakeHistogram2D(IEnumerable<(float x, float y)> dataToBin, Histogram2DOptions options = null)
    {
        var toBin = dataToBin.ToArray();
        if (!toBin.Any()) return new float[0, 0];

        options ??= new Histogram2DOptions();

        var minX = options.MinX ?? toBin.Min(point => point.x);
        var minY = options.MinY ?? toBin.Min(point => point.y);
        var maxX = options.MaxX ?? toBin.Max(point => point.x);
        var maxY = options.MaxY ?? toBin.Max(point => point.y);
        
        var numBinsX = Mathf.CeilToInt((maxX - minX) / options.BinWidthX);
        var numBinsY = Mathf.CeilToInt((maxY - minY) / options.BinWidthY);
        var histogram = new float[numBinsX, numBinsY];

        foreach (var (x, y) in toBin)
        {
            // Bins include their max
            // This means the true min needs special handling to not go in bin -1
            // Could add a min-inclusive mode if needed.
            var binIndexX = x > minX ? Mathf.CeilToInt((x - minX) / options.BinWidthX) - 1 : 0;
            var binIndexY = y > minY ? Mathf.CeilToInt((y - minY) / options.BinWidthY) - 1 : 0;
            histogram[binIndexX, binIndexY]++;
        }

        return histogram;
    }

    private static float[,] NormalizeHistogram(float[,] histogram)
    {
        var sum = 0f;
        for (var x = 0; x < histogram.GetLength(0); x++)
        for (var y = 0; y < histogram.GetLength(1); y++)
            sum += histogram[x, y];

        if (sum == 0) return histogram;
            
        var normalized = new float[histogram.GetLength(0), histogram.GetLength(1)];
        for (var x = 0; x < histogram.GetLength(0); x++)
        for (var y = 0; y < histogram.GetLength(1); y++)
            normalized[x, y] = histogram[x, y] / sum;

        return normalized;
    }

    private static float[,] PerCapitaAdjustment(float[,] histogram, int total, bool two)
    {
        var adjusted = new float[histogram.GetLength(0), histogram.GetLength(1)];
        for (var x = 0; x < histogram.GetLength(0); x++)
        for (var y = 0; y < histogram.GetLength(1); y++)
            adjusted[x, y] = histogram[x, y] / (total * (two ? 2 : 1));

        return adjusted;
    }

    private static float[,] ApplyAdjustment<T>(float[,] histogram, Func<IEnumerable<T>> dataSourceGetter, Histogram2DOptions options)
    {
        switch (options.AdjustmentMethod)
        {
            case Histogram2DOptions.AdjustmentMethodType.Normalize:
                histogram = NormalizeHistogram(histogram);
                break;
            case Histogram2DOptions.AdjustmentMethodType.PerCapita:
                histogram = PerCapitaAdjustment(histogram, dataSourceGetter().Count(), false);
                break;
            case Histogram2DOptions.AdjustmentMethodType.PerTwoCapita:
                histogram = PerCapitaAdjustment(histogram, dataSourceGetter().Count(), true);
                break;
            case Histogram2DOptions.AdjustmentMethodType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return histogram;
    }

    public static BarPlot3D.DataFetch PropertyHistogram2D<T>(
        Func<IEnumerable<T>> dataSourceGetter,
        Func<T, (float x, float y)> propertySelector,
        Histogram2DOptions options = null)
    {
        options ??= new Histogram2DOptions();
        
        return () =>
        {
            var histogram = MakeHistogram2D(
                dataSourceGetter().Select(propertySelector),
                options);

            return ApplyAdjustment(histogram, dataSourceGetter, options);
        };
    }

    public static BarPlot3D.DataFetch PropertyHistogram2D<T>(
        Func<IEnumerable<T>> dataSourceGetter,
        Func<T, IEnumerable<(float x, float y)>> propertySelector,
        Histogram2DOptions options = null)
    {
        options ??= new Histogram2DOptions();
        
        return () =>
        {
            var histogram = MakeHistogram2D(
                dataSourceGetter().SelectMany(propertySelector),
                options);
            
            return ApplyAdjustment(histogram, dataSourceGetter, options);
        };
    }
}
