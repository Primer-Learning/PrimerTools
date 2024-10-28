using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Graph;

public class Histogram2DOptions
{
    public bool Normalize { get; set; }
    public float BinWidthX { get; set; } = 1;
    public float BinWidthY { get; set; } = 1;
}

public static class BarData3DUtilities
{
    private static float[,] MakeHistogram2D(IEnumerable<(float x, float y)> dataToBin, Histogram2DOptions options)
    {
        var toBin = dataToBin.ToArray();
        if (!toBin.Any()) return new float[0, 0];

        var maxX = toBin.Max(point => point.x);
        var maxY = toBin.Max(point => point.y);
        
        var numBinsX = Mathf.CeilToInt(maxX / options.BinWidthX);
        var numBinsY = Mathf.CeilToInt(maxY / options.BinWidthY);
        var histogram = new float[numBinsX, numBinsY];

        foreach (var (x, y) in toBin)
        {
            // Handle zero case
            if (x == 0 && y == 0)
            {
                histogram[0, 0]++;
                continue;
            }

            var binIndexX = Mathf.CeilToInt(x / options.BinWidthX) - 1;
            var binIndexY = Mathf.CeilToInt(y / options.BinWidthY) - 1;
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

            return options.Normalize ? NormalizeHistogram(histogram) : histogram;
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

            return options.Normalize ? NormalizeHistogram(histogram) : histogram;
        };
    }
}
