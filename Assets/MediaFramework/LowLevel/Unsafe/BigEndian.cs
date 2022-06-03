using System.Runtime.CompilerServices;
using Unity.Burst;

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
}
