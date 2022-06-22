using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel.Unsafe;
using Unity.Jobs;

using static Unity.MediaFramework.Format.ISOBMFF.AsyncTryParseISOBMFFContent;
using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Burst;

namespace Unity.MediaFramework.Format.ISOBMFF
{
    public struct VideoTrack
    {
        public uint TrackID;                            // TKHD
        public uint TimeScale;                          // MDHD
        public ulong Duration;                          // MDHD
        public int Width, Height;                       // TKHD
        public VideoCodec Codec;                        // STSD
        public int PixelWidth, PixelHeight;             // STSD
        public ColorFormat ColorFormat;                 // STSD

        public int FrameCount;                          // STTS
        public TimeToSampleTable TimeToSampleTable;     // STTS
        public SyncSampleTable SyncSampleTable;         // STSS

        internal UnsafeRawArray TableContent;
    }

    public struct AudioTrack
    {
        public uint TrackID;                            // TKHD
        public uint TimeScale;                          // MDHD
        public ulong Duration;                          // MDHD
        public ISOLanguage Language;                    // MDHD
        public AudioCodec Codec;                        // STSD
        public int ChannelCount;                        // STSD
        public int SampleRate;                          // STSD

        public int SampleCount;                         // STTS
        public TimeToSampleTable TimeToSampleTable;     // STTS

        internal UnsafeRawArray TableContent;
    }

    public struct SampleGoup
    {
        public uint Count;
        public uint Delta;
    }

    public unsafe struct TimeToSampleTable
    {
        public int EntryCount;
        public SampleGoup* SamplesTable;
    }

    public unsafe struct SyncSampleTable
    {
        public int EntryCount;
        public uint* SampleNumberTable;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct ColorFormat
    {
        public uint type;
        public ushort primaries;
        public ushort transfer;
        public ushort matrix;
        public bool fullRange;
    }

    public struct ISOMediaAttributes
    {
        public NativeArray<VideoTrack> VideoTracks;
        public NativeArray<AudioTrack> AudioTracks;
    }

    public struct RawISOHeader
    {
        public NativeReference<UnsafeRawArray> RawBuffer;

        public JobHandle GetMediaAttributes(out ISOMediaAttributes attributes, JobHandle depends = default)
        {
            attributes = new ISOMediaAttributes();
            return depends;
        }

        public JobHandle Dispose(JobHandle depends = default)
        {
            return RawBuffer.Dispose(depends);
        }
    }

    public unsafe static class AsyncISOReader
    {
        public static JobHandle Read(string path, out RawISOHeader header, int streamSize = 8192, JobHandle depends = default)
        {
            FileInfoResult fileInfo;
            AsyncReadManager.GetFileInfo(path, &fileInfo).JobHandle.Complete();

            if (fileInfo.FileState == FileState.Absent)
            {
                Debug.LogError($"File at {path} does not exist");
                header = default;
                return default;
            }

            var fileHandle = AsyncReadManager.OpenFileAsync(path);

            header = new RawISOHeader
            {
                RawBuffer = new NativeReference<UnsafeRawArray>(Allocator.Persistent)
            };

            int readCount = 16;
            var stream = new NativeArray<byte>(streamSize, Allocator.TempJob);
            var readCommands = new NativeArray<ReadCommand>(readCount, Allocator.TempJob);
            var boxChunks = new NativeList<BoxChunk>(readCount, Allocator.TempJob);

            var ioContext = new MediaIOContext
            {
                FileSize = fileInfo.FileSize,
                Commands = new ReadCommandArray
                {
                    CommandCount = 0,
                    ReadCommands = (ReadCommand*)readCommands.GetUnsafePtr()
                }
            };

            var jobContext = new MediaJobContext()
            {
                ReadCommand = new ReadCommand
                {
                    Buffer = stream.GetUnsafePtr(),
                    Size = stream.Length,
                    Offset = 0,
                }
            };

            ioContext.AddReadCommandJob(ref jobContext.ReadCommand);

            var refJobContext = new NativeReference<MediaJobContext>(jobContext, Allocator.TempJob);
            var refIOContext = new NativeReference<MediaIOContext>(ioContext, Allocator.TempJob);

            var handle = JobHandle.CombineDependencies(fileHandle.JobHandle, depends);
            var readCommandsPtr = &((MediaIOContext*)refIOContext.GetUnsafePtr())->Commands;
            for (int i = 0; i < readCount; i++)
            {
                var readHandle = AsyncReadManager.ReadDeferred(fileHandle, readCommandsPtr, handle);
                handle = JobHandle.CombineDependencies(handle, readHandle.JobHandle);

                handle = new FindTopBoxes
                {
                    Stream = stream,
                    BoxChunks = boxChunks,
                    IOContext = refIOContext,
                    JobContext = refJobContext
                }
                .Schedule(handle);

                handle = new DisposeReadHandleJob 
                    { Job = readHandle }.Schedule(handle);
            }

            handle = new ReadAndLoadInMemoryAllBoxChunks
            {
                BoxChunks = boxChunks,
                IOContext = refIOContext,
                RawBuffer = header.RawBuffer
            }
            .Schedule(handle);

            handle = JobHandle.CombineDependencies(handle, fileHandle.Close(handle));

            var diposeList = new NativeList<JobHandle>(8, Allocator.Temp);
            diposeList.Add(boxChunks.Dispose(handle));
            diposeList.Add(stream.Dispose(handle));
            diposeList.Add(readCommands.Dispose(handle));
            diposeList.Add(refJobContext.Dispose(handle));
            diposeList.Add(refIOContext.Dispose(handle));

            handle = JobHandle.CombineDependencies(diposeList);
            diposeList.Dispose();

            return handle;
        }

        [BurstCompile]
        private struct DisposeReadHandleJob : IJob
        {
            public ReadHandle Job;

            public void Execute() 
            {
                Job.Dispose();
            }
        }
    }
}
