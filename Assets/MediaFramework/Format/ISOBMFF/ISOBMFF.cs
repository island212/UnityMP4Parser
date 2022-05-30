using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.MediaFramework.Video;

namespace Unity.MediaFramework.Format.ISOBMFF
{
    // Useful link
    // https://b.goeswhere.com/ISO_IEC_14496-12_2015.pdf

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct FTYPBox
    {
        public uint majorBrand;
        public uint minorVersion;
        public uint compatibleBrands;
    }

    // 4.2 ISO/IEC 14496-12:2015(E)
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public unsafe struct ISOBox
    {
        public uint size;           // 32 bits
        public ISOBoxType type;     // 32 bits

        public ISOBox(in BitStream stream)
        {
            size = stream.PeekUInt32();
            type = (ISOBoxType)stream.PeekUInt32(4);
        }

        public ISOBox(in BitStream stream, int offset)
        {
            size = stream.PeekUInt32(offset);
            type = (ISOBoxType)stream.PeekUInt32(offset + 4);
        }

        public ulong GetExtendedSize(in BitStream stream)
        {
            return size > 1 ? size : size == 1 ? stream.PeekUInt64(8) : (ulong)stream.length;
        }

        public int GetHeaderSize()
        {
            return size != 1 ? 8 : 16;
        }
    }

    // 4.2 ISO/IEC 14496-12:2015(E)
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public unsafe struct ISOFullBox
    {
        public uint size;           // 32 bits
        public ISOBoxType type;     // 32 bits
        public byte version;        // 8 bits
        public uint flags;          // 24 bits

        public ISOFullBox(in BitStream stream)
        {
            size = stream.PeekUInt32();
            type = (ISOBoxType)stream.PeekUInt32(4);

            version = stream.PeekByte(8);
            // Peek the version and flags then remove the version
            flags = stream.PeekUInt32(8) & 0x00FFFFFF;
        }

        public ulong GetExtendedSize(in BitStream stream)
        {
            return size > 1 ? size : size == 1 ? stream.PeekUInt64(12) : (ulong)stream.length;
        }

        public int GetBoxSize()
        {
            return size != 1 ? 12 : 20;
        }
    }

    public unsafe static class ISOTools
    {
        public static bool IsBoxTypeValid(uint type) => ((ISOBoxType)type).IsValid();

        public static bool CanBoxTypeBeParent(uint type) => ((ISOBoxType)type).CanBeParent();
    }

    public unsafe static class ISOBMFFExtensions
    {
        public static ISOBox PeekISOBox(this BitStream stream) => new ISOBox(stream);

        public static ISOBox PeekISOBox(this BitStream stream, int offset) => new ISOBox(stream, offset);

        public static ISOFullBox PeekISOFullBox(this BitStream stream) => new ISOFullBox(stream);

        public static ISOBox ReadISOBox(this BitStream stream)
        {
            var box = new ISOBox(stream);
            stream.Seek(8);
            return box;
        }

        public static ISOFullBox ReadISOFullBox(this BitStream stream)
        {
            var box = new ISOFullBox(stream);
            stream.Seek(12);
            return box;
        }

        public static bool HasValidISOBoxType(this BitStream stream, int offset) => ((ISOBoxType)(stream.PeekUInt32(offset + 4))).IsValid();

        public static bool CanBeParent(this ISOBoxType type)
        {
            switch (type)
            {
                case ISOBoxType.MOOV:
                case ISOBoxType.TRAK:
                case ISOBoxType.EDTS:
                case ISOBoxType.MDIA:
                case ISOBoxType.MINF:
                case ISOBoxType.DINF:
                case ISOBoxType.STBL:
                case ISOBoxType.MVEX:
                case ISOBoxType.MOOF:
                case ISOBoxType.TRAF:
                case ISOBoxType.MFRA:
                case ISOBoxType.SKIP:
                case ISOBoxType.META:
                case ISOBoxType.IPRO:
                case ISOBoxType.SINF:
                case ISOBoxType.FIIN:
                case ISOBoxType.PAEN:
                case ISOBoxType.MECO:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsValid(this ISOBoxType type)
        {
            switch (type)
            {
                case ISOBoxType.BXML:
                case ISOBoxType.CO64:
                case ISOBoxType.CPRT:
                case ISOBoxType.CSLG:
                case ISOBoxType.CTTS:
                case ISOBoxType.DINF:
                case ISOBoxType.DREF:
                case ISOBoxType.EDTS:
                case ISOBoxType.ELNG:
                case ISOBoxType.ELST:
                case ISOBoxType.FECR:
                case ISOBoxType.FIIN:
                case ISOBoxType.FIRE:
                case ISOBoxType.FPAR:
                case ISOBoxType.FREE:
                case ISOBoxType.FRMA:
                case ISOBoxType.FTYP:
                case ISOBoxType.GITN:
                case ISOBoxType.HDLR:
                case ISOBoxType.HMHD:
                case ISOBoxType.IDAT:
                case ISOBoxType.IINF:
                case ISOBoxType.ILOC:
                case ISOBoxType.IPRO:
                case ISOBoxType.IREF:
                case ISOBoxType.LEVA:
                case ISOBoxType.MDAT:
                case ISOBoxType.MDHD:
                case ISOBoxType.MDIA:
                case ISOBoxType.MECO:
                case ISOBoxType.MEHD:
                case ISOBoxType.MERE:
                case ISOBoxType.META:
                case ISOBoxType.MFHD:
                case ISOBoxType.MFRA:
                case ISOBoxType.MFRO:
                case ISOBoxType.MINF:
                case ISOBoxType.MOOF:
                case ISOBoxType.MOOV:
                case ISOBoxType.MVEX:
                case ISOBoxType.MVHD:
                case ISOBoxType.NMHD:
                case ISOBoxType.PADB:
                case ISOBoxType.PAEN:
                case ISOBoxType.PDIN:
                case ISOBoxType.PITM:
                case ISOBoxType.PRFT:
                case ISOBoxType.SAIO:
                case ISOBoxType.SAIZ:
                case ISOBoxType.SBGP:
                case ISOBoxType.SCHI:
                case ISOBoxType.SCHM:
                case ISOBoxType.SDTP:
                case ISOBoxType.SEGR:
                case ISOBoxType.SGPD:
                case ISOBoxType.SIDX:
                case ISOBoxType.SINF:
                case ISOBoxType.SKIP:
                case ISOBoxType.SMHD:
                case ISOBoxType.SSIX:
                case ISOBoxType.STBL:
                case ISOBoxType.STCO:
                case ISOBoxType.STDP:
                case ISOBoxType.STHD:
                case ISOBoxType.STRD:
                case ISOBoxType.STRI:
                case ISOBoxType.STRK:
                case ISOBoxType.STSC:
                case ISOBoxType.STSD:
                case ISOBoxType.STSH:
                case ISOBoxType.STSS:
                case ISOBoxType.STSZ:
                case ISOBoxType.STTS:
                case ISOBoxType.STYP:
                case ISOBoxType.STZ2:
                case ISOBoxType.SUBS:
                case ISOBoxType.TFDT:
                case ISOBoxType.TFHD:
                case ISOBoxType.TFRA:
                case ISOBoxType.TKHD:
                case ISOBoxType.TRAF:
                case ISOBoxType.TRAK:
                case ISOBoxType.TREF:
                case ISOBoxType.TREX:
                case ISOBoxType.TRGR:
                case ISOBoxType.TRUN:
                case ISOBoxType.TSEL:
                case ISOBoxType.UDTA:
                case ISOBoxType.UUID:
                case ISOBoxType.VMHD:
                case ISOBoxType.WIDE:
                case ISOBoxType.XML_:
                case ISOBoxType.CHAP:
                case ISOBoxType.CLIP:
                case ISOBoxType.CRGN:
                case ISOBoxType.CTAB:
                case ISOBoxType.IMAP:
                case ISOBoxType.KMAT:
                case ISOBoxType.LOAD:
                case ISOBoxType.MATT:
                case ISOBoxType.PNOT:
                case ISOBoxType.SCPT:
                case ISOBoxType.SYNC:
                case ISOBoxType.TMCD:
                    return true;
                default:
                    return false;
            }
        }
    }

    // Useful to convert FourCC to HEX
    // https://www.branah.com/ascii-converter

    /// <summary>
    /// FourCC boxes. The value is the FourCC in decimal
    /// </summary>
    public enum ISOBoxType : uint
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
