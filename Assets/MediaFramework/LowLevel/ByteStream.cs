using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.MediaFramework.LowLevel.Unsafe;

namespace Unity.MediaFramework.LowLevel
{
    public interface ByteReader
    {
        public int Length { get; }
        public int Capacity { get; }

        public sbyte PeekInt8();
        public short PeekInt16();
        public int PeekInt32();
        public long PeekInt64();
        public byte PeekUInt8();
        public ushort PeekUInt16();
        public uint PeekUInt32();
        public ulong PeekUInt64();

        public sbyte PeekInt8(int offset);
        public short PeekInt16(int offset);
        public int PeekInt32(int offset);
        public long PeekInt64(int offset);
        public byte PeekUInt8(int offset);
        public ushort PeekUInt16(int offset);
        public uint PeekUInt32(int offset);
        public ulong PeekUInt64(int offset);

        public sbyte ReadInt8();
        public short ReadInt16();
        public int ReadInt32();
        public long ReadInt64();
        public byte ReadUInt8();
        public ushort ReadUInt16();
        public uint ReadUInt32();
        public ulong ReadUInt64();

        public void Seek(int offset);
    }

    public interface ByteWriter
    {
        public int Length { get; }
        public int Capacity { get; }

        void WriteInt8(sbyte value);
        void WriteInt16(short value);
        void WriteInt32(int value);
        void WriteInt64(long value);
        void WriteUInt8(byte value);
        void WriteUInt16(ushort value);
        void WriteUInt24(uint value);
        void WriteUInt32(uint value);
        void WriteUInt64(ulong value);
        void WriteBytes(byte value, int count);
        unsafe void WriteBytes(byte* srcArray, int srcLength);

        public void Seek(int offset);
        public void RemoveLast(int count);
        public void Clear();
    }

    public enum Endianess { Little, Big }

    public unsafe static class ByteStreamExtensions
    {
        public static ByteWriter AsByteWriter(this NativeArray<byte> array, Endianess endian) => endian switch
        {
            Endianess.Big => new BigEndianByteWriter() 
            { 
                array = (byte*)array.GetUnsafePtr(), 
                capacity = array.Length, 
                length = 0 
            },
#if ENABLE_UNITY_MEDIA_CHECKS
            _ => throw new NotSupportedException()
#endif
        };

        public static ByteReader AsByteReader(this NativeArray<byte> array, Endianess endian) => endian switch
        {
            Endianess.Big => new BigEndianByteReader()
            {
                array = (byte*)array.GetUnsafePtr(),
                capacity = array.Length,
                length = 0
            },
#if ENABLE_UNITY_MEDIA_CHECKS
            _ => throw new NotSupportedException()
#endif
        };
    }
}
