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
}
