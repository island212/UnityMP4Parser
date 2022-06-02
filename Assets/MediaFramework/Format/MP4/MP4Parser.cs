using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.MediaFramework.Format.ISOBMFF;
using Unity.MediaFramework.LowLevel.Unsafe;
using Unity.MediaFramework.Video;

namespace Unity.MediaFramework.Format.MP4
{
    // Useful link
    // https://xhelmboyx.tripod.com/formats/mp4-layout.txt
    // https://developer.apple.com/library/archive/documentation/QuickTime/QTFF/QTFFChap1/qtff1.html#//apple_ref/doc/uid/TP40000939-CH203-BBCGDDDF
    // https://b.goeswhere.com/ISO_IEC_14496-12_2015.pdf

    public unsafe static class MP4Parser
    {
        
    }

    public struct MediaAttributes
    {
        public int FrameCount;
        public BigRational Duration;
    }

    public struct AudioTrack
    {
        public int channelCount;
        public int sampleRate;
        public uint codecFourCC;
    }

    public struct VideoTrack
    {
        public int width, height;
        public Rational aspectRatio;
        public uint codecFourCC;
        public uint colorStandard;
    }

    public struct BigRational
    {
        public uint num, denom;
    }

    public struct Rational
    {
        public int num, denom;
    }

    public enum MP4Brand : uint
    {
        AVC1 = 0x61766331,
        ISO2 = 0x69736f32,
        ISO3 = 0x69736f33,
        ISO4 = 0x69736f34,
        ISO5 = 0x69736f35,
        ISO6 = 0x69736f36,
        ISO7 = 0x69736f37,
        ISO8 = 0x69736f38,
        ISO9 = 0x69736f39,
        ISOM = 0x69736f6d,
        MP71 = 0x6d703731,
    }
}
