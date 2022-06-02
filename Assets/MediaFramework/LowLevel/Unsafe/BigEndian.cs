using Unity.Burst;

namespace Unity.MediaFramework.LowLevel.Unsafe
{
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
