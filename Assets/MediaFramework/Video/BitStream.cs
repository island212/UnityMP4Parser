using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.MediaFramework.Video
{
    public unsafe struct BitStream
    {
        [NativeDisableUnsafePtrRestriction]
        public byte* buffer;
        public ulong length;

        public BitStream(NativeList<byte> list)
        {
            length = (ulong)list.Length;
            buffer = (byte*)list.GetUnsafePtr();
        }

        public bool EndOfStream() => length == 0;

        public byte PeekByte() => buffer[0];

        public byte PeekByte(int offset) => buffer[offset];

        public uint PeekUInt32() => BitTools.GetUInt32(buffer);

        public uint PeekUInt32(int offset) => BitTools.GetUInt32(buffer + offset);

        public ulong PeekUInt64() => BitTools.GetUInt64(buffer);

        public ulong PeekUInt64(int offset) => BitTools.GetUInt64(buffer + offset);

        public uint ReadUInt32()
        {
            Seek(4);
            return BitTools.GetUInt32(buffer - 4);
        }

        public ulong ReadUInt64()
        {
            Seek(8);
            return BitTools.GetUInt64(buffer - 8);
        }

        public void Seek(uint seek)
        {
            buffer += math.min(length, seek);
            length -= math.min(length, seek);
        }

        public void Seek(ulong seek)
        {
            buffer += math.min(length, seek);
            length -= math.min(length, seek);
        }

        public void CopyInto(ref NativeList<byte> dest, ulong count)
        {
            ulong copied = 0;
            do
            {
                ulong mincount = math.min(int.MaxValue, count - copied);
                dest.AddRange(buffer, (int)mincount);
                copied += mincount;
            }
            while (copied < count);
        }

        public void CopyInto(ref NativeList<byte> dest, uint count)
        {
            uint copied = 0;
            do
            {
                uint mincount = math.min(int.MaxValue, count - copied);
                dest.AddRange(buffer, (int)mincount);
                copied += mincount;
            }
            while (copied < count);
        }

        public void CopyInto(ref NativeList<byte> dest, int count) => dest.AddRange(buffer, count);
    }

    public unsafe static class BitTools
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
