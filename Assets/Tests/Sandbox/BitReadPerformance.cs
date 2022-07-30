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

        byte* readonlyBuffer = (byte*)array.GetUnsafeReadOnlyPtr();

        Measure.Method(() =>
        {
            int sum = 0;
            byte* buffer = readonlyBuffer;

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

            byte* buffer = readonlyBuffer;
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

            byte* buffer = readonlyBuffer;
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
            int offset = 0;
            byte* buffer = readonlyBuffer;
            for (int i = 0; i < array.Length; i += 16)
            {
                sum ^= ReadInt32Seperate(buffer, ref offset);
                sum ^= ReadInt32Seperate(buffer, ref offset);
                sum ^= ReadInt32Seperate(buffer, ref offset);
                sum ^= ReadInt32Seperate(buffer, ref offset);
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
                buffer = readonlyBuffer,
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

        Measure.Method(() =>
        {
            int sum = 0;
            var stream = new BitNoOffset()
            {
                buffer = readonlyBuffer
            };

            for (int i = 0; i < array.Length; i += 16)
            {
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
            }
        })
        .SampleGroup("BitNoOffset.ReadInt32")
        .WarmupCount(20)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            int sum = 0;
            var stream = new BitNoOffsetNoTemp()
            {
                buffer = readonlyBuffer
            };

            for (int i = 0; i < array.Length; i += 16)
            {
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
            }
        })
        .SampleGroup("BitNoOffsetNoTemp.ReadInt32")
        .WarmupCount(20)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            int sum = 0;
            var stream = new BitNoOffsetNoTempOneline(readonlyBuffer, array.Length);

            for (int i = 0; i < array.Length; i += 16)
            {
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
                sum ^= stream.ReadInt32();
            }
        })
        .SampleGroup("BitNoOffsetNoTempOneline.ReadInt32")
        .WarmupCount(20)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();
    }

    public struct BitStream
    { 
        public byte* buffer;
        public int offset;

        public int ReadInt32() =>
            *(buffer + offset++) << 24 | *(buffer + offset++) << 16 | *(buffer + offset++) << 8 | *(buffer + offset++);
    }

    public struct BitNoOffset
    {
        public byte* buffer;

        public int ReadInt32()
        {
            var temp = buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
            buffer += 4;
            return temp;
        }
    }

    public struct BitNoOffsetNoTempOneline
    {
        public byte* start;
        public byte* end;

        public byte* head;

        public BitNoOffsetNoTempOneline(byte* buffer, int length)
        {
            start = head = buffer;
            end = buffer + length;
        }

        public int ReadInt32() => *head++ << 24 | *head++ << 16 | *head++ << 8 | *head++;
    }

    public struct BitNoOffsetNoTemp
    {
        public int length;
        public byte* buffer;

        public int ReadInt32()
        {
            buffer += 4;
            return *(buffer - 4) << 24 | *(buffer - 3) << 16 | *(buffer - 2) << 8 | *(buffer - 1);
        }
    }

    public static int ReadInt32Seperate(byte* data, ref int offset)
    {
        offset += 4;
        return *(data + offset - 4) << 24 | *(data + offset - 3) << 16 | *(data + offset - 2) << 8 | *(data + offset - 1);
    }

    public static int ReadInt32Backward(ref byte* data)
    {
        data += 4;
        return *(data - 4) << 24 | *(data - 3) << 16 | *(data - 2) << 8 | *(data - 1);
    }

    public static int GetInt32(byte* data) =>
        data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3];
}
