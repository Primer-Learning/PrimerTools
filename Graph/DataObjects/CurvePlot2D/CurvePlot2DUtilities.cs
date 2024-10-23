using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.Graph;

public static class CurvePlot2DUtilities
{
    // TODO: Could make these instance methods since they need a curve?
    public static CurvePlot2D.DataFetch AppendCount<T>(Func<IEnumerable<T>> dataSourceGetter, CurvePlot2D curve)
    {
        return () =>
        {
            var dataList = curve.GetData().ToList();
            var collection = dataSourceGetter().ToArray();
            dataList.Add(new Vector3(dataList.Count, collection.Length, 0));
            return dataList;
        };
    }

    public static CurvePlot2D.DataFetch AppendAverageProperty<T>(
        Func<IEnumerable<T>> dataSourceGetter, 
        CurvePlot2D curve,
        Func<T, float> propertySelector
    )
    {
        return () =>
        {
            var dataList = curve.GetData().ToList();
            dataList.Add(new Vector3(dataList.Count, dataSourceGetter().Average(propertySelector), 0));
            return dataList;
        };
    }
}