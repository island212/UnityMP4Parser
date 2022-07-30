using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.MediaFramework.LowLevel.Unsafe
{
    public unsafe static class BigEndian
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(byte* data) => (ushort)(data[0] << 8 | data[1]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(byte* data) =>
                (uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | data[3];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Read2UInt32(byte* data) =>
            (ulong)data[4] << 56 | (ulong)data[5] << 48 | (ulong)data[6] << 40 | (ulong)data[7] << 32 |
            (ulong)data[0] << 24 | (ulong)data[1] << 16 | (ulong)data[2] << 8 | data[3];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64(byte* data) =>
            (ulong)data[0] << 56 | (ulong)data[1] << 48 | (ulong)data[2] << 40 | (ulong)data[3] << 32 |
            (ulong)data[4] << 24 | (ulong)data[5] << 16 | (ulong)data[6] << 8 | data[7];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(byte* data) => (short)(data[0] << 8 | data[1]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(byte* data) =>
            (int)data[0] << 24 | (int)data[1] << 16 | (int)data[2] << 8 | data[3];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(byte* data) =>
            (long)data[0] << 56 | (long)data[1] << 48 | (long)data[2] << 40 | (long)data[3] << 32 |
            (long)data[4] << 24 | (long)data[5] << 16 | (long)data[6] << 8 | data[7];

        public static void WriteUInt16(byte* data, ushort value)
        {
            data[0] = (byte)(value >> 8);
            data[1] = (byte)value;
        }

        public static void WriteUInt24(byte* data, uint value)
        {
            data[0] = (byte)(value >> 16);
            data[1] = (byte)(value >> 8);
            data[2] = (byte)value;
        }

        public static void WriteUInt32(byte* data, uint value)
        {
            data[0] = (byte)(value >> 24);
            data[1] = (byte)(value >> 16);
            data[2] = (byte)(value >> 8);
            data[3] = (byte)value;
        }

        public static void WriteUInt64(byte* data, ulong value)
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

        public static void WriteInt16(byte* data, short value) => WriteUInt16(data, (ushort)value);

        public static void WriteInt32(byte* data, int value) => WriteUInt32(data, (uint)value);

        public static void WriteInt64(byte* data, long value) => WriteUInt64(data, (ulong)value);

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

    public unsafe struct BigEndianByteReader : ByteReader
    {
        public byte* array;
        public int length;
        public int capacity;

        public int Length => length;

        public short PeekInt16()
        {
            CheckIfOutOfBounds(2);

            return BigEndian.ReadInt16(array + length);
        }

        public short PeekInt16(int offset)
        {
            CheckIfOutOfBounds(offset + 2);

            return BigEndian.ReadInt16(array + length + offset);
        }

        public int PeekInt32()
        {
            CheckIfOutOfBounds(4);

            return BigEndian.ReadInt32(array + length);
        }

        public int PeekInt32(int offset)
        {
            CheckIfOutOfBounds(4);

            return BigEndian.ReadInt32(array + length);
        }

        public long PeekInt64()
        {
            CheckIfOutOfBounds(8);

            return BigEndian.ReadInt64(array + length);
        }

        public long PeekInt64(int offset)
        {
            CheckIfOutOfBounds(8);

            return BigEndian.ReadInt64(array + length);
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

            return BigEndian.ReadUInt16(array + length);
        }

        public ushort PeekUInt16(int offset)
        {
            CheckIfOutOfBounds(offset + 2);

            return BigEndian.ReadUInt16(array + length + offset);
        }

        public uint PeekUInt32()
        {
            CheckIfOutOfBounds(4);

            return BigEndian.ReadUInt32(array + length);
        }

        public uint PeekUInt32(int offset)
        {
            CheckIfOutOfBounds(offset + 4);

            return BigEndian.ReadUInt32(array + length + offset);
        }

        public ulong PeekUInt64()
        {
            CheckIfOutOfBounds(8);

            return BigEndian.ReadUInt64(array + length);
        }

        public ulong PeekUInt64(int offset)
        {
            CheckIfOutOfBounds(offset + 8);

            return BigEndian.ReadUInt64(array + length + offset);
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
            return BigEndian.ReadInt16(array + length - 2);
        }

        public int ReadInt32()
        {
            CheckIfOutOfBounds(4);

            length += 4;
            return BigEndian.ReadInt32(array + length - 4);
        }

        public long ReadInt64()
        {
            CheckIfOutOfBounds(8);

            length += 8;
            return BigEndian.ReadInt64(array + length - 8);
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
            return BigEndian.ReadUInt16(array + length - 2);
        }

        public uint ReadUInt32()
        {
            CheckIfOutOfBounds(4);

            length += 4;
            return BigEndian.ReadUInt32(array + length - 4);
        }

        public ulong ReadUInt64()
        {
            CheckIfOutOfBounds(8);

            length += 8;
            return BigEndian.ReadUInt64(array + length - 8);
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
                throw new System.ArgumentOutOfRangeException($"Error write {size} bytes at {length} with a capacity of {capacity}");
        }
    }

    public unsafe struct BigEndianByteWriter : ByteWriter
    {
        public byte* array;
        public int length;
        public int capacity;

        public int Length => length;

        public int Capacity => capacity;

        public void WriteUInt8(byte value)
        {
            CheckIfOutOfBounds(1);

            array[length] = value;

            length += 1;
        }

        public void WriteUInt16(ushort value)
        {
            CheckIfOutOfBounds(2);

            BigEndian.WriteUInt16(array + length, value);

            length += 2;
        }

        public void WriteUInt24(uint value)
        {
            CheckIfOutOfBounds(3);

            BigEndian.WriteUInt24(array + length, value & 0x00FFFFFF);

            length += 3;
        }

        public void WriteUInt32(uint value)
        {
            CheckIfOutOfBounds(4);

            BigEndian.WriteUInt32(array + length, value);

            length += 4;
        }

        public void WriteUInt64(ulong value)
        {
            CheckIfOutOfBounds(8);

            BigEndian.WriteUInt64(array + length, value);

            length += 8;
        }

        public void WriteInt16(short value)
        {
            CheckIfOutOfBounds(2);

            BigEndian.WriteInt16(array + length, value);

            length += 2;
        }

        public void WriteInt32(int value)
        {
            CheckIfOutOfBounds(4);

            BigEndian.WriteInt32(array + length, value);

            length += 4;
        }

        public void WriteInt64(long value)
        {
            CheckIfOutOfBounds(8);

            BigEndian.WriteInt64(array + length, value);

            length += 8;
        }

        public void WriteInt8(sbyte value)
        {
            CheckIfOutOfBounds(1);

            array[length] = (byte)value;

            length += 1;
        }

        public void WriteBytes(byte value, int count)
        {
            CheckIfOutOfBounds(count);

            UnsafeUtility.MemSet(array + length, value, count);

            length += count;
        }

        public unsafe void WriteBytes(byte* srcArray, int srcLength)
        {
            CheckIfOutOfBounds(srcLength);

            UnsafeUtility.MemCpy(array + length, srcArray, srcLength);

            length += srcLength;
        }

        public void Seek(int offset)
        {
            length += math.min(offset, capacity - length);
        }

        public void RemoveLast(int count)
        {
            math.max(length - count, 0);
        }

        public void Clear()
        {
            length = 0;
        }

        [Conditional("ENABLE_UNITY_MEDIA_CHECKS")]
        public void CheckIfOutOfBounds(int size)
        {
            if (length + size >= capacity)
                throw new System.ArgumentOutOfRangeException($"Error: write {size} bytes at {length} with a capacity of {capacity}");
        }
    }

    public enum ReaderError
    {
        None,
        Overflow,
        OutOfRange
    }

    public unsafe struct BitReader
    {
        public ReaderError Error;
        public int Position;
        public int Length;
        public byte* Stream;

        public BitReader(byte* buffer, int length)
        {
            Error = ReaderError.None;

            Position = 0;
            Stream = buffer;
            Length = length * 8;
        }

        public bool HasEnoughBits(int bits) => Length - Position >= bits;

        public byte ReadBit()
        {
            var p = (Position >> 3);
            var o = 0x07 - (Position & 0x07);
            var val = (Stream[p] >> o) & 0x01;
            Position++;
            return (byte)val;
        }

        public uint ReadBits(int bits)
        {
            uint value = 0;
            for (int i = 0; i < bits; i++)
            {
                value = (value << 1) | ReadBit();
            }
            return value;
        }

        public bool ReadBool() => ReadBit() == 1;

        public uint ReadUExpGolomb()
        {
            if (!HasEnoughBits(1))
            {
                Error = ReaderError.OutOfRange;
                return 0;
            }            

            var zeros = 0;
            while (ReadBit() == 0)
            {
                zeros++;
                if (!HasEnoughBits(zeros * 2 + 1))
                {
                    Error = ReaderError.OutOfRange;
                    return 0;
                }

                if (zeros > 32)
                {
                    Error = ReaderError.Overflow;
                    return 0;
                }
            }

            var value = 1u << zeros;
            for (var i = zeros - 1; i >= 0; i--)
                value |= (uint)(ReadBit() << i);

            return value - 1;
        }

        public int ReadSExpGolomb()
        {
            var value = ReadUExpGolomb();
            return value == 0 || Error != ReaderError.None ? 0 : 
                (value & 0x01) == 0 ? 1 + (int)(value >> 1) : -(int)(value >> 1);
        }
    }
}
