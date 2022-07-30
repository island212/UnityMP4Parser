using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public class SwitchVsArrayVsContains
{
    const int WarmupCount = 10;
    const int IterationsPerMeasurement = 1;
    const int MeasurementCount = 50;

    // A Test behaves as an ordinary method
    [Test, Performance]
    public void SwitchVsArrayVsContainsSimplePasses()
    {
        var profileIDCs = new NativeArray<byte>(100000, Allocator.TempJob);

        var random = new Unity.Mathematics.Random(1234);
        for (int i = 0; i < profileIDCs.Length; i++)
            profileIDCs[i] = (byte)random.NextUInt(255);

        Measure.Method(() =>
        {
            int sum = 0;
            for (int i = 0; i < profileIDCs.Length; i++)
            {
                sum += HasChroma(profileIDCs[i]) ? 1 : 0;
            }
        })
        .SampleGroup($"Switch")
        .WarmupCount(WarmupCount)
        .IterationsPerMeasurement(IterationsPerMeasurement)
        .MeasurementCount(MeasurementCount)
        .Run();

        Measure.Method(() =>
        {
            int sum = 0;
            for (int i = 0; i < profileIDCs.Length; i++)
            {
                sum += ArrayHasChromaIndex[profileIDCs[i]] ? 1 : 0;
            }
        })
        .SampleGroup($"Array Index")
        .WarmupCount(WarmupCount)
        .IterationsPerMeasurement(IterationsPerMeasurement)
        .MeasurementCount(MeasurementCount)
        .Run();

        Measure.Method(() =>
        {
            int sum = 0;
            for (int i = 0; i < profileIDCs.Length; i++)
            {            
                sum += ArrayHasChromaValue.Contains(profileIDCs[i]) ? 1 : 0;
            }
        })
        .SampleGroup($"Array Contains")
        .WarmupCount(WarmupCount)
        .IterationsPerMeasurement(IterationsPerMeasurement)
        .MeasurementCount(MeasurementCount)
        .Run();

        Measure.Method(() =>
        {
            int sum = 0;
            for (int i = 0; i < profileIDCs.Length; i++)
            {
                sum += ArrayHasChromaValue.BinarySearch(profileIDCs[i]) >= 0 ? 1 : 0;
            }
        })
        .SampleGroup($"Array BinarySearch")
        .WarmupCount(WarmupCount)
        .IterationsPerMeasurement(IterationsPerMeasurement)
        .MeasurementCount(MeasurementCount)
        .Run();

        profileIDCs.Dispose();
    }

    public static bool HasChroma(byte profile_idc) => profile_idc switch
    {
        44 or 83 or 86 or 100 or 110 or 118 or 122 or 128 or 134 or 135 or 138 or 139 or 244 => true,
        _ => false
    };

    public static readonly bool[] ArrayHasChromaIndex = CreateArrayHasChromaIndex();

    public static readonly NativeArray<byte> ArrayHasChromaValue 
        = new NativeArray<byte>(new byte[]{ 44, 83, 86, 100, 110, 118, 122, 128, 134, 135, 138, 139, 244 }, Allocator.Persistent);

    private static bool[] CreateArrayHasChromaIndex()
    {
        var array = new bool[256];
        array[44] = true;
        array[83] = true;
        array[86] = true;
        array[100] = true;
        array[110] = true;
        array[118] = true;
        array[122] = true;
        array[128] = true;
        array[134] = true;
        array[135] = true;
        array[138] = true;
        array[139] = true;
        array[244] = true;
        return array;
    }
}
