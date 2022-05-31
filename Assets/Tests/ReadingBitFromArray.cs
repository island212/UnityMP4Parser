using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;


public unsafe class ReadingBitFromArray
{
    [Test, Performance]
    public void ReadingBitFromArrayTest()
    {
        int sampleSize = 1000;
        var array = new NativeArray<byte>(8 * sampleSize, Allocator.Temp);

        byte storeValue = 9;
        ulong answer = storeValue * (ulong)sampleSize;
        for (int i = 7; i < array.Length; i+=8)
            array[i] = storeValue;

        var buffer = array.GetUnsafeReadOnlyPtr();

        ulong finalSum = 0;
        Measure.Method(() =>
        {
            ulong value = 0;
            for (int i = 0; i < array.Length; i+=8)
            {
                value += GetUInt64((byte*)buffer + i);
            }

            finalSum = value;
        })
        .SampleGroup("GetUInt64")
        .WarmupCount(5)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Debug.Assert(finalSum == answer, $"Failed to sum GetUInt64 {finalSum} != {answer}");

        Measure.Method(() =>
        {
            ulong value = 0;
            for (int i = 0; i < array.Length; i += 8)
            {
                value += UnsafeUtility.ReadArrayElementWithStride<ulong>(buffer, i, 1);
            }

            finalSum = value;
        })
        .SampleGroup("UnsafeUtility.ReadArrayElement")
        .WarmupCount(10)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        //Debug.Assert(finalSum == answer, $"Failed to sum UnsafeUtility.ReadArrayElement {finalSum} != {answer}");

        Measure.Method(() =>
        {
            byte* tempBuffer = stackalloc byte[8];

            ulong value = 0;
            for (int i = 0; i < array.Length; i += 8)
            {
                value += ReadArrayElement(tempBuffer, (byte*)buffer, i);
            }

            finalSum = value;
        })
        .SampleGroup("ReadArrayElement")
        .WarmupCount(10)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Debug.Assert(finalSum == answer, $"Failed to sum ReadArrayElement {finalSum} != {answer}");

        //Measure.Method(() =>
        //{
        //    ulong value = 0;
        //    for (int i = 0; i < array.Length; i+=8)
        //    {
        //        value += array.ReinterpretLoad<ulong>(i);
        //    }

        //    finalSum = value;
        //})
        //.SampleGroup("ReinterpretLoad")
        //.WarmupCount(5)
        //.IterationsPerMeasurement(100)
        //.MeasurementCount(20)
        //.Run();

        //Debug.Assert(finalSum == answer, $"Failed to sum ReinterpretLoad {finalSum} != {answer}");

        Measure.Method(() =>
        {
            ulong value = 0;
            for (int i = 0; i < array.Length; i += 8)
            {
                value += GetUInt64WithValue((byte*)buffer + i);
            }

            finalSum = value;
        })
        .SampleGroup("GetUInt64WithValue")
        .WarmupCount(10)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Debug.Assert(finalSum == answer, $"Failed to sum GetUInt64WithValue {finalSum} != {answer}");

        array.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    public unsafe static ulong ReadArrayElement(byte* temp, byte* source, int index)
    {
        temp[0] = source[index + 7];
        temp[1] = source[index + 6];
        temp[2] = source[index + 5];
        temp[3] = source[index + 4];
        temp[4] = source[index + 3];
        temp[5] = source[index + 2];
        temp[6] = source[index + 1];
        temp[7] = source[index + 0];
        return *(ulong*)temp;
    }

    [BurstCompile(CompileSynchronously = true)]
    public static ulong GetBigEndianUInt64(byte* data) =>
        (uint)data[7] << 56 | (uint)data[6] << 48 | (uint)data[5] << 40 | (uint)data[4] << 32 |
        (uint)data[3] << 24 | (uint)data[2] << 16 | (uint)data[1] << 8 | (uint)data[0];

    [BurstCompile(CompileSynchronously = true)]
    public static ulong GetUInt64(byte* data) =>
        (uint)data[0] << 56 | (uint)data[1] << 48 | (uint)data[2] << 40 | (uint)data[3] << 32 |
        (uint)data[4] << 24 | (uint)data[5] << 16 | (uint)data[6] << 8 | (uint)data[7];

    [BurstCompile(CompileSynchronously = true)]
    public static ulong GetUInt64WithValue(byte* data)
    {
        ulong value = data[0];
        value <<= 8;
        value |= data[1];
        value <<= 8;
        value |= data[2];
        value <<= 8;
        value |= data[3];
        value <<= 8;
        value |= data[4];
        value <<= 8;
        value |= data[5];
        value <<= 8;
        value |= data[6];
        value <<= 8;
        value |= data[7];
        return value;
    }
}
