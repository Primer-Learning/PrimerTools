using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools.Graph;

namespace PrimerTools.Simulation;

public static class SimulationCurveDataFetchMethods
{
    public static CurvePlot2D.DataFetch AverageAge(IEnumerable<DataCreature> entities) 
    {
        return () =>
        {
            var dataList = new List<Vector3>();
            dataList.Add(new Vector3(dataList.Count, entities.Average(x => x.Age), 0));
            return dataList;
        };
    }

    public static CurvePlot2D.DataFetch CreatureCount(IEnumerable<DataCreature> entities)
    {
        return () =>
        {
            var dataList = new List<Vector3>();
            dataList.Add(new Vector3(dataList.Count, entities.Count(), 0));
            return dataList;
        };
    }

    public static CurvePlot2D.DataFetch TrackProperty(
        IEnumerable<DataCreature> entities, 
        Func<DataCreature, float> propertySelector)
    {
        return () =>
        {
            var dataList = new List<Vector3>();
            dataList.Add(new Vector3(dataList.Count, entities.Average(propertySelector), 0));
            return dataList;
        };
    }
}

public static class SimulationBarDataFetchMethods
{
    public static BarPlot.DataFetch MaxAgeHistogram(IEnumerable<DataCreature> entities) 
    {
        return () =>
        {
            var values = entities.Select(x => x.MaxAge).ToList();
            var binWidth = 1;

            if (values.Count == 0) return new List<float>();

            var maxValue = values.Max();
            var numBins = Mathf.CeilToInt(maxValue / binWidth);
            var histogram = new List<float>(new float[numBins]);

            foreach (var value in values)
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
        };
    }
}