using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.MediaFramework.Video
{
    public unsafe struct BitStream
    {
        [NativeDisableUnsafePtrRestriction]
        public byte* buffer;
        public int length;

        [NativeDisableUnsafePtrRestriction]
        public byte* current;
        public int position;

        public BitStream(byte* buffer, int length)
        {
            this.length = length;
            this.buffer = buffer;

            current = buffer;
            position = 0;
        }

        public int Remains() => length - 1 - position;

        public bool EndOfStream() => position == length - 1;

        public byte PeekByte() => current[0];

        public byte PeekByte(int offset) => current[offset];

        public uint PeekUInt32() => BitTools.GetUInt32(current);

        public uint PeekUInt32(int offset) => BitTools.GetUInt32(current + offset);

        public ulong PeekUInt64() => BitTools.GetUInt64(current);

        public ulong PeekUInt64(int offset) => BitTools.GetUInt64(current + offset);

        public void Seek(int seek)
        {
            current += math.min(length - 1 - position, seek);
            position = math.min(length - 1, position + seek);
        }
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
