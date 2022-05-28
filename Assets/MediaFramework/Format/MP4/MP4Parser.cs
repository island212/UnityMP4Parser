using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.MediaFramework.Video;

namespace Unity.MediaFramework.Format.MP4
{
    // Useful link
    // https://xhelmboyx.tripod.com/formats/mp4-layout.txt
    // https://developer.apple.com/library/archive/documentation/QuickTime/QTFF/QTFFChap1/qtff1.html#//apple_ref/doc/uid/TP40000939-CH203-BBCGDDDF
    // https://b.goeswhere.com/ISO_IEC_14496-12_2015.pdf

    public unsafe static class MP4Parser
    {
        /// <summary>
        /// Find all boxes type and copy them in the buffer
        /// </summary>
        [BurstCompile]
        public unsafe struct FindBoxes : IJob
        {
            [ReadOnly] public NativeArray<uint> SearchedBoxes;

            public BitStream Stream;
            public NativeList<byte> FoundBoxes;

            public void Execute()
            {
                int foundBoxes = 0;
                while (!Stream.EndOfStream() && foundBoxes < SearchedBoxes.Length)
                {
                    var box = Stream.PeekMP4Box();
                    ulong size = box.GetExtendedSize(Stream);

                    if (SearchedBoxes.Contains(box.type))
                    {
                        Stream.CopyInto(ref FoundBoxes, size);
                        foundBoxes++;
                    }

                    // We need to know what is the current box size.
                    // The size depends on if it is extended or not.
                    int offset = box.GetBoxSize();

                    // Check first if the current box can be a parent.
                    // If yes, let's peek and check if the children type is valid.
                    // It is necessary to do that because some childrens are optional so,
                    // it is possible that a box can be a parent, but is currently not.
                    Stream.Seek(box.CanBeParent() && Stream.HasValidMP4BoxType(offset) ? (ulong)offset : size);
                }
            }
        }

        [BurstCompile]
        public unsafe struct ParseFTYP : IJob
        {
            public BitStream Stream;
            public NativeReference<FTYPBox> Result;

            public void Execute()
            {
                if (Stream.FindMP4BoxType((uint)MP4BoxType.FTYP))
                {
                    var box = Stream.PeekMP4Box();

                    FTYPBox result;
                    result.majorBrand = Stream.PeekUInt32(8);
                    result.minorVersion = Stream.PeekUInt32(12);

                    result.compatibleBrands = 0;
                    for (int offset = 16; offset < box.size; offset+=4)
                        result.compatibleBrands |= MP4Tools.BrandToFlag(Stream.PeekUInt32(offset));

                    Result.Value = result;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct FTYPBox
    {
        public uint majorBrand;
        public uint minorVersion;
        public uint compatibleBrands;

        public bool IsValid()
        {
            return MP4Tools.BrandToFlag(majorBrand) != 0;
        }
    }

    // 4.2 ISO/IEC 14496-12:2015(E)
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public unsafe struct MP4Box
    {
        public uint size;       // 32 bits
        public uint type;       // 32 bits

        public MP4Box(in BitStream stream)
        {
            size = stream.PeekUInt32();
            type = stream.PeekUInt32(4);
        }

        public MP4Box(in BitStream stream, int offset)
        {
            size = stream.PeekUInt32(offset);
            type = stream.PeekUInt32(offset + 4);
        }

        public ulong GetExtendedSize(in BitStream stream)
        { 
            return size > 1 ? size : size == 1 ? stream.PeekUInt64(8) : stream.length;
        }

        public int GetBoxSize()
        { 
            return size != 1 ? 8 : 16;
        }

        public bool HasValidType()
        {
            return ((MP4BoxType)type).IsValid();
        }

        public bool CanBeParent()
        {
            return ((MP4BoxType)type).CanBeParent();
        }
    }

    // 4.2 ISO/IEC 14496-12:2015(E)
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public unsafe struct MP4FullBox
    {
        public uint size;       // 32 bits
        public uint type;       // 32 bits
        public byte version;    // 8 bits
        public uint flags;      // 24 bits

        public MP4FullBox(in BitStream stream)
        {
            size = stream.PeekUInt32();
            type = stream.PeekUInt32(4);

            version = stream.PeekByte(8);
            // Peek the version and flags then remove the version
            flags = stream.PeekUInt32(8) & 0x00FFFFFF;
        }

        public ulong GetExtendedSize(in BitStream stream)
        {
            return size > 1 ? size : size == 1 ? stream.PeekUInt64(12) : stream.length;
        }

        public int GetBoxSize()
        {
            return size != 1 ? 12 : 20;
        }
    }

    public unsafe static class MP4Tools
    {
        public const int MP4BrandCount = 11;

        public static bool IsBoxTypeValid(uint type) => ((MP4BoxType)type).IsValid();

        public static bool CanBoxTypeBeParent(uint type) => ((MP4BoxType)type).CanBeParent();

        public static uint BrandToFlag(MP4Brand brand) => brand switch
        {
            MP4Brand.AVC1 => 1u << 00,
            MP4Brand.ISO2 => 1u << 01,
            MP4Brand.ISO3 => 1u << 02,
            MP4Brand.ISO4 => 1u << 03,
            MP4Brand.ISO5 => 1u << 04,
            MP4Brand.ISO6 => 1u << 05,
            MP4Brand.ISO7 => 1u << 06,
            MP4Brand.ISO8 => 1u << 07,
            MP4Brand.ISO9 => 1u << 08,
            MP4Brand.ISOM => 1u << 09,
            MP4Brand.MP71 => 1u << 10,
            _ => 0,
        };

        public static uint BrandToFlag(uint brand) => BrandToFlag((MP4Brand)brand);

        public static uint[] FlagToBrand(uint brand)
        {
            using (var found = new NativeList<uint>(MP4BrandCount, Allocator.Temp))
            {
                for (int i = 0; i < MP4BrandCount; i++)
                {
                    switch (brand & 1u << i)
                    {
                        case 1u << 00: found.Add((uint)MP4Brand.AVC1); break;
                        case 1u << 01: found.Add((uint)MP4Brand.ISO2); break;
                        case 1u << 02: found.Add((uint)MP4Brand.ISO3); break;
                        case 1u << 03: found.Add((uint)MP4Brand.ISO4); break;
                        case 1u << 04: found.Add((uint)MP4Brand.ISO5); break;
                        case 1u << 05: found.Add((uint)MP4Brand.ISO6); break;
                        case 1u << 06: found.Add((uint)MP4Brand.ISO7); break;
                        case 1u << 07: found.Add((uint)MP4Brand.ISO8); break;
                        case 1u << 08: found.Add((uint)MP4Brand.ISO9); break;
                        case 1u << 09: found.Add((uint)MP4Brand.ISOM); break;
                        case 1u << 10: found.Add((uint)MP4Brand.MP71); break;
                    }
                }
                return found.ToArray();
            }
        }
    }

    public unsafe static class MP4Extensions
    {
        public static MP4Box PeekMP4Box(this BitStream stream) => new MP4Box(stream);

        public static MP4Box PeekMP4Box(this BitStream stream, int offset) => new MP4Box(stream, offset);

        public static MP4FullBox PeekMP4FullBox(this BitStream stream) => new MP4FullBox(stream);

        public static MP4Box ReadMP4Box(this BitStream stream)
        {
            var box = new MP4Box(stream);
            stream.Seek(8);
            return box;
        }

        public static MP4FullBox ReadMP4FullBox(this BitStream stream) 
        {
            var box = new MP4FullBox(stream);
            stream.Seek(12);
            return box; 
        }

        public static bool FindMP4BoxType(this BitStream stream, uint type)
        {
            var box = stream.PeekMP4Box();
            while ((MP4BoxType)box.type != MP4BoxType.FTYP && !stream.EndOfStream())
            {
                ulong size = box.GetExtendedSize(stream);
                stream.Seek(size);

                box = stream.PeekMP4Box();
            }

            return box.type == type;
        }

        public static bool HasValidMP4BoxType(this BitStream stream, int offset) => ((MP4BoxType)(stream.PeekUInt32(offset + 4))).IsValid();

        public static bool CanBeParent(this MP4BoxType type)
        {
            switch (type)
            {
                case MP4BoxType.MOOV:
                case MP4BoxType.TRAK:
                case MP4BoxType.EDTS:
                case MP4BoxType.MDIA:
                case MP4BoxType.MINF:
                case MP4BoxType.DINF:
                case MP4BoxType.STBL:
                case MP4BoxType.MVEX:
                case MP4BoxType.MOOF:
                case MP4BoxType.TRAF:
                case MP4BoxType.MFRA:
                case MP4BoxType.SKIP:
                case MP4BoxType.META:
                case MP4BoxType.IPRO:
                case MP4BoxType.SINF:
                case MP4BoxType.FIIN:
                case MP4BoxType.PAEN:
                case MP4BoxType.MECO:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsValid(this MP4BoxType type)
        {
            switch (type)
            {
                case MP4BoxType.BXML:
                case MP4BoxType.CO64:
                case MP4BoxType.CPRT:
                case MP4BoxType.CSLG:
                case MP4BoxType.CTTS:
                case MP4BoxType.DINF:
                case MP4BoxType.DREF:
                case MP4BoxType.EDTS:
                case MP4BoxType.ELNG:
                case MP4BoxType.ELST:
                case MP4BoxType.FECR:
                case MP4BoxType.FIIN:
                case MP4BoxType.FIRE:
                case MP4BoxType.FPAR:
                case MP4BoxType.FREE:
                case MP4BoxType.FRMA:
                case MP4BoxType.FTYP:
                case MP4BoxType.GITN:
                case MP4BoxType.HDLR:
                case MP4BoxType.HMHD:
                case MP4BoxType.IDAT:
                case MP4BoxType.IINF:
                case MP4BoxType.ILOC:
                case MP4BoxType.IPRO:
                case MP4BoxType.IREF:
                case MP4BoxType.LEVA:
                case MP4BoxType.MDAT:
                case MP4BoxType.MDHD:
                case MP4BoxType.MDIA:
                case MP4BoxType.MECO:
                case MP4BoxType.MEHD:
                case MP4BoxType.MERE:
                case MP4BoxType.META:
                case MP4BoxType.MFHD:
                case MP4BoxType.MFRA:
                case MP4BoxType.MFRO:
                case MP4BoxType.MINF:
                case MP4BoxType.MOOF:
                case MP4BoxType.MOOV:
                case MP4BoxType.MVEX:
                case MP4BoxType.MVHD:
                case MP4BoxType.NMHD:
                case MP4BoxType.PADB:
                case MP4BoxType.PAEN:
                case MP4BoxType.PDIN:
                case MP4BoxType.PITM:
                case MP4BoxType.PRFT:
                case MP4BoxType.SAIO:
                case MP4BoxType.SAIZ:
                case MP4BoxType.SBGP:
                case MP4BoxType.SCHI:
                case MP4BoxType.SCHM:
                case MP4BoxType.SDTP:
                case MP4BoxType.SEGR:
                case MP4BoxType.SGPD:
                case MP4BoxType.SIDX:
                case MP4BoxType.SINF:
                case MP4BoxType.SKIP:
                case MP4BoxType.SMHD:
                case MP4BoxType.SSIX:
                case MP4BoxType.STBL:
                case MP4BoxType.STCO:
                case MP4BoxType.STDP:
                case MP4BoxType.STHD:
                case MP4BoxType.STRD:
                case MP4BoxType.STRI:
                case MP4BoxType.STRK:
                case MP4BoxType.STSC:
                case MP4BoxType.STSD:
                case MP4BoxType.STSH:
                case MP4BoxType.STSS:
                case MP4BoxType.STSZ:
                case MP4BoxType.STTS:
                case MP4BoxType.STYP:
                case MP4BoxType.STZ2:
                case MP4BoxType.SUBS:
                case MP4BoxType.TFDT:
                case MP4BoxType.TFHD:
                case MP4BoxType.TFRA:
                case MP4BoxType.TKHD:
                case MP4BoxType.TRAF:
                case MP4BoxType.TRAK:
                case MP4BoxType.TREF:
                case MP4BoxType.TREX:
                case MP4BoxType.TRGR:
                case MP4BoxType.TRUN:
                case MP4BoxType.TSEL:
                case MP4BoxType.UDTA:
                case MP4BoxType.UUID:
                case MP4BoxType.VMHD:
                case MP4BoxType.WIDE:
                case MP4BoxType.XML_:
                case MP4BoxType.CHAP:
                case MP4BoxType.CLIP:
                case MP4BoxType.CRGN:
                case MP4BoxType.CTAB:
                case MP4BoxType.IMAP:
                case MP4BoxType.KMAT:
                case MP4BoxType.LOAD:
                case MP4BoxType.MATT:
                case MP4BoxType.PNOT:
                case MP4BoxType.SCPT:
                case MP4BoxType.SYNC:
                case MP4BoxType.TMCD:
                    return true;
                default:
                    return false;
            }
        }
    }

    // Useful to convert FourCC to HEX
    // https://www.branah.com/ascii-converter

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

    /// <summary>
    /// FourCC boxes. The value is the FourCC in decimal
    /// </summary>
    public enum MP4BoxType : uint
    {
        BXML = 0X62786D6C,
        CO64 = 0x636f3634,
        CPRT = 0X63707274,
        CSLG = 0x63736c67,
        CTTS = 0x63747473,
        DINF = 0x64696e66,
        DREF = 0x64726566,
        EDTS = 0x65647473,
        ELNG = 0x656c6e67,
        ELST = 0x656c7374,
        FECR = 0X66656372,
        FIIN = 0X6669696E,
        FIRE = 0X66697265,
        FPAR = 0X66706172,
        FREE = 0x66726565,
        FRMA = 0X66726D61,
        FTYP = 0x66747970,
        GITN = 0X6769746E,
        HDLR = 0x68646c72,
        HMHD = 0x686d6864,
        IDAT = 0X69646174,
        IINF = 0X69696E66,
        ILOC = 0X696C6F63,
        IPRO = 0X6970726F,
        IREF = 0X69726566,
        LEVA = 0x6c657661,
        MDAT = 0x6d646174,
        MDHD = 0x6d646864,
        MDIA = 0x6d646961,
        MECO = 0X6D65636F,
        MEHD = 0x6d656864,
        MERE = 0X6D657265,
        META = 0x6d657461,
        MFHD = 0X6D666864,
        MFRA = 0X6D667261,
        MFRO = 0X6D66726F,
        MINF = 0x6d696e66,
        MOOF = 0X6D6F6F66,
        MOOV = 0x6d6f6f76,
        MVEX = 0x6d766578,
        MVHD = 0x6d766864,
        NMHD = 0x6e6d6864,
        PADB = 0x70616462,
        PAEN = 0X7061656E,
        PDIN = 0x7064696e,
        PITM = 0X7069746D,
        PRFT = 0X70726674,
        SAIO = 0x7361696f,
        SAIZ = 0x7361697a,
        SBGP = 0x73626770,
        SCHI = 0X73636869,
        SCHM = 0X7363686D,
        SDTP = 0x73647470,
        SEGR = 0X73656772,
        SGPD = 0x73677064,
        SIDX = 0X73696478,
        SINF = 0X73696E66,
        SKIP = 0x736b6970,
        SMHD = 0x736d6864,
        SSIX = 0X73736978,
        STBL = 0x7374626c,
        STCO = 0x7374636f,
        STDP = 0x73746470,
        STHD = 0x73746864,
        STRD = 0X73747264,
        STRI = 0X73747269,
        STRK = 0X7374726B,
        STSC = 0x73747363,
        STSD = 0x73747364,
        STSH = 0x73747368,
        STSS = 0x73747373,
        STSZ = 0x7374737a,
        STTS = 0x73747473,
        STYP = 0X73747970,
        STZ2 = 0x73747a32,
        SUBS = 0x73756273,
        TFDT = 0X74666474,
        TFHD = 0X74666864,
        TFRA = 0X74667261,
        TKHD = 0x746b6864,
        TRAF = 0X74726166,
        TRAK = 0x7472616b,
        TREF = 0x74726566,
        TREX = 0x74726578,
        TRGR = 0x74726772,
        TRUN = 0X7472756E,
        TSEL = 0X7473656C,
        UDTA = 0x75647461,
        UUID = 0x75756964,
        VMHD = 0x766d6864,
        WIDE = 0x77696465,
        XML_ = 0X786D6C20,

        // 6.2.1 ISO/IEC 14496-12:2015
        // Should not be use anymore but was in previous implementation
        CHAP = 0x63686170,
        CLIP = 0x636c6970,
        CRGN = 0x6372676e,
        CTAB = 0x63746162,
        IMAP = 0x696d6170,
        KMAT = 0x6b6d6174,
        LOAD = 0x6c6f6164,
        MATT = 0x6d617474,
        PNOT = 0x706e6f74,
        SCPT = 0x73637074,
        SYNC = 0x73796e63,
        TMCD = 0x746d6364,
    }

    public struct UUIDType : IEquatable<UUIDType>
    {
        public byte data0000;
        public byte data0001;
        public byte data0002;
        public byte data0003;
        public byte data0004;
        public byte data0005;
        public byte data0006;
        public byte data0007;
        public byte data0008;
        public byte data0009;
        public byte data0010;
        public byte data0011;
        public byte data0012;
        public byte data0013;
        public byte data0014;
        public byte data0015;

        public bool Equals(UUIDType other) => 
            data0000 == other.data0000 && data0001 == other.data0001 && 
            data0002 == other.data0002 && data0003 == other.data0003 &&
            data0004 == other.data0004 && data0005 == other.data0005 && 
            data0006 == other.data0006 && data0007 == other.data0007 &&
            data0008 == other.data0008 && data0009 == other.data0009 && 
            data0010 == other.data0010 && data0011 == other.data0011 &&
            data0012 == other.data0012 && data0013 == other.data0013 && 
            data0014 == other.data0014 && data0015 == other.data0015;
    }
}
