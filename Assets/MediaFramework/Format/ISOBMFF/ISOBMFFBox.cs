using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel.Unsafe;

namespace Unity.MediaFramework.Format.ISOBMFF
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

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public readonly struct FTYPBox
    {
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

        public unsafe static FTYPBox Parse(byte* buffer)
        {

            ISOBrand majorBrand = (ISOBrand)BigEndian.ReadUInt32(buffer + ISOBox.ByteNeeded);
            uint minorVersion = BigEndian.ReadUInt32(buffer + ISOBox.ByteNeeded + 4);

            int brandCount = (int)(BigEndian.ReadUInt32(buffer) - ISOBox.ByteNeeded - 8) / 4;

            // Maximum 6 compatible brands
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
    }

    public readonly struct MVHDBox
    {
        public enum Size : uint
        { 
            Version0 = 108u,
            Version1 = 120u
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

    }

    public readonly struct TKHDBox
    {
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

        public unsafe static TKHDBox Parse(byte* buffer)
        {
            var details = ISOFullBox.GetDetails(buffer + ISOBox.ByteNeeded);

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
    }
}
