using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Graph;

public static class BarData3DUtilities
{
    public static float[,] MakeHistogram2D(IEnumerable<(float x, float y)> dataToBin, float binWidthX, float binWidthY)
    {
        var toBin = dataToBin.ToArray();
        if (!toBin.Any()) return new float[0, 0];

        var maxX = toBin.Max(point => point.x);
        var maxY = toBin.Max(point => point.y);
        
        var numBinsX = Mathf.CeilToInt(maxX / binWidthX);
        var numBinsY = Mathf.CeilToInt(maxY / binWidthY);
        var histogram = new float[numBinsX, numBinsY];

        foreach (var (x, y) in toBin)
        {
            // Handle zero case
            if (x == 0 && y == 0)
            {
                histogram[0, 0]++;
                continue;
            }

            var binIndexX = Mathf.CeilToInt(x / binWidthX) - 1;
            var binIndexY = Mathf.CeilToInt(y / binWidthY) - 1;
            histogram[binIndexX, binIndexY]++;
        }

        return histogram;
    }

    public static BarPlot3D.DataFetch PropertyHistogram2D<T>(
        Func<IEnumerable<T>> dataSourceGetter, 
        Func<T, (float x, float y)> propertySelector,
        float binWidthX = 1,
        float binWidthY = 1)
    {
        return () => MakeHistogram2D(
            dataSourceGetter().Select(propertySelector),
            binWidthX,
            binWidthY);
    }

    public static BarPlot3D.DataFetch NormalizedPropertyHistogram2D<T>(
        Func<IEnumerable<T>> dataSourceGetter,
        Func<T, (float x, float y)> propertySelector,
        float binWidthX = 1,
        float binWidthY = 1)
    {
        return () =>
        {
            var histogram = MakeHistogram2D(
                dataSourceGetter().Select(propertySelector),
                binWidthX,
                binWidthY);
            
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
        };
    }

    public static BarPlot3D.DataFetch PropertyHistogram2D<T>(
        Func<IEnumerable<T>> dataSourceGetter,
        Func<T, IEnumerable<(float x, float y)>> propertySelector,
        float binWidthX = 1,
        float binWidthY = 1)
    {
        return () => MakeHistogram2D(
            dataSourceGetter().SelectMany(propertySelector),
            binWidthX,
            binWidthY
        );
    }

    public static BarPlot3D.DataFetch NormalizedPropertyHistogram2DMulti<T>(
        Func<IEnumerable<T>> dataSourceGetter,
        Func<T, IEnumerable<(float x, float y)>> propertySelector,
        float binWidthX = 1,
        float binWidthY = 1)
    {
        return () =>
        {
            var histogram = MakeHistogram2D(
                dataSourceGetter().SelectMany(propertySelector),
                binWidthX,
                binWidthY);
            
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
        };
    }
}
