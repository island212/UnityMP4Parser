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

    public struct PartialISOParse
    {
        public JobHandle Handle;

        public NativeReference<JobContext> Context;
        public NativeList<ISOTrack> Tracks;

        internal NativeList<byte> MemCopyBuffer;

        public PartialISOParse(int copyBufferCapacity = 16384)
        {
            Context = new NativeReference<JobContext>(Allocator.Persistent);
            MemCopyBuffer = new NativeList<byte>(copyBufferCapacity, Allocator.Persistent);
            Tracks = new NativeList<ISOTrack>(8, Allocator.Persistent);

            Handle = default;
        }

        public JobHandle GetMediaAttributes(out ISOMediaAttributes attributes, JobHandle depends = default)
        {
            int videoTrackCount = 0, audioTrackCount = 0;
            foreach (var track in Tracks)
            {
                switch (track.Handler)
                {
                    case ISOHandler.VIDE: videoTrackCount++; break;
                    case ISOHandler.SOUN: audioTrackCount++; break;
                }
            }

            attributes = new ISOMediaAttributes
            {
                VideoTracks = new NativeArray<VideoTrack>(videoTrackCount, Allocator.Persistent),
                AudioTracks = new NativeArray<AudioTrack>(audioTrackCount, Allocator.Persistent)
            };

            return depends;
        }

        public JobHandle Dispose(JobHandle depends = default)
        {
            var disposeArray = new NativeArray<JobHandle>(3, Allocator.Temp);
            disposeArray[0] = Context.Dispose(depends);
            disposeArray[1] = Tracks.Dispose(depends);
            disposeArray[2] = MemCopyBuffer.Dispose(depends);
            var handle = JobHandle.CombineDependencies(disposeArray);
            disposeArray.Dispose();
            return handle;
        }
    }

    public unsafe static class AsyncISOReader
    {
        public static PartialISOParse ParseFile(string path, int streamSize = 65536, int copyBufferCapacity = 16384)
        {
            FileInfoResult fileInfo;
            AsyncReadManager.GetFileInfo(path, &fileInfo).JobHandle.Complete();

            if (fileInfo.FileState == FileState.Absent)
            {
                Debug.LogError($"File at {path} does not exist");
                return default;
            }

            var fileHandle = AsyncReadManager.OpenFileAsync(path);

            var isoParse = new PartialISOParse
            {
                Context = new NativeReference<JobContext>(Allocator.Persistent),
                MemCopyBuffer = new NativeList<byte>(copyBufferCapacity, Allocator.Persistent),
                Tracks = new NativeList<ISOTrack>(8, Allocator.Persistent),
                Handle = fileHandle.JobHandle
            };

            var stream = new NativeArray<byte>(streamSize, Allocator.TempJob);
            var readCommands = new NativeArray<ReadCommand>(8, Allocator.TempJob);

            var ioContext = new IOContext
            {
                FileSize = fileInfo.FileSize,
                Commands = new ReadCommandArray
                {
                    CommandCount = 0,
                    ReadCommands = (ReadCommand*)readCommands.GetUnsafePtr()
                }
            };

            var readCommand = new ReadCommand
            {
                Buffer = stream.GetUnsafePtr(),
                Size = stream.Length,
                Offset = 0,
            };

            ioContext.AddReadCommandJob(ref readCommand);

            isoParse.Context.Value = new JobContext
            {
                ReadCommand = readCommand
            };

            var refIOContext = new NativeReference<IOContext>(ioContext, Allocator.TempJob);
            var readCommandsPtr = &((IOContext*)refIOContext.GetUnsafePtr())->Commands;

            var readJobList = new NativeArray<ReadHandle>(32, Allocator.TempJob);
            for (int i = 0; i < readJobList.Length; i++)
            {
                readJobList[i] = AsyncReadManager.ReadDeferred(fileHandle, readCommandsPtr, isoParse.Handle);
                isoParse.Handle = JobHandle.CombineDependencies(isoParse.Handle, readJobList[i].JobHandle);

                isoParse.Handle = new AsyncTryParseISOBMFFContent
                {
                    Stream = stream,
                    IOContext = refIOContext,
                    Context = isoParse.Context,
                    MemCopyBuffer = isoParse.MemCopyBuffer,
                    Tracks = isoParse.Tracks
                }
                .Schedule(isoParse.Handle);
            }

            isoParse.Handle = fileHandle.Close(isoParse.Handle);
            isoParse.Handle = new ParseDisposeJob
            { 
                ReadJobs = readJobList,
                IOContext = refIOContext,
                ReadCommands = readCommands,
                Stream = stream
            }
            .Schedule(isoParse.Handle);

            return isoParse;
        }

        private struct ParseDisposeJob : IJob
        {
            public NativeArray<ReadHandle> ReadJobs;
            public NativeArray<byte> Stream;
            public NativeArray<ReadCommand> ReadCommands;
            public NativeReference<IOContext> IOContext;

            public void Execute()
            {
                ReadJobs.Dispose();
                Stream.Dispose();
                ReadCommands.Dispose();
                IOContext.Dispose();
            }
        }
    }
}
