using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public class ReorderAndReadBytePerformance
{
    [Test, Performance]
    public unsafe void PerformReorderingAndSumTest()
    {
        var values = new NativeArray<uint>(200000, Allocator.Temp);
        var ptr = (byte*)values.GetUnsafePtr();

        Measure.Method(() =>
        {
            uint sum = 0;
            var uintPtr = (uint*)ptr;
            for (int i = 0; i < values.Length; i += 4, uintPtr++)
            {
                var val = BigEndian.ReadUInt32(ptr + i);
                sum += val;
                *uintPtr = val;
            }
        })
        .SampleGroup("With Variable")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            uint sum = 0;
            var uintPtr = (uint*)ptr;
            for (int i = 0; i < values.Length; i += 4, uintPtr++)
            {
                *uintPtr = BigEndian.ReadUInt32(ptr + i);
                sum += *uintPtr;
            }
        })
        .SampleGroup("Using Ptr")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            uint sum = 0;
            var ulongPtr = (ulong*)ptr;
            for (int i = 0; i < values.Length; i += 8, ulongPtr++)
            {
                *ulongPtr = BigEndian.Read2UInt32(ptr + i);
                sum += *(uint*)ulongPtr;
            }
        })
        .SampleGroup("Using Ptr Read 2 UInt")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        values.Dispose();
    }

    public unsafe static class BigEndian
    {
        public static ushort ReadUInt16(byte* data) => (ushort)(data[0] << 8 | data[1]);

        public static uint ReadUInt32(byte* data) =>
                (uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | data[3];

        public static ulong Read2UInt32(byte* data) =>
            (ulong)data[3] << 56 | (ulong)data[2] << 48 | (ulong)data[1] << 40 | (ulong)data[0] << 32 |
            (ulong)data[7] << 24 | (ulong)data[6] << 16 | (ulong)data[5] << 8 | data[4];

        public static ulong ReadUInt64(byte* data) =>
            (ulong)data[0] << 56 | (ulong)data[1] << 48 | (ulong)data[2] << 40 | (ulong)data[3] << 32 |
            (ulong)data[4] << 24 | (ulong)data[5] << 16 | (ulong)data[6] << 8 | data[7];
    }
}
