using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.MediaFramework.Format.ISOBMFF;
using Unity.MediaFramework.LowLevel.Unsafe;
using UnityEngine.TestTools;

public unsafe class AsyncISOParseContentTests
{
    public const int StreamSize = 4096;

    public NativeArray<byte> Stream;
    public NativeReference<FixedString128Bytes> Error;
    public NativeReference<ReadCommandArray> Commands;
    public NativeReference<AsyncISOParseContent.HeaderValue> Header;
    public NativeList<AsyncISOParseContent.TrackValue> Tracks;

    public AsyncISOParseContent Job;

    [SetUp]
    public void SetUp()
    {
        Stream = new NativeArray<byte>(StreamSize, Allocator.TempJob);
        Error = new NativeReference<FixedString128Bytes>(Allocator.TempJob);
        Commands = new NativeReference<ReadCommandArray>(Allocator.TempJob);
        Header = new NativeReference<AsyncISOParseContent.HeaderValue>(Allocator.TempJob);
        Tracks = new NativeList<AsyncISOParseContent.TrackValue>(8, Allocator.TempJob);

        var readCommandArray = new ReadCommandArray();
        readCommandArray.CommandCount = 1;
        readCommandArray.ReadCommands = (ReadCommand*)UnsafeUtility.Malloc(sizeof(ReadCommand), 8, Allocator.TempJob);

        readCommandArray.ReadCommands[0].Buffer = Stream.GetUnsafePtr();
        readCommandArray.ReadCommands[0].Size = Stream.Length;
        readCommandArray.ReadCommands[0].Offset = 0;

        Job = new AsyncISOParseContent()
        {
            Commands = Commands,
            Error = Error,
            Header = Header,
            Tracks = Tracks,
            Stream = Stream,
            FileSize = 0
        };
    }

    [TearDown]
    public void TearDown()
    {
        UnsafeUtility.Free(Commands.Value.ReadCommands, Allocator.TempJob);

        Stream.Dispose();
        Error.Dispose();
        Commands.Dispose();
        Header.Dispose();
        Tracks.Dispose();
    }

    [Test]
    public void RunJob_WhenStreamEmpty_ReturnNoError()
    {
        Job.FileSize = 0;
        Job.Run();

        Assert.IsTrue(Job.Error.Value.IsEmpty);
    }

    [Test]
    public void ParseMVHD_WithValidData_ReturnValidData()
    {     
        var writer = Stream.AsByteWriter(Endianess.Big);

        SetWriterForJob(writer);

        Job.Run();
    }

    [Test]
    public void ParseMVHD_WhenNoMOOVBox_ProvideError()
    {

    }

    public void SetWriterForJob(in ByteWriter writer)
    {
        Job.FileSize = writer.Length;

        ref var readCommand = ref UnsafeUtility.AsRef<ReadCommand>(Commands.Value.ReadCommands);
        readCommand.Size = writer.Length;
    }
}
