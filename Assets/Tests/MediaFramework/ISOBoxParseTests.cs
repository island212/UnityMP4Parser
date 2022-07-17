using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel.Format.ISOBMFF;
using Unity.MediaFramework.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TestTools;

public unsafe class ISOBoxParseTests
{
    public const int StreamSize = 1024;

    public NativeArray<byte> Stream;
    public BigEndianByteWriter Writer;

    [SetUp]
    public void SetUp()
    {
        Stream = new NativeArray<byte>(StreamSize, Allocator.TempJob);
        Writer = new BigEndianByteWriter()
        {
            array = (byte*)Stream.GetUnsafePtr(),
            capacity = Stream.Length,
            length = 0
        };
    }

    [TearDown]
    public void TearDown()
    {
        Stream.Dispose();
    }

    [Test]
    public void ISOBoxParse_ValidFTYPBox_ReturnExactValue()
    {
        var expected = ISOBoxTestFactory.FTYP.Get();

        Writer.WriteFTYP(expected);

        var actual = FTYPBox.Parse(Writer.array);

        AssertISOBox.AreEqual(expected, actual);
    }

    [Test]
    public void ISOBoxParse_ValidMVHDBox_ReturnExactValue()
    {
        var expected = ISOBoxTestFactory.MVHD.Get();

        Writer.WriteMVHD(expected);

        var box = ISOBox.Parse(Writer.array);
        var actual = MVHDBox.Parse(Writer.array);

        AssertISOBox.AreEqual(expected, actual);
        Assert.AreEqual(expected.Duration, MVHDBox.GetDuration(actual.Version, Writer.array));
        Assert.AreEqual(expected.Timescale, MVHDBox.GetTimeScale(actual.Version, Writer.array));
    }

    [Test]
    public void ISOBoxParse_ValidTKHDBoxVideo_ReturnExactValue()
    {
        var expected = ISOBoxTestFactory.TKHD.GetVideo();

        Writer.WriteTKHD(expected);

        var actual = TKHDBox.Parse(Writer.array);

        AssertISOBox.AreEqual(expected, actual);
    }

    [Test]
    public void ISOBoxParse_ValidTKHDBoxAudio_ReturnExactValue()
    {
        var expected = ISOBoxTestFactory.TKHD.GetAudio();

        Writer.WriteTKHD(expected);

        var actual = TKHDBox.Parse(Writer.array);

        AssertISOBox.AreEqual(expected, actual);
    }

    [Test]
    public void ISOBoxParse_ValidMDHDBox_ReturnExactValue()
    {
        var expected = ISOBoxTestFactory.MDHD.Get();

        Writer.WriteMDHD(expected);

        var actual = MDHDBox.Parse(Writer.array);

        AssertISOBox.AreEqual(expected, actual);
    }

    [Test]
    public void ISOBoxParse_ValidHDLRBoxVideo_ReturnExactValue()
    {
        var expected = ISOBoxTestFactory.HDLR.GetVideo();

        Writer.WriteHDLR(expected);

        var actual = HDLRBox.Parse(Writer.array);

        AssertISOBox.AreEqual(expected, actual);
    }
}
