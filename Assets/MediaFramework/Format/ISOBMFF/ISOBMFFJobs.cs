using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.MediaFramework.Video;

namespace Unity.MediaFramework.Format.ISOBMFF
{
    /// <summary>
    /// Find all parent boxes type and copy them in the buffer
    /// </summary>
    [BurstCompile]
    public unsafe struct ISOExtractAllParentBoxes : IJob
    {
        public BitStream Stream;

        public NativeList<ISOBox> Boxes;
        public NativeList<ulong> ExtendedSizes;

        public void Execute()
        {
            uint seek = 0;
            do
            {
                // We can safely cast seek to int because Remains will never be over int.MaxValue
                Stream.Seek((int)seek);

                var box = Stream.PeekISOBox();
                Boxes.Add(box);

                if (box.size == 1)
                    ExtendedSizes.Add(Stream.PeekUInt64(8));

                seek = box.size;
            }
            while (seek > 1 && Stream.Remains() > seek + 12);
        }
    }

    /// <summary>
    /// Find all boxes type and copy them in the buffer
    /// </summary>
    [BurstCompile]
    public unsafe struct ISOExtractAllBoxes : IJob
    {
        public BitStream Stream;

        public NativeList<ISOBox> Boxes;
        public NativeList<ulong> ExtendedSizes;

        public void Execute()
        {
            uint seek = 0;
            do
            {
                // We can safely cast seek to int because Remains will never be over int.MaxValue
                Stream.Seek((int)seek);

                var box = Stream.PeekISOBox();
                Boxes.Add(box);

                int offset = 8;
                if (box.size == 1)
                {
                    ExtendedSizes.Add(Stream.PeekUInt64(offset));
                    offset += 4;
                }

                // Check first if the current box can be a parent.
                // If yes, let's peek and check if the children type is valid.
                // It is necessary to do that because some childrens are optional so,
                // it is possible that a box can be a parent, but is currently not.
                seek = box.type.CanBeParent() && Stream.HasValidISOBoxType(offset) ? (uint)offset : box.size;
            }
            while (seek > 1 && Stream.Remains() > seek + 12);
        }
    }

    public unsafe struct ISOExtractMOOV : IJob
    {
        public BitStream Stream;

        public NativeReference<MediaAttributes> Attributes;
        public NativeList<VideoTrack> VideoTracks;
        public NativeList<AudioTrack> AudioTracks;

        public void Execute()
        {
            var moovBox = Stream.ReadISOBox();
            if (moovBox.type != ISOBoxType.MOOV)
                return;

            while (Stream.Remains() >= 8)
            {
                var box = Stream.PeekISOBox();
                switch (box.type)
                { 
                    case ISOBoxType.MVHD: ParseMVHD(); break;
                }
            }
        }

        public void ParseMVHD()
        {
            var fullBox = Stream.ReadISOFullBox();
            if (fullBox.version == 1)
            {
                ulong creationTime = Stream.PeekUInt64();
                ulong modificationTime = Stream.PeekUInt64(8);
                uint timescale = Stream.PeekUInt32(16);
                ulong duration = Stream.PeekUInt64(20);

                Stream.Seek(28);
            }
            else
            {
                uint creationTime = Stream.PeekUInt32();
                uint modificationTime = Stream.PeekUInt32(4);
                uint timescale = Stream.PeekUInt32(8);
                uint duration = Stream.PeekUInt32(12);

                Stream.Seek(16);
            }

            //double rate = 

        }
    }

    public struct MVHDBox
    {
        public ulong creationTime;
        public ulong modificationTime;
        public uint timescale;
        public ulong duration;
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
