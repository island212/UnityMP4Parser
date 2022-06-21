using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.MediaFramework.Format.ISOBMFF;

public static class ISOBoxTestFactory
{
    public static class FTYP
    {
        public static FTYPBox Get() =>
            new(ISOBrand.MP42, 512, 4, ISOBrand.ISOM, ISOBrand.ISO2, ISOBrand.AVC1, ISOBrand.MP41, 0);
    }

    public static class MVHD
    {
        public static MVHDBox Get() =>
            new(0, 3395928231, 3395928231, 600, 7222220, 65536, 256, new int3x3(65536, 0, 0, 0, 65536, 0, 0, 0, 1073741824), 3);
    }

    public static class TKHD
    {
        public static TKHDBox Get() => GetVideo();

        public static TKHDBox GetVideo() =>
            new(0, TKHDBox.FullBoxFlags.Enabled, 3395928231, 3395928404, 1, 7222215, 0, 0, 0, new int3x3(65536, 0, 0, 0, 65536, 0, 0, 0, 1073741824), 125829120, 52428800);

        public static TKHDBox GetAudio() =>
            new(0, TKHDBox.FullBoxFlags.Enabled, 3395928381, 3395928404, 2, 7222220, 0, 0, 256, new int3x3(65536, 0, 0, 0, 65536, 0, 0, 0, 1073741824), 0, 0);
    }

    public static class MDHD
    {
        public static MDHDBox Get() => new(0, 3351965351, 3351965352, 90000, 498000, 21956);
    }

    public static class HDLR
    {
        public static HDLRBox GetVideo() => new(ISOHandler.VIDE, "VideoHandler");
        public static HDLRBox GetAudio() => new(ISOHandler.SOUN, "Stereo");
    }
}