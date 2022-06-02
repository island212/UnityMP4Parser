using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public class BitStreamPerformance
{
    // A Test behaves as an ordinary method
    [Test, Performance]
    public unsafe void BitStreamVsNativeArray()
    {
        var bigBuffer = new NativeArray<byte>(200000, Allocator.Temp);

        Measure.Method(() =>
        {
            ulong sum = 0;
            var stream = new BitStream((byte*)bigBuffer.GetUnsafeReadOnlyPtr(), bigBuffer.Length);

            for (int i = 0; i < stream.Length; i+=20)
            {
                sum += stream.PeekUInt64(i);
                sum += stream.PeekUInt32(i + 8);
                sum += stream.PeekUInt64(i + 12);
            }
        })
        .SampleGroup("BitStream")
        .WarmupCount(5)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            ulong sum = 0;
            byte* buffer = (byte*)bigBuffer.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < bigBuffer.Length; i+=20)
            {
                sum += BigEndian.GetUInt64(buffer + i);
                sum += BigEndian.GetUInt32(buffer + i + 8);
                sum += BigEndian.GetUInt64(buffer + i + 12);
            }
        })
        .SampleGroup("NativeArray")
        .WarmupCount(5)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();
    }

    [BurstCompile]
    public unsafe struct BitStream
    {
        [NativeDisableUnsafePtrRestriction]
        public byte* Buffer;
        public int Length;

        public int Position;

        public BitStream(byte* buffer, int length)
        {
            this.Length = length;
            this.Buffer = buffer;

            Position = 0;
        }

        public int Remains() => Length - 1 - Position;

        public bool EndOfStream() => Position == Length - 1;

        public byte PeekByte() => *(Buffer + Position);

        public byte PeekByte(int offset) => *(Buffer + Position + offset);

        public uint PeekUInt32() => BigEndian.GetUInt32(Buffer + Position);

        public uint PeekUInt32(int offset) => BigEndian.GetUInt32(Buffer + Position + offset);

        public ulong PeekUInt64() => BigEndian.GetUInt64(Buffer + Position);

        public ulong PeekUInt64(int offset) => BigEndian.GetUInt64(Buffer + Position + offset);

        public void Seek(int seek)
        {
            Position = math.min(Position + seek, Length - 1);
        }
    }

    [BurstCompile]
    public unsafe static class BigEndian
    {
        public static uint GetUInt32(byte* data) =>
                (uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | (uint)data[3];

        public static ulong GetUInt64(byte* data) =>
            (uint)data[0] << 56 | (uint)data[1] << 48 | (uint)data[2] << 40 | (uint)data[3] << 32 |
            (uint)data[4] << 24 | (uint)data[5] << 16 | (uint)data[6] << 8 | (uint)data[7];

        public static string ConvertToString(uint data) =>
            new string(new char[4] {
                (char)((data & 0xFF000000) >> 24),
                (char)((data & 0x00FF0000) >> 16),
                (char)((data & 0x0000FF00) >> 8),
                (char)((data & 0x000000FF))
            });

        public static uint ConvertFourCCToUInt32(string data) =>
            (uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | (uint)data[3];
    }
}
