using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public unsafe class LazyReadingVSFullReading
{
    // A Test behaves as an ordinary method
    [Test, Performance]
    public void LazyReadingVSFullReadingSimplePasses()
    {
        using var values = new NativeArray<int>(100, Allocator.Temp);

        var buffer = (byte*)values.GetUnsafePtr();

        Measure.Method(() =>
        {
            for (int i = 0; i < 10000; i++)
            {
                var mvhd = new MVHDBox(buffer);
            }
        })
        .SampleGroup("FullRead")
        .WarmupCount(10)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            for (int i = 0; i < 10000; i++)
            {
                var duration = MVHDBox.GetDuration(buffer);
                var timeScale = MVHDBox.GetTimeScale(buffer);
            }
        })
        .SampleGroup("Lazy Inline")
        .WarmupCount(10)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            for (int i = 0; i < 10000; i++)
            {
                var duration = NoInline.GetDuration(buffer);
                var timeScale = NoInline.GetTimeScale(buffer);
            }
        })
        .SampleGroup("Lazy NoInline")
        .WarmupCount(10)
        .IterationsPerMeasurement(100)
        .MeasurementCount(20)
        .Run();
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public unsafe struct ISOFullBox
    {
        public const int Stride = 12;

        public uint size;           // 32 bits
        public ISOBoxType type;     // 32 bits
        public byte version;        // 8 bits
        public uint flags;          // 24 bits

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetVersion(byte* buffer) => *(buffer + 8);
    }

    public readonly unsafe struct MVHDBox
    {
        public readonly ISOFullBox header;
        public readonly ulong creationTime;
        public readonly ulong modificationTime;
        public readonly uint timescale;
        public readonly ulong duration;
        public readonly FixedPoint1616 rate;
        public readonly FixedPoint88 volume;
        public readonly FixedPoint1616Matrix3x3 matrix;
        public readonly uint nextTrackID;

        public MVHDBox(byte* buffer)
        {
            header = ISOUtility.GetISOFullBox(buffer);
            if (header.version == 1)
            {
                creationTime = BigEndian.GetUInt64(buffer + ISOFullBox.Stride);
                modificationTime = BigEndian.GetUInt64(buffer + ISOFullBox.Stride + 8);
                timescale = BigEndian.GetUInt32(buffer + ISOFullBox.Stride + 16);
                duration = BigEndian.GetUInt64(buffer + ISOFullBox.Stride + 20);

                buffer += ISOFullBox.Stride + 28; // fullBox + creationTime + modificationTime + timescale + duration
            }
            else
            {
                creationTime = BigEndian.GetUInt32(buffer + ISOFullBox.Stride);
                modificationTime = BigEndian.GetUInt32(buffer + ISOFullBox.Stride + 4);
                timescale = BigEndian.GetUInt32(buffer + ISOFullBox.Stride + 8);
                duration = BigEndian.GetUInt32(buffer + ISOFullBox.Stride + 12);

                buffer += ISOFullBox.Stride + 16; // fullBox + creationTime + modificationTime + timescale + duration
            }

            rate = new FixedPoint1616() { value = BigEndian.GetInt32(buffer) };
            volume = new FixedPoint88() { value = BigEndian.GetInt16(buffer + 4) };

            buffer += 4 + 2 + 2 + 4 + 4; // rate + volume + reserved(16) + reserved(32)[2]

            matrix = new FixedPoint1616Matrix3x3
            {
                value = new int3x3(
                    BigEndian.GetInt32(buffer), BigEndian.GetInt32(buffer + 4), BigEndian.GetInt32(buffer + 8),
                    BigEndian.GetInt32(buffer + 12), BigEndian.GetInt32(buffer + 16), BigEndian.GetInt32(buffer + 20),
                    BigEndian.GetInt32(buffer + 24), BigEndian.GetInt32(buffer + 28), BigEndian.GetInt32(buffer + 32))
            };

            buffer += FixedPoint1616Matrix3x3.Stride + 4 * 6; // matrix + reserved(32)[6]

            nextTrackID = BigEndian.GetUInt32(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetTimeScale(byte* buffer)
            => BigEndian.GetUInt32(buffer + ISOFullBox.Stride + (ISOFullBox.GetVersion(buffer) == 1 ? 16 : 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetDuration(byte* buffer)
            => BigEndian.GetUInt32(buffer + ISOFullBox.Stride + (ISOFullBox.GetVersion(buffer) == 1 ? 20 : 12));
    }

    public static class NoInline
    {
        public static int GetVersion(byte* buffer) => *(buffer + 8);

        public static uint GetTimeScale(byte* buffer)
            => BigEndian.GetUInt32(buffer + ISOFullBox.Stride + (GetVersion(buffer) == 1 ? 16 : 8));

        public static ulong GetDuration(byte* buffer)
            => BigEndian.GetUInt32(buffer + ISOFullBox.Stride + (GetVersion(buffer) == 1 ? 20 : 12));
    }

    public struct FixedPoint1616Matrix3x3
    {
        public const int Stride = 36;

        public int3x3 value;
    }

    public struct FixedPoint1616
    {
        public int value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ConvertDouble() => ConvertDouble(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConvertDouble(int value)
            => value / 65536.0;
    }

    public struct UFixedPoint1616
    {
        public uint value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ConvertDouble() => ConvertDouble(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConvertDouble(uint value)
            => value / 65536.0;
    }

    public struct FixedPoint88
    {
        public short value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ConvertDouble() => ConvertDouble(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConvertDouble(short value)
            => value / 256.0;
    }

    public struct MediaRational
    {
        public ulong num;
        public ulong dem;
    }

    public unsafe static class ISOUtility
    {

        public static ISOFullBox GetISOFullBox(byte* buffer) => new ISOFullBox()
        {
            size = BigEndian.GetUInt32(buffer),
            type = (ISOBoxType)BigEndian.GetUInt32(buffer + 4),

            version = *(buffer + 8),
            // Peek the version and flags then remove the version
            flags = BigEndian.GetUInt32(buffer + 8) & 0x00FFFFFF
        };
    }

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

    [BurstCompile]
    public unsafe static class BigEndian
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetUInt16(byte* data) => (ushort)(data[0] << 8 | data[1]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUInt32(byte* data) =>
                (uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | data[3];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetUInt64(byte* data) =>
            (ulong)data[0] << 56 | (ulong)data[1] << 48 | (ulong)data[2] << 40 | (ulong)data[3] << 32 |
            (ulong)data[4] << 24 | (ulong)data[5] << 16 | (ulong)data[6] << 8 | data[7];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short GetInt16(byte* data) => (short)(data[0] << 8 | data[1]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetInt32(byte* data) =>
            (int)data[0] << 24 | (int)data[1] << 16 | (int)data[2] << 8 | data[3];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetInt64(byte* data) =>
            (long)data[0] << 56 | (long)data[1] << 48 | (long)data[2] << 40 | (long)data[3] << 32 |
            (long)data[4] << 24 | (long)data[5] << 16 | (long)data[6] << 8 | data[7];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, ushort value)
        {
            data[0] = (byte)(value >> 8);
            data[1] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, uint value)
        {
            data[0] = (byte)(value >> 24);
            data[1] = (byte)(value >> 16);
            data[2] = (byte)(value >> 8);
            data[3] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, ulong value)
        {
            data[0] = (byte)(value >> 56);
            data[1] = (byte)(value >> 48);
            data[2] = (byte)(value >> 40);
            data[3] = (byte)(value >> 32);
            data[4] = (byte)(value >> 24);
            data[5] = (byte)(value >> 16);
            data[6] = (byte)(value >> 8);
            data[7] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, short value) => Write(data, (ushort)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, int value) => Write(data, (uint)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte* data, long value) => Write(data, (ulong)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ConvertToString(uint data) =>
            new string(new char[4] {
                (char)((data & 0xFF000000) >> 24),
                (char)((data & 0x00FF0000) >> 16),
                (char)((data & 0x0000FF00) >> 8),
                (char)((data & 0x000000FF))
            });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ConvertFourCCToUInt32(string data) =>
            (uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | (uint)data[3];
    }
}
