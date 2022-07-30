using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public class BinaryStreamPerformance
{
    [Test, Performance]
    public unsafe void BinaryReaderVsNativeArrayMixes()
    {
        using var bigBuffer = new NativeArray<byte>(200000, Allocator.Temp);

        Measure.Method(() =>
        {
            ulong sum = 0;
            var stream = new UnsafeByteReader.BigEndian((byte*)bigBuffer.GetUnsafeReadOnlyPtr(), bigBuffer.Length);

            for (int i = 0; i < bigBuffer.Length; i += 20)
            {
                sum += stream.ReadULong();
                sum += stream.ReadUInt();
                sum += stream.ReadULong();
            }
        })
        .SampleGroup("UnsafeBinaryReader.BigEndian")
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
                sum += BigEndian.GetULong(buffer + i);
                sum += BigEndian.GetUInt(buffer + i + 8);
                sum += BigEndian.GetULong(buffer + i + 12);
            }
        })
        .SampleGroup("NativeArray.BigEndian")
        .WarmupCount(5)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            ulong sum = 0;
            var stream = new UnsafeByteReader((byte*)bigBuffer.GetUnsafeReadOnlyPtr(), bigBuffer.Length);

            for (int i = 0; i < bigBuffer.Length; i += 20)
            {
                sum += stream.ReadULong();
                sum += stream.ReadUInt();
                sum += stream.ReadULong();
            }
        })
        .SampleGroup("UnsafeBinaryReader")
        .WarmupCount(5)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            ulong sum = 0;
            byte* buffer = (byte*)bigBuffer.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < bigBuffer.Length; i += 20)
            {
                sum += UnsafeUtility.AsRef<ulong>(buffer + i);
                sum += UnsafeUtility.AsRef<uint>(buffer + i + 8);
                sum += UnsafeUtility.AsRef<ulong>(buffer + i + 12);
            }
        })
        .SampleGroup("NativeArray")
        .WarmupCount(5)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();
    }

    [Test, Performance]
    public unsafe void BinaryReaderVsNativeArray64()
    {
        using var bigBuffer = new NativeArray<byte>(160000, Allocator.Temp);

        Measure.Method(() =>
        {
            ulong sum = 0;
            var stream = new UnsafeByteReader.BigEndian((byte*)bigBuffer.GetUnsafeReadOnlyPtr(), bigBuffer.Length);

            for (int i = 0; i < bigBuffer.Length; i += 16)
            {
                sum += stream.ReadULong();
                sum += stream.ReadULong();
            }
        })
        .SampleGroup("UnsafeBinaryReader.BigEndian")
        .WarmupCount(5)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            ulong sum = 0;
            byte* buffer = (byte*)bigBuffer.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < bigBuffer.Length; i += 16)
            {
                sum += BigEndian.GetULong(buffer + i);
                sum += BigEndian.GetULong(buffer + i + 8);
            }
        })
        .SampleGroup("NativeArray.BigEndian")
        .WarmupCount(5)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            ulong sum = 0;
            var stream = new UnsafeByteReader((byte*)bigBuffer.GetUnsafeReadOnlyPtr(), bigBuffer.Length);

            for (int i = 0; i < bigBuffer.Length; i += 16)
            {
                sum += stream.ReadULong();
                sum += stream.ReadULong();
            }
        })
        .SampleGroup("UnsafeBinaryReader")
        .WarmupCount(5)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            ulong sum = 0;
            byte* buffer = (byte*)bigBuffer.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < bigBuffer.Length; i += 16)
            {
                sum += UnsafeUtility.AsRef<ulong>(buffer + i);
                sum += UnsafeUtility.AsRef<ulong>(buffer + i + 8);
            }
        })
        .SampleGroup("NativeArray")
        .WarmupCount(5)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();
    }

    public unsafe struct UnsafeByteReader
    {
        public struct BigEndian
        {
            [NativeDisableUnsafePtrRestriction]
            public byte* m_Head;

            [NativeDisableUnsafePtrRestriction]
            public readonly byte* m_Buffer;

            public readonly int m_Length;

            public int Index => (int)(m_Head - m_Buffer);

            public int Length => m_Length;

            public BigEndian(byte* buffer, int length)
            {
                m_Buffer = m_Head = buffer;
                m_Length = length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint ReadUInt()
            {
                CheckForOutOfRange(4);

                return (uint)(*m_Head++ << 24 | *m_Head++ << 16 | *m_Head++ << 8 | *m_Head++);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong ReadULong()
            {
                CheckForOutOfRange(8);

                return (ulong)*m_Head++ << 56 | (ulong)*m_Head++ << 48 | (ulong)*m_Head++ << 40 | (ulong)*m_Head++ << 32 |
                       (ulong)*m_Head++ << 24 | (ulong)*m_Head++ << 16 | (ulong)*m_Head++ << 8 | *m_Head++;
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            public void CheckForOutOfRange(int count)
            {
                if (Index >= m_Length)
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        [NativeDisableUnsafePtrRestriction]
        public byte* m_Head;

        [NativeDisableUnsafePtrRestriction]
        public readonly byte* m_Buffer;

        public readonly int m_Length;

        public int Index => (int)(m_Head - m_Buffer);

        public int Length => m_Length;

        public UnsafeByteReader(byte* buffer, int length)
        {
            m_Buffer = m_Head = buffer;
            m_Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref uint ReadUInt()
        {
            CheckForOutOfRange(4);

            ref var refValue = ref UnsafeUtility.AsRef<uint>(m_Head);
            m_Head += 4;
            return ref refValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ulong ReadULong()
        {
            CheckForOutOfRange(8);

            ref var refValue = ref UnsafeUtility.AsRef<ulong>(m_Head);
            m_Head += 4;
            return ref refValue;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void CheckForOutOfRange(int count)
        {
            if (Index >= m_Length)
                throw new System.ArgumentOutOfRangeException();
        }
    }

    public unsafe static class BigEndian
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUInt(byte* data) =>
                (uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | (uint)data[3];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULong(byte* data) =>
            (ulong)data[0] << 56 | (ulong)data[1] << 48 | (ulong)data[2] << 40 | (ulong)data[3] << 32 |
            (ulong)data[4] << 24 | (ulong)data[5] << 16 | (ulong)data[6] << 8 | (ulong)data[7];

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
