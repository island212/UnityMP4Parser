using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.MediaFramework.LowLevel.Unsafe
{
    [BurstCompile]
    public unsafe static class BigEndian
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetUInt16(byte* data) => (ushort)(data[0] << 8 | data[1]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUInt32(byte* data) =>
                (uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | data[3];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetUInt64(byte* data) =>
            (ulong)data[0] << 56 | (ulong)data[1] << 48 | (ulong)data[2] << 40 | (ulong)data[3] << 32 |
            (ulong)data[4] << 24 | (ulong)data[5] << 16 | (ulong)data[6] << 8 | data[7];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short GetInt16(byte* data) => (short)(data[0] << 8 | data[1]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetInt32(byte* data) =>
            (int)data[0] << 24 | (int)data[1] << 16 | (int)data[2] << 8 | data[3];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetInt64(byte* data) =>
            (long)data[0] << 56 | (long)data[1] << 48 | (long)data[2] << 40 | (long)data[3] << 32 |
            (long)data[4] << 24 | (long)data[5] << 16 | (long)data[6] << 8 | data[7];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, ushort value)
        {
            data[0] = (byte)(value >> 8);
            data[1] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, uint value)
        {
            data[0] = (byte)(value >> 24);
            data[1] = (byte)(value >> 16);
            data[2] = (byte)(value >> 8);
            data[3] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, ulong value)
        {
            data[0] = (byte)(value >> 56);
            data[1] = (byte)(value >> 48);
            data[2] = (byte)(value >> 40);
            data[3] = (byte)(value >> 32);
            data[4] = (byte)(value >> 24);
            data[5] = (byte)(value >> 16);
            data[6] = (byte)(value >> 8);
            data[7] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, short value) => Write(data, (ushort)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, int value) => Write(data, (uint)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, long value) => Write(data, (ulong)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ConvertToString(uint data) =>
            new string(new char[4] {
                (char)((data & 0xFF000000) >> 24),
                (char)((data & 0x00FF0000) >> 16),
                (char)((data & 0x0000FF00) >> 8),
                (char)((data & 0x000000FF))
            });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ConvertFourCCToUInt32(string data) =>
            (uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | (uint)data[3];
    }

    [BurstCompile]
    public unsafe struct BigEndianByteReader : ByteReader
    {
        public byte* array;
        public int length;
        public int capacity;

        public int Length => length;

        public int Capacity => capacity;

        public short PeekInt16()
        {
            CheckIfOutOfBounds(2);

            return BigEndian.GetInt16(array + length);
        }

        public short PeekInt16(int offset)
        {
            CheckIfOutOfBounds(offset + 2);

            return BigEndian.GetInt16(array + length + offset);
        }

        public int PeekInt32()
        {
            CheckIfOutOfBounds(4);

            return BigEndian.GetInt32(array + length);
        }

        public int PeekInt32(int offset)
        {
            CheckIfOutOfBounds(4);

            return BigEndian.GetInt32(array + length);
        }

        public long PeekInt64()
        {
            CheckIfOutOfBounds(8);

            return BigEndian.GetInt64(array + length);
        }

        public long PeekInt64(int offset)
        {
            CheckIfOutOfBounds(8);

            return BigEndian.GetInt64(array + length);
        }

        public sbyte PeekInt8()
        {
            CheckIfOutOfBounds(1);

            return (sbyte)array[length];
        }

        public sbyte PeekInt8(int offset)
        {
            CheckIfOutOfBounds(offset + 1);

            return (sbyte)array[length + offset];
        }

        public ushort PeekUInt16()
        {
            CheckIfOutOfBounds(2);

            return BigEndian.GetUInt16(array + length);
        }

        public ushort PeekUInt16(int offset)
        {
            CheckIfOutOfBounds(offset + 2);

            return BigEndian.GetUInt16(array + length + offset);
        }

        public uint PeekUInt32()
        {
            CheckIfOutOfBounds(4);

            return BigEndian.GetUInt32(array + length);
        }

        public uint PeekUInt32(int offset)
        {
            CheckIfOutOfBounds(offset + 4);

            return BigEndian.GetUInt32(array + length + offset);
        }

        public ulong PeekUInt64()
        {
            CheckIfOutOfBounds(8);

            return BigEndian.GetUInt64(array + length);
        }

        public ulong PeekUInt64(int offset)
        {
            CheckIfOutOfBounds(offset + 8);

            return BigEndian.GetUInt64(array + length + offset);
        }

        public byte PeekUInt8()
        {
            CheckIfOutOfBounds(1);

            return array[length];
        }

        public byte PeekUInt8(int offset)
        {
            CheckIfOutOfBounds(offset + 1);

            return array[length + offset];
        }

        public short ReadInt16()
        {
            CheckIfOutOfBounds(2);

            length += 2;
            return BigEndian.GetInt16(array + length - 2);
        }

        public int ReadInt32()
        {
            CheckIfOutOfBounds(4);

            length += 4;
            return BigEndian.GetInt32(array + length - 4);
        }

        public long ReadInt64()
        {
            CheckIfOutOfBounds(8);

            length += 8;
            return BigEndian.GetInt64(array + length - 8);
        }

        public sbyte ReadInt8()
        {
            CheckIfOutOfBounds(1);

            length += 1;
            return (sbyte)array[length - 1];
        }

        public ushort ReadUInt16()
        {
            CheckIfOutOfBounds(2);

            length += 2;
            return BigEndian.GetUInt16(array + length - 2);
        }

        public uint ReadUInt32()
        {
            CheckIfOutOfBounds(4);

            length += 4;
            return BigEndian.GetUInt32(array + length - 4);
        }

        public ulong ReadUInt64()
        {
            CheckIfOutOfBounds(8);

            length += 8;
            return BigEndian.GetUInt64(array + length - 8);
        }

        public byte ReadUInt8()
        {
            CheckIfOutOfBounds(1);

            length += 1;
            return array[length - 1];
        }

        public void Seek(int offset)
        {
            length = math.min(length + offset, capacity);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void CheckIfOutOfBounds(int size)
        {
            if (length + size >= capacity)
                throw new System.ArgumentOutOfRangeException($"Error write {size} bytes at index {length} with a length {capacity}");
        }
    }

    [BurstCompile]
    public unsafe struct BigEndianByteWriter : ByteWriter
    {
        public byte* array;
        public int length;
        public int capacity;

        public int Length => length;

        public int Capacity => capacity;

        public void WriteUInt16(ushort value)
        {
            CheckIfOutOfBounds(2);

            length += 2;
            BigEndian.Write(array + length - 2, value);
        }

        public void WriteUInt32(uint value)
        {
            CheckIfOutOfBounds(4);

            length += 4;
            BigEndian.Write(array + length - 4, value);
        }

        public void WriteUInt64(ulong value)
        {
            CheckIfOutOfBounds(8);

            length += 8;
            BigEndian.Write(array + length - 8, value);
        }

        public void WriteInt16(short value)
        {
            CheckIfOutOfBounds(2);

            length += 2;
            BigEndian.Write(array + length - 2, value);
        }

        public void WriteInt32(int value)
        {
            CheckIfOutOfBounds(4);

            length += 4;
            BigEndian.Write(array + length - 4, value);
        }

        public void WriteInt64(long value)
        {
            CheckIfOutOfBounds(8);

            length += 8;
            BigEndian.Write(array + length - 8, value);
        }

        public void WriteInt8(sbyte value)
        {
            CheckIfOutOfBounds(1);

            length += 1;
            array[length - 1] = (byte)value;
        }

        public void WriteUInt8(byte value)
        {
            CheckIfOutOfBounds(1);

            length += 1;
            array[length - 1] = value;
        }

        public void Seek(int offset)
        {
            length = math.min(length + offset, capacity);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void CheckIfOutOfBounds(int size)
        {
            if (length + size >= capacity)
                throw new System.ArgumentOutOfRangeException($"Error write {size} bytes at index {length} with a length {capacity}");
        }

        public void RemoveLast(int count)
        {
            math.max(length - count, 0);
        }

        public void Clear()
        {
            length = 0;
        }
    }
}
