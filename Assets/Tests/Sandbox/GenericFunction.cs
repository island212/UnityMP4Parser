using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public class GenericFunction
{
    // A Test behaves as an ordinary method
    [Test, Performance]
    public void GenericFunctionTest()
    {
        NativeArray<int> values = new NativeArray<int>(100, Allocator.Temp);

        Measure.Method(() =>
        {
            bool final = false;
            for (int i = 1; i < values.Length; i++)
            {
                final = Compare<int>(values[i - 1], values[i]);
            }
        })
        .SampleGroup("Compare<T>")
        .WarmupCount(5)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            bool final = false;
            for (int i = 1; i < values.Length; i++)
            {
                final = Compare(values[i - 1], values[i]);
            }
        })
        .SampleGroup("Compare")
        .WarmupCount(5)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        values.Dispose();
    }

    public bool Compare<T>(T value1, T value2) where T : unmanaged
    {
        return value1.Equals(value2); 
    }

    public bool Compare(int value1, int value2)
    {
        return value1.Equals(value2);
    }
}
