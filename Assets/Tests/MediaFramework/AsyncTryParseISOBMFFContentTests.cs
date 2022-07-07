using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel.Format.ISOBMFF;
using Unity.MediaFramework.LowLevel;
using Unity.MediaFramework.LowLevel.Unsafe;
using UnityEngine.TestTools;

public unsafe class AsyncTryParseISOBMFFContentTests
{
    public const int StreamSize = 4096;

    public NativeArray<byte> Stream;
    public NativeList<ReadCommand> ReadCommands;

    public AsyncTryParseISOBMFFContent Job;

    //[SetUp]
    //public void SetUp()
    //{
    //    Stream = new NativeArray<byte>(StreamSize, Allocator.TempJob);
    //    ReadCommands = new NativeList<ReadCommand>(Allocator.TempJob);

    //    ReadCommands.Add(new ReadCommand() 
    //    { 
    //        Buffer = Stream.GetUnsafePtr(),
    //        Size = Stream.Length,
    //        Offset = 0,
    //    });

    //    var readCommandArray = new ReadCommandArray();
    //    readCommandArray.CommandCount = 1;
    //    readCommandArray.ReadCommands = (ReadCommand*)ReadCommands.GetUnsafePtr();

    //    var readCommands = new NativeReference<ReadCommandArray>(Allocator.TempJob);
    //    readCommands.Value = readCommandArray;

    //    Job = new AsyncTryParseISOBMFFContent()
    //    {
    //        Stream = Stream,
    //        Context = new NativeReference<AsyncTryParseISOBMFFContent.JobContext>(Allocator.TempJob),
    //        IOContext = new NativeReference<IOContext>(Allocator.TempJob),
    //        Tracks = new NativeList<AsyncTryParseISOBMFFContent.ISOTrack>(16, Allocator.TempJob)
    //    };
    //}

    //[TearDown]
    //public void TearDown()
    //{
    //    Stream.Dispose();
    //    ReadCommands.Dispose();

    //    Job.Error.Dispose();
    //    Job.Commands.Dispose();
    //    Job.Header.Dispose();
    //    Job.Tracks.Dispose();
    //}

    //[Test]
    //public void RunJob_WhenStreamEmpty_ReturnNoError()
    //{
    //    SetReadCommandSizeForJob(0);

    //    Job.Run();

    //    AssertNoError();
    //}

    //[Test]
    //public void ParseMVHD_WithValidData_ReturnValidData()
    //{     
    //    var writer = Stream.AsByteWriter(Endianess.Big);
    //    var box = ISOBoxTestFactory.MVHD.Get();

    //    writer.WriteMVHD(box);

    //    SetReadCommandSizeForJob(writer.Length);

    //    Job.Run();

    //    AssertNoError();

    //    Assert.AreEqual(box.Duration, Job.Header.Value.duration);
    //    Assert.AreEqual(box.Timescale, Job.Header.Value.timescale);
    //}

    //[Test]
    //public void ParseMVHD_WithDuplicateBox_ReturnError()
    //{
    //    var writer = Stream.AsByteWriter(Endianess.Big);
    //    var box = ISOBoxTestFactory.MVHD.Get();

    //    writer.WriteMVHD(box);
    //    writer.WriteMVHD(box);

    //    SetReadCommandSizeForJob(writer.Length);

    //    Job.Run();

    //    AssertError(AsyncTryParseISOBMFFContent.ErrorType.DuplicateBox);
    //}

    //[Test]
    //public void ParseMVHD_WithInvalidSize_ReturnError()
    //{
    //    var writer = Stream.AsByteWriter(Endianess.Big);
    //    var box = ISOBoxTestFactory.MVHD.Get();

    //    writer.WriteMVHD(box);

    //    BigEndian.WriteUInt32((byte*)Stream.GetUnsafePtr(), (uint)MovieHeaderBox.GetSize(0) - 10);

    //    SetReadCommandSizeForJob((int)MovieHeaderBox.GetSize(0));

    //    Job.Run();

    //    AssertError(AsyncTryParseISOBMFFContent.ErrorType.InvalidSize);
    //}

    //[Test]
    //public void ParseMVHD_WithInvalidSizeForVersion_ReturnError()
    //{
    //    var writer = Stream.AsByteWriter(Endianess.Big);
    //    var box = ISOBoxTestFactory.MVHD.Get();

    //    writer.WriteMVHD(box);

    //    BigEndian.WriteUInt32((byte*)Stream.GetUnsafePtr(), (uint)MovieHeaderBox.GetSize(1));

    //    SetReadCommandSizeForJob((int)MovieHeaderBox.GetSize(1));

    //    Job.Run();

    //    AssertError(AsyncTryParseISOBMFFContent.ErrorType.InvalidSize);
    //}

    //public void SetReadCommandSizeForJob(int size)
    //{
    //    Job.FileSize = size;

    //    ref var readCommand = ref UnsafeUtility.AsRef<ReadCommand>(Job.Commands.Value.ReadCommands);
    //    readCommand.Size = size;
    //}

    //public void AssertNoError()
    //{
    //    Assert.AreEqual(ErrorType.None, Job.Context.Value.Error.Type);
    //    Assert.IsTrue(Job.Context.Value.Error.Message.IsEmpty);
    //}

    //public void AssertError(ErrorType error)
    //{
    //    Assert.AreEqual(error, Job.Context.Value.Error.Type);
    //    Assert.IsTrue(!Job.Context.Value.Error.Message.IsEmpty);
    //    Assert.AreEqual(0, Job.Commands.Value.CommandCount);
    //}
}
