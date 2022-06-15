using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.MediaFramework.Format.ISOBMFF;
using Unity.MediaFramework.LowLevel.Unsafe;
using UnityEngine.TestTools;

public unsafe class AsyncISOParseContentTests
{
    public const int StreamSize = 4096;

    public NativeArray<byte> Stream;
    public NativeList<ReadCommand> ReadCommands;

    public AsyncISOParseContent Job;

    [SetUp]
    public void SetUp()
    {
        Stream = new NativeArray<byte>(StreamSize, Allocator.TempJob);
        ReadCommands = new NativeList<ReadCommand>(Allocator.TempJob);

        ReadCommands.Add(new ReadCommand() 
        { 
            Buffer = Stream.GetUnsafePtr(),
            Size = Stream.Length,
            Offset = 0,
        });

        var readCommandArray = new ReadCommandArray();
        readCommandArray.CommandCount = 1;
        readCommandArray.ReadCommands = (ReadCommand*)ReadCommands.GetUnsafePtr();

        var readCommands = new NativeReference<ReadCommandArray>(Allocator.TempJob);
        readCommands.Value = readCommandArray;

        Job = new AsyncISOParseContent()
        {
            FileSize = 0,
            Stream = Stream,
            Commands = readCommands,
            Error = new NativeReference<AsyncISOParseContent.ErrorValue>(Allocator.TempJob),
            Header = new NativeReference<AsyncISOParseContent.HeaderValue>(Allocator.TempJob),
            Tracks = new NativeList<AsyncISOParseContent.TrackValue>(8, Allocator.TempJob)
        };
    }

    [TearDown]
    public void TearDown()
    {
        Stream.Dispose();
        ReadCommands.Dispose();

        Job.Error.Dispose();
        Job.Commands.Dispose();
        Job.Header.Dispose();
        Job.Tracks.Dispose();
    }

    [Test]
    public void RunJob_WhenStreamEmpty_ReturnNoError()
    {
        SetReadCommandSizeForJob(0);

        Job.Run();

        AssertNoError();
    }

    [Test]
    public void ParseMVHD_WithValidData_ReturnValidData()
    {     
        var writer = Stream.AsByteWriter(Endianess.Big);
        var box = ISOBoxTestFactory.GetValidMVHDBox();

        writer.WriteMVHD(box);

        SetReadCommandSizeForJob(writer.Length);

        Job.Run();

        AssertNoError();

        Assert.AreEqual(box.Duration, Job.Header.Value.duration);
        Assert.AreEqual(box.Timescale, Job.Header.Value.timescale);
    }

    [Test]
    public void ParseMVHD_WithDuplicateBox_ReturnError()
    {
        var writer = Stream.AsByteWriter(Endianess.Big);
        var box = ISOBoxTestFactory.GetValidMVHDBox();

        writer.WriteMVHD(box);
        writer.WriteMVHD(box);

        SetReadCommandSizeForJob(writer.Length);

        Job.Run();

        AssertError(AsyncISOParseContent.ErrorType.DuplicateBox);
    }

    [Test]
    public void ParseMVHD_WithInvalidSize_ReturnError()
    {
        var writer = Stream.AsByteWriter(Endianess.Big);
        var box = ISOBoxTestFactory.GetValidMVHDBox();

        writer.WriteMVHD(box);

        BigEndian.WriteUInt32((byte*)Stream.GetUnsafePtr(), (uint)MVHDBox.GetSize(0) - 10);

        SetReadCommandSizeForJob((int)MVHDBox.GetSize(0));

        Job.Run();

        AssertError(AsyncISOParseContent.ErrorType.InvalidSize);
    }

    [Test]
    public void ParseMVHD_WithInvalidSizeForVersion_ReturnError()
    {
        var writer = Stream.AsByteWriter(Endianess.Big);
        var box = ISOBoxTestFactory.GetValidMVHDBox();

        writer.WriteMVHD(box);

        BigEndian.WriteUInt32((byte*)Stream.GetUnsafePtr(), (uint)MVHDBox.GetSize(1));

        SetReadCommandSizeForJob((int)MVHDBox.GetSize(1));

        Job.Run();

        AssertError(AsyncISOParseContent.ErrorType.InvalidSize);
    }

    public void SetReadCommandSizeForJob(int size)
    {
        Job.FileSize = size;

        ref var readCommand = ref UnsafeUtility.AsRef<ReadCommand>(Job.Commands.Value.ReadCommands);
        readCommand.Size = size;
    }

    public void AssertNoError()
    {
        Assert.AreEqual(AsyncISOParseContent.ErrorType.None, Job.Error.Value.Type);
        Assert.IsTrue(Job.Error.Value.Message.IsEmpty);
    }

    public void AssertError(AsyncISOParseContent.ErrorType error)
    {
        Assert.AreEqual(error, Job.Error.Value.Type);
        Assert.IsTrue(!Job.Error.Value.Message.IsEmpty);
        Assert.AreEqual(0, Job.Commands.Value.CommandCount);
    }
}
