using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel.Format.NAL;
using Unity.MediaFramework.LowLevel.Unsafe;

namespace Unity.MediaFramework.LowLevel.Format.ISOBMFF
{
    // Useful link
    // https://b.goeswhere.com/ISO_IEC_14496-12_2015.pdf

    // 4.2 ISO/IEC 14496-12:2015(E)
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public readonly struct ISOBox
    {
        public const int ByteNeeded = 8;

        public readonly uint Size;           // 32 bits
        public readonly ISOBoxType Type;     // 32 bits

        public ISOBox(uint size, ISOBoxType type)
        {
            Size = size;
            Type = type;
        }

        public unsafe static ISOBox Parse(byte* buffer) => new
        (
            BigEndian.ReadUInt32(buffer),
            (ISOBoxType)BigEndian.ReadUInt32(buffer + 4)
        );
    }

    // 4.2 ISO/IEC 14496-12:2015(E)
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public readonly struct ISOFullBox
    {
        public const int ByteNeeded = 12;

        [StructLayout(LayoutKind.Sequential, Size = 8)]
        public readonly struct VersionFlags
        {
            public readonly byte Version;        // 8 bits
            public readonly uint Flags;          // 24 bits

            public VersionFlags(byte version, uint flags)
            {
                Version = version;
                Flags = flags;
            }
        }

        public readonly uint Size;               // 32 bits
        public readonly ISOBoxType Type;         // 32 bits
        public readonly VersionFlags Details;    // 32 bits

        public ISOFullBox(uint size, ISOBoxType type, VersionFlags details)
        {
            Size = size;
            Type = type;
            Details = details;
        }

        public unsafe static ISOFullBox Parse(byte* buffer) => new
        (
            BigEndian.ReadUInt32(buffer),
            (ISOBoxType)BigEndian.ReadUInt32(buffer + 4),
            GetDetails(buffer)
        );

        public unsafe static VersionFlags GetDetails(byte* buffer) => new
        (
            *(buffer + 8),
            // Peek the version and flags then remove the version
            BigEndian.ReadUInt32(buffer + 8) & 0x00FFFFFF
        );

        public unsafe static byte GetVersion(byte* buffer) => *(buffer + 8);

        public unsafe static uint GetFlags(byte* buffer) => BigEndian.ReadUInt32(buffer + 8) & 0x00FFFFFF;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct SampleEntry
    {
        public const int ByteNeeded = 16;

        public readonly ushort DataReferenceIndex;

        public SampleEntry(ushort dataReferenceIndex)
        {
            DataReferenceIndex = dataReferenceIndex;
        }

        public unsafe static SampleEntry Parse(byte* buffer) => new
        (
            GetDataReferenceIndex(buffer)
        );

        public unsafe static ushort GetDataReferenceIndex(byte* buffer) => BigEndian.ReadUInt16(buffer + ISOBox.ByteNeeded + 48);
    }

    /// <summary>
    /// FileTypeBox
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public readonly struct FTYPBox : IEquatable<FTYPBox>
    {
        public unsafe struct Ptr
        { 
            public byte* value;

            public FTYPBox Parse() => FTYPBox.Parse(value);
        }

        public const int MaxCachedBrands = 5;

        public readonly ISOBrand MajorBrand;
        public readonly uint MinorVersion;

        public readonly int BrandCount;
        public readonly ISOBrand Brand0;
        public readonly ISOBrand Brand1;
        public readonly ISOBrand Brand2;
        public readonly ISOBrand Brand3;
        public readonly ISOBrand Brand4;

        public FTYPBox(ISOBrand majorBrand, uint minorVersion, int brandCount,
            ISOBrand brand0, ISOBrand brand1, ISOBrand brand2, ISOBrand brand3, ISOBrand brand4)
        {
            MajorBrand = majorBrand;
            MinorVersion = minorVersion;
            BrandCount = brandCount;
            Brand0 = brand0;
            Brand1 = brand1;
            Brand2 = brand2;
            Brand3 = brand3;
            Brand4 = brand4;
        }

        public int GetSize() => GetSize(BrandCount);

        public unsafe static FTYPBox Parse(byte* buffer)
        {

            ISOBrand majorBrand = (ISOBrand)BigEndian.ReadUInt32(buffer + ISOBox.ByteNeeded);
            uint minorVersion = BigEndian.ReadUInt32(buffer + ISOBox.ByteNeeded + 4);

            int brandCount = (int)(BigEndian.ReadUInt32(buffer) - ISOBox.ByteNeeded - 8) / 4;

            // Support maximum 5 compatible brands
            int count = math.min(brandCount, MaxCachedBrands);
            var brandsPtr = stackalloc ISOBrand[5];
            for (int i = 0; i < count; i++)
            {
                brandsPtr[i] = (ISOBrand)BigEndian.ReadUInt32(buffer + ISOBox.ByteNeeded + 8 + i * 4);
            }

            return new(majorBrand, minorVersion, brandCount,
                brandsPtr[0], brandsPtr[1], brandsPtr[2], brandsPtr[3], brandsPtr[4]);
        }

        public static int GetSize(int brandCount) => ISOBox.ByteNeeded + 8 + brandCount * 4;

        public bool Equals(FTYPBox other) 
            => MajorBrand == other.MajorBrand && MinorVersion == other.MinorVersion && BrandCount == other.BrandCount &&
               Brand0 == other.Brand0 && Brand1 == other.Brand1 && Brand2 == other.Brand2 && Brand3 == other.Brand3 && Brand4 == other.Brand4;
    }

    /// <summary>
    /// MovieHeaderBox
    /// </summary>
    public readonly struct MVHDBox : IEquatable<MVHDBox>
    {
        public enum Size
        {
            Version0 = 108,
            Version1 = 120
        }

        public unsafe struct Ptr
        {
            public byte* value;

            public MVHDBox Parse() => MVHDBox.Parse(value);
        }

        public readonly byte Version;
        public readonly ISODate CreationTime;
        public readonly ISODate ModificationTime;
        public readonly uint Timescale;
        public readonly ulong Duration;
        public readonly FixedPoint1616 Rate;
        public readonly FixedPoint88 Volume;
        public readonly FixedPoint1616Matrix3x3 Matrix;
        public readonly uint NextTrackID;

        public MVHDBox(byte version, ulong creationTime, ulong modificationTime, uint timescale,
            ulong duration, int rate, short volume, int3x3 matrix, uint nextTrackID)
        {
            Version = version;
            CreationTime = new ISODate { value = creationTime };
            ModificationTime = new ISODate { value = modificationTime };
            Timescale = timescale;
            Duration = duration;
            Rate = new FixedPoint1616 { value = rate };
            Volume = new FixedPoint88 { value = volume };
            Matrix = new FixedPoint1616Matrix3x3 { value = matrix };
            NextTrackID = nextTrackID;
        }

        public int GetSize() => GetSize(Version);

        public unsafe static MVHDBox Parse(byte* buffer)
        {
            var version = ISOFullBox.GetVersion(buffer);

            ulong creationTime; ulong modificationTime;
            uint timescale; ulong duration;
            if (version == 1)
            {
                creationTime = BigEndian.ReadUInt64(buffer + ISOFullBox.ByteNeeded);
                modificationTime = BigEndian.ReadUInt64(buffer + ISOFullBox.ByteNeeded + 8);
                timescale = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 16);
                duration = BigEndian.ReadUInt64(buffer + ISOFullBox.ByteNeeded + 20);

                buffer += ISOFullBox.ByteNeeded + 28; // fullBox + creationTime + modificationTime + timescale + duration
            }
            else
            {
                creationTime = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded);
                modificationTime = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 4);
                timescale = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 8);
                duration = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 12);

                buffer += ISOFullBox.ByteNeeded + 16; // fullBox + creationTime + modificationTime + timescale + duration
            }

            var rate = BigEndian.ReadInt32(buffer);
            var volume = BigEndian.ReadInt16(buffer + 4);

            buffer += 4 + 2 + 2 + 4 + 4; // rate + volume + reserved(16) + reserved(32)[2]

            var matrix = new int3x3(
                BigEndian.ReadInt32(buffer), BigEndian.ReadInt32(buffer + 4), BigEndian.ReadInt32(buffer + 8),
                BigEndian.ReadInt32(buffer + 12), BigEndian.ReadInt32(buffer + 16), BigEndian.ReadInt32(buffer + 20),
                BigEndian.ReadInt32(buffer + 24), BigEndian.ReadInt32(buffer + 28), BigEndian.ReadInt32(buffer + 32));

            buffer += FixedPoint1616Matrix3x3.ByteNeeded + 4 * 6; // matrix + reserved(32)[6]

            var nextTrackID = BigEndian.ReadUInt32(buffer);

            return new(version, creationTime, modificationTime, timescale,
                duration, rate, volume, matrix, nextTrackID);
        }

        public static int GetSize(byte version) => version == 1 ? (int)Size.Version1 : (int)Size.Version0;

        public unsafe static uint GetTimeScale(byte version, byte* buffer)
            => BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + (version == 1 ? 16 : 8));

        public unsafe static ulong GetDuration(byte version, byte* buffer)
            => BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + (version == 1 ? 20 : 12));

        public bool Equals(MVHDBox other) 
            => Version == other.Version && CreationTime.value == other.CreationTime.value && ModificationTime.value == other.ModificationTime.value &&
               Timescale == other.Timescale && Duration == other.Duration && Rate.value == other.Rate.value && Volume.value == other.Volume.value &&
               Matrix.value.Equals(other.Matrix.value) && NextTrackID == other.NextTrackID;
    }

    public enum TrackType
    {
        Video = 0,
        Audio = 1,
        Meta = 2,
    }

    /// <summary>
    /// TrackHeaderBox
    /// </summary>
    public readonly struct TKHDBox : IEquatable<TKHDBox>
    {
        public unsafe struct Ptr
        {
            public byte* value;

            public TKHDBox Parse() => TKHDBox.Parse(value);
        }

        public enum Size
        {
            Version0 = 92,
            Version1 = 104
        }

        public enum FullBoxFlags : uint
        {
            Disabled = 0,
            Enabled = 0x1,
            InMovie = 0x2,
            InPreview = 0x4,
            SizeIsAspectRatio = 0x8
        }

        public readonly byte Version;
        public readonly FullBoxFlags Flags;
        public readonly ISODate CreationTime;
        public readonly ISODate ModificationTime;
        public readonly uint TrackID;
        public readonly ulong Duration;
        public readonly short Layer;
        public readonly short AlternateGroup;
        public readonly FixedPoint88 Volume;
        public readonly FixedPoint1616Matrix3x3 Matrix;
        public readonly UFixedPoint1616 Width;
        public readonly UFixedPoint1616 Height;

        public TKHDBox(byte version, FullBoxFlags flags, ulong creationTime, ulong modificationTime, uint trackID, ulong duration, short layer,
            short alternateGroup, short volume, int3x3 matrix, uint width, uint height)
        {
            Version = version;
            Flags = flags;
            CreationTime = new ISODate { value = creationTime };
            ModificationTime = new ISODate { value = modificationTime };
            TrackID = trackID;
            Duration = duration;
            Layer = layer;
            AlternateGroup = alternateGroup;
            Volume = new FixedPoint88 { value = volume };
            Matrix = new FixedPoint1616Matrix3x3 { value = matrix };
            Width = new UFixedPoint1616 { value = width };
            Height = new UFixedPoint1616 { value = height };
        }

        public TrackType GetTrackType()
        {
            if (Volume.value != 0)
                return TrackType.Audio;

            if (Width.value != 0)
                return TrackType.Video;

            return TrackType.Meta;
        }

        public int GetSize() => GetSize(Version);

        public unsafe static TKHDBox Parse(byte* buffer)
        {
            var details = ISOFullBox.GetDetails(buffer);

            ulong creationTime; ulong modificationTime;
            uint trackID; ulong duration;

            if (details.Version == 1)
            {
                creationTime = BigEndian.ReadUInt64(buffer + ISOFullBox.ByteNeeded);
                modificationTime = BigEndian.ReadUInt64(buffer + ISOFullBox.ByteNeeded + 8);
                trackID = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 16);
                // reserved(32)
                duration = BigEndian.ReadUInt64(buffer + ISOFullBox.ByteNeeded + 24);

                buffer += ISOFullBox.ByteNeeded + 40; // fullBox + creationTime + modificationTime + trackID + reserved(32) + duration + reserved(32)[2]
            }
            else
            {
                creationTime = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded);
                modificationTime = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 4);
                trackID = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 8);
                // reserved(32)
                duration = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 16);

                buffer += ISOFullBox.ByteNeeded + 28; // fullBox + creationTime + modificationTime + trackID + reserved(32) + duration + reserved(32)[2]
            }

            var layer = BigEndian.ReadInt16(buffer);
            var alternateGroup = BigEndian.ReadInt16(buffer + 2);
            var volume = BigEndian.ReadInt16(buffer + 4);

            buffer += 2 + 2 + 2 + 2; // layer + alternateGroup + volume + reserved(16)

            var matrix = new int3x3(
                    BigEndian.ReadInt32(buffer), BigEndian.ReadInt32(buffer + 4), BigEndian.ReadInt32(buffer + 8),
                    BigEndian.ReadInt32(buffer + 12), BigEndian.ReadInt32(buffer + 16), BigEndian.ReadInt32(buffer + 20),
                    BigEndian.ReadInt32(buffer + 24), BigEndian.ReadInt32(buffer + 28), BigEndian.ReadInt32(buffer + 32));

            buffer += 4 * 9;

            var width = BigEndian.ReadUInt32(buffer);
            var height = BigEndian.ReadUInt32(buffer + 4);

            return new(details.Version, (FullBoxFlags)details.Flags, creationTime, modificationTime,
                trackID, duration, layer, alternateGroup, volume, matrix, width, height);
        }

        public static int GetSize(byte version) => version == 1 ? (int)Size.Version1 : (int)Size.Version0;

        public bool Equals(TKHDBox other) 
            => Version == other.Version && CreationTime.value == other.CreationTime.value && ModificationTime.value == other.ModificationTime.value &&
               TrackID == other.TrackID && Duration == other.Duration && Layer == other.Layer && AlternateGroup == other.AlternateGroup &&
               Volume.value == other.Volume.value && Matrix.value.Equals(other.Matrix.value) && Width.value == other.Width.value && Height.value == other.Height.value;
    }

    /// <summary>
    /// MediaHeaderBox
    /// </summary>
    public readonly struct MDHDBox : IEquatable<MDHDBox>
    {
        public unsafe struct Ptr
        {
            public byte* value;

            public MDHDBox Parse() => MDHDBox.Parse(value);
        }

        public enum Size
        {
            Version0 = 32,
            Version1 = 44
        }

        public readonly byte Version;
        public readonly ISODate CreationTime;
        public readonly ISODate ModificationTime;
        public readonly uint Timescale;
        public readonly ulong Duration;
        public readonly ISOLanguage Language;

        public MDHDBox(byte version, ulong creationTime, ulong modificationTime,
            uint timescale, ulong duration, ushort language)
        {
            Version = version;
            CreationTime = new ISODate { value = creationTime };
            ModificationTime = new ISODate { value = modificationTime };
            Timescale = timescale;
            Duration = duration;
            Language = new ISOLanguage { value = language };
        }

        public bool Equals(MDHDBox other)
        {
            return Version == other.Version && CreationTime.value == other.CreationTime.value && ModificationTime.value == other.ModificationTime.value &&
                Timescale == other.Timescale && Duration == other.Duration && Language.value == other.Language.value;
        }

        public unsafe static MDHDBox Parse(byte* buffer)
        {
            var version = ISOFullBox.GetVersion(buffer);

            ulong creationTime; ulong modificationTime;
            uint timescale; ulong duration;
            if (version == 1)
            {
                creationTime = BigEndian.ReadUInt64(buffer + ISOFullBox.ByteNeeded);
                modificationTime = BigEndian.ReadUInt64(buffer + ISOFullBox.ByteNeeded + 8);
                timescale = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 16);
                duration = BigEndian.ReadUInt64(buffer + ISOFullBox.ByteNeeded + 20);

                buffer += ISOFullBox.ByteNeeded + 28; // fullBox + creationTime + modificationTime + timescale + duration
            }
            else
            {
                creationTime = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded);
                modificationTime = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 4);
                timescale = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 8);
                duration = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 12);

                buffer += ISOFullBox.ByteNeeded + 16; // fullBox + creationTime + modificationTime + timescale + duration
            }

            var language = BigEndian.ReadUInt16(buffer);

            return new(version, creationTime, modificationTime, timescale, duration, language);
        }

        public int GetSize() => GetSize(Version);

        public static int GetSize(byte version) => version == 1 ? (int)Size.Version1 : (int)Size.Version0;
    }

    public readonly struct HDLRBox : IEquatable<HDLRBox>
    {
        public unsafe struct Ptr
        {
            public byte* value;

            public HDLRBox Parse() => HDLRBox.Parse(value);
        }

        public readonly ISOHandler Handler;
        public readonly FixedString64Bytes Name;

        public HDLRBox(ISOHandler handler, in FixedString64Bytes name)
        {
            Handler = handler;
            Name = name;
        }

        public unsafe static HDLRBox Parse(byte* buffer)
        {
            var fullBox = ISOFullBox.Parse(buffer);
            // reserved(32)
            var handler = (ISOHandler)BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 4);
            // reserved(32)[3]

            buffer += ISOFullBox.ByteNeeded + 20;

            var name = new FixedString64Bytes();
            name.Append(buffer, math.min(FixedString64Bytes.UTF8MaxLengthInBytes, (int)fullBox.Size - ISOFullBox.ByteNeeded - 20 - 1));

            return new(handler, name);
        }

        public int GetSize() => GetSize(Name.Length);

        public static int GetSize(int nameLength) 
            => ISOFullBox.ByteNeeded + 20 + nameLength + 1; // + 1 for the NULL terminate

        public unsafe static ISOHandler GetHandlerType(byte* buffer)
            => (ISOHandler)BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded + 4);

        public bool Equals(HDLRBox other)
            => Handler == other.Handler && Name == other.Name;
    }

    /// <summary>
    /// SampleDescriptionBox
    /// </summary>
    public readonly struct STSDBox
    {
        public unsafe struct Ptr
        {
            public byte* value;

            public VisualSampleEntry ParseVideo() => VisualSampleEntry.Parse(value);

            public AudioSampleEntry ParseAudio() => AudioSampleEntry.Parse(value);
        }
    }

    public unsafe readonly struct VisualSampleEntry
    {
        public enum EntryType
        { 
            AVCC = 0x61766343, // avcC
            BTRT = 0x62747274,
            CLAP = 0x636c6170,
            COLR = 0x636f6c72,
            PASP = 0x70617370,
        }

        public const int ByteNeeded = SampleEntry.ByteNeeded + 70;

        public readonly VideoCodec Codec;
        public readonly ushort Width, Height;
        public readonly UFixedPoint1616 HorizResolution;
        public readonly UFixedPoint1616 VertResolution;
        public readonly ushort FrameCount;
        public readonly FixedString64Bytes CompressorName;

        public readonly byte* BitRateBox;
        public readonly byte* DecoderConfigurationBox;
        public readonly byte* CleanApertureBox;
        public readonly byte* PixelAspectRatioBox;
        public readonly byte* ColourInformationBox;

        public VisualSampleEntry(VideoCodec codec, ushort width, ushort height, uint horizResolution, uint vertResolution, ushort frameCount, 
            in FixedString64Bytes compressorName, byte* bitRateBox, byte* decoderConfigurationBox, byte* cleanApertureBox, byte* pixelAspectRatioBox, byte* colourInformationBox)
        {
            Codec = codec;
            Width = width;
            Height = height;
            HorizResolution = new UFixedPoint1616 { value = horizResolution };
            VertResolution = new UFixedPoint1616 { value = vertResolution };
            FrameCount = frameCount;
            CompressorName = compressorName;
            BitRateBox = bitRateBox;
            DecoderConfigurationBox = decoderConfigurationBox;
            CleanApertureBox = cleanApertureBox;
            PixelAspectRatioBox = pixelAspectRatioBox;
            ColourInformationBox = colourInformationBox;
        }

        public static VisualSampleEntry Parse(byte* buffer)
        {
            var entryCount = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded);

            var box = ISOBox.Parse(buffer + ISOFullBox.ByteNeeded + 4);
            var codec = (VideoCodec)box.Type;

            buffer += ISOFullBox.ByteNeeded + 4 + SampleEntry.ByteNeeded + 16; // ISOFullBox + EntryCount + SampleEntry + reserved(32)[4]

            var width = BigEndian.ReadUInt16(buffer);
            var height = BigEndian.ReadUInt16(buffer + 2);
            var horizresolution = BigEndian.ReadUInt32(buffer + 4);
            var vertresolution = BigEndian.ReadUInt32(buffer + 8);
            // reserved(32)
            var frameCount = BigEndian.ReadUInt16(buffer + 16);

            buffer += 18;

            var compressorNameLength = *buffer;
            var compressorName = new FixedString64Bytes();
            compressorName.Append(buffer + 1, math.min(FixedString64Bytes.UTF8MaxLengthInBytes, compressorNameLength));

            buffer += 36; // string[32] + depth(16) + reserved(16)

            int size = (int)box.Size - ByteNeeded;
            byte* bitRateBox = null, decoderConfigurationBox = null, cleanApertureBox = null, pixelAspectRatioBox = null, colourInformationBox = null;
            while (size >= ISOBox.ByteNeeded)
            {
                var entryBox = ISOBox.Parse(buffer);
                switch ((EntryType)entryBox.Type)
                {
                    case EntryType.AVCC:
                        decoderConfigurationBox = buffer;
                        break;
                    case EntryType.BTRT:
                        bitRateBox = buffer;
                        break;
                    case EntryType.CLAP:
                        cleanApertureBox = buffer;
                        break;
                    case EntryType.PASP:
                        pixelAspectRatioBox = buffer;
                        break;
                    case EntryType.COLR:
                        colourInformationBox = buffer;
                        break;
                }

                size -= (int)entryBox.Size;
                buffer += (int)entryBox.Size;
            }

            return new(codec, width, height, horizresolution, vertresolution, frameCount, compressorName,
                bitRateBox, decoderConfigurationBox, cleanApertureBox, pixelAspectRatioBox, colourInformationBox);
        }
    }

    /// <summary>
    /// AVCDecoderConfigurationRecord (avcC) ISO/IEC 14496-15
    /// </summary>
    public unsafe readonly struct AVCDecoderConfigurationRecord
    {
        public readonly byte ConfigurationVersion;
        public readonly byte AVCProfileIndication;
        public readonly byte ProfileCompatibility;
        public readonly byte AVCLevelIndication;
        public readonly byte LengthSizeMinusOne;

        public readonly byte* SPS;
        public readonly byte* PPS;
        public readonly byte* AVCProfileIndicationMeta;

        public AVCDecoderConfigurationRecord(byte configurationVersion, byte avcProfileIndication, byte profileCompatibility, 
            byte avcLevelIndication, byte lengthSizeMinusOne, byte* sps, byte* pps, byte* avcProfileIndicationMeta)
        {
            ConfigurationVersion = configurationVersion;
            AVCProfileIndication = avcProfileIndication;
            ProfileCompatibility = profileCompatibility;
            AVCLevelIndication = avcLevelIndication;
            LengthSizeMinusOne = lengthSizeMinusOne;
            SPS = sps;
            PPS = pps;
            AVCProfileIndicationMeta = avcProfileIndicationMeta;
        }

        public static AVCDecoderConfigurationRecord Parse(byte* buffer)
        {
            var box = ISOBox.Parse(buffer);

            int pos = ISOBox.ByteNeeded;

            var configurationVersion = buffer[pos++];
            var avcProfileIndication = buffer[pos++];
            var profileCompatibility = buffer[pos++];
            var avcLevelIndication = buffer[pos++];
            var lengthSizeMinusOne = (byte)(buffer[pos++] & 0b00000011);

            var spsPtr = buffer + pos;
            var numSPS = buffer[pos++] & 0b00011111;

            for (int i = 0; i < numSPS; i++)
            {
                var spsLength = BigEndian.ReadUInt16(buffer + pos);
                pos += 2 + spsLength;
            }

            var ppsPtr = buffer + pos;
            var numPPS = buffer[pos++] & 0b00011111;

            for (int i = 0; i < numPPS; i++)
            {
                var ppsLength = BigEndian.ReadUInt16(buffer + pos);
                pos += 2 + ppsLength;
            }

            byte* avcProfileIndicationMetaPtr = box.Size - pos >= 4 && 
                (avcProfileIndication == 100 || avcProfileIndication == 110 ||
                avcProfileIndication == 122 || avcProfileIndication == 144) 
                ? buffer + pos : null;

            return new(configurationVersion, avcProfileIndication, profileCompatibility, avcLevelIndication, lengthSizeMinusOne, spsPtr, ppsPtr, avcProfileIndicationMetaPtr);
        }
    }

    /// <summary>
    /// AVCProfileIndicationMeta inside AVCDecoderConfigurationRecord (avcC) ISO/IEC 14496-15
    /// If the AvcProfileIndication is 100, 110, 122 or 144 additionnal metadata can be provided
    /// </summary>
    public unsafe readonly struct AVCProfileIndicationMeta
    {
        public readonly ChromaSubsampling ChromaFormat;
        public readonly byte BitDepthLumaMinus8;
        public readonly byte BitDepthChromaMinus8;
        public readonly byte* SPSExt;

        public AVCProfileIndicationMeta(byte chromaFormat, byte bitDepthLumaMinus8, byte bitDepthChromaMinus8, byte* spsExt)
        {
            ChromaFormat = (ChromaSubsampling)chromaFormat;
            BitDepthLumaMinus8 = bitDepthLumaMinus8;
            BitDepthChromaMinus8 = bitDepthChromaMinus8;
            SPSExt = spsExt;
        }

        public static AVCProfileIndicationMeta Parse(byte* buffer)
        {
            var chromaFormat = (byte)(buffer[0] & 0b00000011);
            var bitDepthLumaMinus8 = (byte)(buffer[1] & 0b00000111);
            var bitDepthChromaMinus8 = (byte)(buffer[2] & 0b00000111);
            var spsExt = buffer + 3;

            return new(chromaFormat, bitDepthLumaMinus8, bitDepthChromaMinus8, spsExt);
        }
    }

    public unsafe readonly struct AudioSampleEntry
    {
        public enum EntryType
        {
            SRAT = 0x73726174,
            CHNL = 0x63686e6c,
        }

        public const int ByteNeeded = SampleEntry.ByteNeeded + 20;

        public readonly AudioCodec Codec;
        public readonly ushort ChannelCount;
        public readonly ushort SampleSize;
        public readonly uint SampleRate;

        public readonly byte* ChannelLayout;

        public AudioSampleEntry(AudioCodec codec, ushort channelCount, ushort sampleSize, uint sampleRate, byte* channelLayout)
        {
            Codec = codec;
            ChannelCount = channelCount;
            SampleSize = sampleSize;
            SampleRate = sampleRate;
            ChannelLayout = channelLayout;
        }

        public static AudioSampleEntry Parse(byte* buffer)
        {
            var entryCount = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded);

            var box = ISOBox.Parse(buffer + ISOFullBox.ByteNeeded + 4);
            var codec = (AudioCodec)box.Type;

            buffer += ISOFullBox.ByteNeeded + 4 + SampleEntry.ByteNeeded; // ISOFullBox + EntryCount + SampleEntry

            var entryVersion = BigEndian.ReadUInt16(buffer);
            var channelCount = BigEndian.ReadUInt16(buffer + 8);
            var sampleSize = BigEndian.ReadUInt16(buffer + 10);
            var sampleRate = (uint)UFixedPoint1616.ConvertDouble(BigEndian.ReadUInt32(buffer + 16));

            buffer += 20;

            int size = (int)box.Size - ByteNeeded;
            byte* channelLayout = null;
            while (size >= ISOBox.ByteNeeded)
            {
                var entryBox = ISOBox.Parse(buffer);
                if (entryBox.Size == 0) // Prevent infinite loop
                    return default;

                switch ((EntryType)entryBox.Type)
                {
                    case EntryType.SRAT:
                        if (entryVersion == 1)
                        {
                            sampleRate = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded);
                        }
                        break;
                    case EntryType.CHNL:
                        channelLayout = buffer;
                        break;
                }

                size -= (int)entryBox.Size;
                buffer += (int)entryBox.Size;
            }

            return new(codec, channelCount, sampleSize, sampleRate, channelLayout);      
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public unsafe readonly struct STTSBox
    {
        public unsafe struct Ptr
        {
            public byte* value;

            public STTSBox Parse() => STTSBox.Parse(value);
        }

        public const int SamplesOffset = ISOFullBox.ByteNeeded + 4;

        public readonly uint SampleCount;
        public readonly uint EntryCount;
        public readonly SampleGoup* SamplesTable;

        public STTSBox(uint sampleCount, uint entryCount, SampleGoup* samplesTable)
        {
            SampleCount = sampleCount;
            EntryCount = entryCount;
            SamplesTable = samplesTable;
        }

        public static STTSBox Parse(byte* buffer)
        {
            var entryCount = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded);

            buffer += ISOFullBox.ByteNeeded + 4;

            var sampleCount = 0u;
            var samplePtr = (ulong*)buffer;
            for (int i = 0; i < entryCount; i++)
            {
                // Reading two UInt32 is 2x time faster than one at a time
                *(samplePtr + i) = BigEndian.Read2UInt32(buffer + i * 8);
                sampleCount += *(uint*)(samplePtr + i);
            }

            return new(sampleCount, entryCount, (SampleGoup*)buffer);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public unsafe readonly struct STSSBox
    {
        public unsafe struct Ptr
        {
            public byte* value;

            public STSSBox Parse() => STSSBox.Parse(value);
        }

        public const int SamplesOffset = ISOFullBox.ByteNeeded + 4;

        public readonly uint EntryCount;
        public readonly uint* SyncSamplesTable;

        public STSSBox(uint entryCount, uint* syncSamplesTable)
        {
            EntryCount = entryCount;
            SyncSamplesTable = syncSamplesTable;
        }

        public static STSSBox Parse(byte* buffer)
        {
            var entryCount = BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded);

            buffer += ISOFullBox.ByteNeeded + 4;

            var samplePtr = (uint*)buffer;
            for (int i = 0; i < entryCount; i++)
            {
                *(samplePtr + i) = BigEndian.ReadUInt32(buffer + i * 4);
            }

            return new(entryCount, (uint*)buffer);
        }
    }

    ///// <summary>
    ///// TimeToSampleBox (STTS) 
    ///// </summary>
    //public unsafe readonly struct TimeToSampleRawBox
    //{
    //    public const int SampleSize = 8;
    //    public const int SamplesOffset = ISOFullBox.ByteNeeded + 4;

    //    public readonly uint EntryCount;
    //    public readonly byte* Samples;

    //    public TimeToSampleRawBox(uint entryCount, byte* samples)
    //    {
    //        EntryCount = entryCount;
    //        Samples = samples;
    //    }

    //    public static unsafe byte* Allocate(uint entryCount)
    //        => (byte*)UnsafeUtility.Malloc(entryCount * 8, 8, Allocator.Persistent);

    //    public static unsafe uint GetEntryCount(byte* buffer) 
    //        => BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded);
    //}

    ///// <summary>
    ///// SyncSampleBox (STSS)
    ///// </summary>
    //public unsafe readonly struct SyncSampleRawBox
    //{
    //    public const int SamplesOffset = ISOFullBox.ByteNeeded + 4;

    //    public readonly uint EntryCount;
    //    public readonly byte* SampleNumbers;

    //    public SyncSampleRawBox(uint entryCount, byte* sampleNumbers)
    //    {
    //        EntryCount = entryCount;
    //        SampleNumbers = sampleNumbers;
    //    }

    //    public static unsafe byte* Allocate(uint entryCount)
    //        => (byte*)UnsafeUtility.Malloc(entryCount * 4, 4, Allocator.Persistent);

    //    public static unsafe uint GetEntryCount(byte* buffer)
    //        => BigEndian.ReadUInt32(buffer + ISOFullBox.ByteNeeded);
    //}
}
