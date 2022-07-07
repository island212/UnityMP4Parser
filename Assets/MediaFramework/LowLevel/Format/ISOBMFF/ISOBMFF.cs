using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel.Unsafe;
using Unity.MediaFramework.Video;

namespace Unity.MediaFramework.LowLevel.Format.ISOBMFF
{
    public struct ISODate
    {
        public ulong value;
    }

    // ISO 639-2 Code
    // https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes
    public struct ISOLanguage
    {
        // padding (1 bit) + character (5 bits)[3]
        public ushort value;
    }

    public struct FixedPoint1616Matrix3x3
    {
        public const int ByteNeeded = 36;

        public int3x3 value;
    }

    public struct FixedPoint1616
    {
        public int value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ConvertDouble() => ConvertDouble(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConvertDouble(int value)
            => value > 0 ? value / 65536.0 : 0;
    }

    public struct UFixedPoint1616
    {
        public uint value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ConvertDouble() => ConvertDouble(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConvertDouble(uint value)
            => value > 0 ? value / 65536.0 : 0;
    }

    public struct FixedPoint88
    {
        public short value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ConvertDouble() => ConvertDouble(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConvertDouble(short value)
            => value > 0 ? value / 256.0 : 0;
    }

    public static class ISOUtility
    {
        public static bool CanBeParent(ISOBoxType type)
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

    /// <summary>
    /// FourCC brands. The value is the FourCC in decimal
    /// </summary>
    public enum ISOBrand : uint
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
        MP41 = 0x6d703431,
        MP42 = 0x6d703432,
        MP71 = 0x6d703731,
    }

    public enum ISOHandler
    { 
        None = 0,
        VIDE = 0x76696465,
        SOUN = 0x736f756e
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
