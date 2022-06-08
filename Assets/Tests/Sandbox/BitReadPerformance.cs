using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public unsafe class BitReadPerformance
{
    // A Test behaves as an ordinary method
    [Test, Performance]
    public void BitReadPerformanceTest()
    {
        using var array = new NativeArray<byte>(16000, Allocator.Temp);

        Measure.Method(() =>
        {
            int sum = 0;
            byte* buffer = (byte*)array.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < array.Length; i += 16)
            {
                sum ^= GetInt32(buffer);
                sum ^= GetInt32(buffer + 4);
                sum ^= GetInt32(buffer + 8);
                sum ^= GetInt32(buffer + 12);

                buffer += 16;
            }
        })
        .SampleGroup("GetInt32")
        .WarmupCount(20)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            int sum = 0;
            byte* buffer = (byte*)array.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < array.Length; i += 16)
            {
                sum ^= GetInt32(buffer); buffer += 4;
                sum ^= GetInt32(buffer); buffer += 4;
                sum ^= GetInt32(buffer); buffer += 4;
                sum ^= GetInt32(buffer); buffer += 4;
            }
        })
        .SampleGroup("GetInt32 + Offset")
        .WarmupCount(20)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            int sum = 0;
            byte* buffer = (byte*)array.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < array.Length; i += 16)
            {
                sum ^= ReadInt32Backward(ref buffer);
                sum ^= ReadInt32Backward(ref buffer);
                sum ^= ReadInt32Backward(ref buffer);
                sum ^= ReadInt32Backward(ref buffer);
            }
        })
        .SampleGroup("ReadInt32Backward")
        .WarmupCount(20)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            int sum = 0;
            byte* buffer = (byte*)array.GetUnsafeReadOnlyPtr();

            int offset = 0;
            for (int i = 0; i < array.Length; i += 16)
            {
                sum ^= ReadInt32Seperate(buffer, ref offset);
                sum ^= ReadInt32Seperate(buffer + offset, ref offset);
                sum ^= ReadInt32Seperate(buffer + offset, ref offset);
                sum ^= ReadInt32Seperate(buffer + offset, ref offset);
            }
        })
        .SampleGroup("ReadInt32Seperate")
        .WarmupCount(20)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            int sum = 0;
            var stream = new BitStream()
            {
                buffer = (byte*)array.GetUnsafeReadOnlyPtr(),
                offset = 0,
            };

            for (int i = 0; i < array.Length; i += 16)
            {
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
            }
        })
        .SampleGroup("BitStream.ReadInt32")
        .WarmupCount(20)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();
    }

    public struct BitStream
    { 
        public byte* buffer;
        public int offset;

        public int ReadInt32()
        {
            offset += 4;
            return GetInt32(buffer + offset - 4);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt32Seperate(byte* data, ref int offset)
    {
        offset += 4;
        return GetInt32(data);
    }

    [BurstCompile(CompileSynchronously = true)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt32Backward(ref byte* data)
    {
        data += 4;
        return GetInt32(data - 4);
    }

    [BurstCompile(CompileSynchronously = true)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetInt32(byte* data) =>
        (int)data[0] << 24 | (int)data[1] << 16 | (int)data[2] << 8 | data[3];
}
