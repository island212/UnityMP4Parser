using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.MediaFramework.Format.ISOBMFF;
using Unity.MediaFramework.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TestTools;

public unsafe class ISOBoxParseTests
{
    public const int StreamSize = 1024;

    public NativeArray<byte> Stream;

    [SetUp]
    public void SetUp()
    {
        Stream = new NativeArray<byte>(StreamSize, Allocator.TempJob);
    }

    [TearDown]
    public void TearDown()
    {
        Stream.Dispose();
    }

    [Test]
    public void ISOBoxParse_ValidMVHDBox_ReturnExactValue()
    {
        var writer = new BigEndianByteWriter()
        {
            array = (byte*)Stream.GetUnsafePtr(),
            capacity = Stream.Length,
            length = 0
        };

        var expected = ISOBoxTestFactory.GetValidMVHDBox();

        writer.WriteMVHD(expected);

        var actual = MVHDBox.Parse(writer.array);

        AssertISOBox.AreEqual(expected, actual);
        Assert.AreEqual(expected.Duration, MVHDBox.GetDuration(expected.Version, writer.array));
        Assert.AreEqual(expected.Timescale, MVHDBox.GetTimeScale(expected.Version, writer.array));
    }

    [Test]
    public void ISOBoxParse_ValidFTYPBox_ReturnExactValue()
    {
        var writer = new BigEndianByteWriter()
        {
            array = (byte*)Stream.GetUnsafePtr(),
            capacity = Stream.Length,
            length = 0
        };

        var expected = ISOBoxTestFactory.GetValidFTYPBox();

        writer.WriteFTYP(expected);

        var actual = FTYPBox.Parse(writer.array);

        AssertISOBox.AreEqual(expected, actual);
    }
}
