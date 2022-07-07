using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public class ReorderBytePerformance
{
    // A Test behaves as an ordinary method
    [Test, Performance]
    public unsafe void PerformReorderingTest()
    {
        var values = new NativeArray<uint>(200000, Allocator.Temp);
        var ptr = (byte*)values.GetUnsafePtr();

        Measure.Method(() =>
        {
            for (int i = 0; i < values.Length; i += 4)
            {
                byte temp = ptr[i];
                ptr[i] = ptr[i + 3];
                ptr[i + 3] = temp;

                temp = ptr[i + 1];
                ptr[i + 1] = ptr[i + 2];
                ptr[i + 2] = temp;
            }
        })
        .SampleGroup("Temp Variable")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            byte temp;
            for (int i = 0; i < values.Length; i += 4)
            {
                temp = ptr[i];
                ptr[i] = ptr[i + 3];
                ptr[i + 3] = temp;

                temp = ptr[i + 1];
                ptr[i + 1] = ptr[i + 2];
                ptr[i + 2] = temp;
            }
        })
        .SampleGroup("Outside Temp Variable")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            for (int i = 0; i < values.Length; i += 4)
            {
                ReverseOrder(ptr);
            }
        })
        .SampleGroup("Static Temp Variable")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            for (int i = 0; i < values.Length; i += 4)
            {
                ReverseOrderInline(ptr);
            }
        })
        .SampleGroup("Static Temp Variable Inline")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            var uintPtr = (uint*)ptr;
            for (int i = 0; i < values.Length; i += 4)
            {
                *uintPtr++ = BigEndian.ReadUInt32(ptr + i);
            }
        })
        .SampleGroup("BigEndian.ReadUInt32")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        values.Dispose();
    }

    // A Test behaves as an ordinary method
    [Test, Performance]
    public unsafe void PerformReordering64Test()
    {
        var values = new NativeArray<ulong>(200000, Allocator.Temp);
        var ptr = (byte*)values.GetUnsafePtr();

        Measure.Method(() =>
        {
            for (int i = 0; i < values.Length; i += 8)
            {
                byte temp = ptr[i];
                ptr[i] = ptr[i + 7];
                ptr[i + 7] = temp;

                temp = ptr[i + 1];
                ptr[i + 1] = ptr[i + 6];
                ptr[i + 6] = temp;

                temp = ptr[i + 2];
                ptr[i + 2] = ptr[i + 5];
                ptr[i + 5] = temp;

                temp = ptr[i + 3];
                ptr[i + 3] = ptr[i + 4];
                ptr[i + 4] = temp;
            }
        })
        .SampleGroup("Temp Variable")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            byte temp;
            for (int i = 0; i < values.Length; i += 8)
            {
                temp = ptr[i];
                ptr[i] = ptr[i + 7];
                ptr[i + 7] = temp;

                temp = ptr[i + 1];
                ptr[i + 1] = ptr[i + 6];
                ptr[i + 6] = temp;

                temp = ptr[i + 2];
                ptr[i + 2] = ptr[i + 5];
                ptr[i + 5] = temp;

                temp = ptr[i + 3];
                ptr[i + 3] = ptr[i + 4];
                ptr[i + 4] = temp;
            }
        })
        .SampleGroup("Outside Temp Variable")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            for (int i = 0; i < values.Length; i += 8)
            {
                ReverseOrder64(ptr);
            }
        })
        .SampleGroup("Static Temp Variable")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            for (int i = 0; i < values.Length; i += 8)
            {
                ReverseOrder64Inline(ptr);
            }
        })
        .SampleGroup("Static Temp Variable Inline")
        .WarmupCount(10)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            var ulongPtr = (ulong*)ptr;
            for (int i = 0; i < values.Length; i += 8)
            {
                *ulongPtr++ = BigEndian.ReadUInt64(ptr + i);
            }
        })
        .SampleGroup("BigEndian.ReadUInt64")
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

        public static ulong ReadUInt64(byte* data) =>
            (ulong)data[0] << 56 | (ulong)data[1] << 48 | (ulong)data[2] << 40 | (ulong)data[3] << 32 |
            (ulong)data[4] << 24 | (ulong)data[5] << 16 | (ulong)data[6] << 8 | data[7];
    }

    public unsafe static void ReverseOrder(byte* ptr)
    {
        var temp = ptr[0];
        ptr[0] = ptr[3];
        ptr[3] = temp;

        temp = ptr[1];
        ptr[1] = ptr[2];
        ptr[2] = temp;
    }

    public unsafe static void ReverseOrder64(byte* ptr)
    {
        var temp = ptr[0];
        ptr[0] = ptr[7];
        ptr[7] = temp;

        temp = ptr[1];
        ptr[1] = ptr[6];
        ptr[6] = temp;

        temp = ptr[2];
        ptr[2] = ptr[5];
        ptr[5] = temp;

        temp = ptr[3];
        ptr[3] = ptr[4];
        ptr[4] = temp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void ReverseOrderInline(byte* ptr)
    {
        var temp = ptr[0];
        ptr[0] = ptr[3];
        ptr[3] = temp;

        temp = ptr[1];
        ptr[1] = ptr[2];
        ptr[2] = temp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void ReverseOrder64Inline(byte* ptr)
    {
        var temp = ptr[0];
        ptr[0] = ptr[7];
        ptr[7] = temp;

        temp = ptr[1];
        ptr[1] = ptr[6];
        ptr[6] = temp;

        temp = ptr[2];
        ptr[2] = ptr[5];
        ptr[5] = temp;

        temp = ptr[3];
        ptr[3] = ptr[4];
        ptr[4] = temp;
    }
}
