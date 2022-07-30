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

        Measure.Method(() =>
        {
            int sum = 0;
            for (int i = 0; i < profileIDCs.Length; i++)
            {
                sum += BitFieldArrayHasChromaIndex[profileIDCs[i] >> 7].GetBits(profileIDCs[i] & 0x3F) >= 0 ? 1 : 0;
            }
        })
        .SampleGroup($"BitField64 Index")
        .WarmupCount(WarmupCount)
        .IterationsPerMeasurement(IterationsPerMeasurement)
        .MeasurementCount(MeasurementCount)
        .Run();

        Measure.Method(() =>
        {
            int sum = 0;
            for (int i = 0; i < profileIDCs.Length; i++)
            {
                sum += BitArrayHasChromaIndex.Get(profileIDCs[i]) ? 1 : 0;
            }
        })
        .SampleGroup($"BitArray")
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

    public static readonly BitField64[] BitFieldArrayHasChromaIndex = CreateBitFieldArrayHasChromaIndex();

    public static readonly BitArray BitArrayHasChromaIndex = CreateBitArrayHasChromaIndex();

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

    private static BitArray CreateBitArrayHasChromaIndex()
    {
        var array = new BitArray(256, false);
        array.Set(44, true);
        array.Set(83, true);
        array.Set(86, true);
        array.Set(100, true);
        array.Set(110, true);
        array.Set(118, true);
        array.Set(122, true);
        array.Set(128, true);
        array.Set(134, true);
        array.Set(135, true);
        array.Set(138, true);
        array.Set(139, true);
        array.Set(244, true);
        return array;
    }

    private static BitField64[] CreateBitFieldArrayHasChromaIndex()
    {
        var array = new BitField64[4];
        array[0].SetBits(44, true);
        array[1].SetBits(83 - 64, true);
        array[1].SetBits(86 - 64, true);
        array[1].SetBits(100 - 64, true);
        array[1].SetBits(110 - 64, true);
        array[1].SetBits(118 - 64, true);
        array[1].SetBits(122 - 64, true);
        array[2].SetBits(128 - 128, true);
        array[2].SetBits(134 - 128, true);
        array[2].SetBits(135 - 128, true);
        array[2].SetBits(138 - 128, true);
        array[3].SetBits(244 - 192, true);
        return array;
    }
}
