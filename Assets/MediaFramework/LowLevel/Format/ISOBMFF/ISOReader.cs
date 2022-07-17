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
using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Burst;

namespace Unity.MediaFramework.LowLevel.Format.ISOBMFF
{

    public struct ISOBuffer
    {
        public NativeReference<UnsafeByteArray> RawBuffer;

        public JobHandle Dispose(JobHandle depends = default)
        {
            depends = new DisposeRawBufferJob 
                { RawBuffer = RawBuffer }.Schedule(depends);
            return RawBuffer.Dispose(depends);
        }

        public struct DisposeRawBufferJob : IJob
        {
            public NativeReference<UnsafeByteArray> RawBuffer;

            public unsafe void Execute()
            {
                UnsafeUtility.AsRef<UnsafeByteArray>(RawBuffer.GetUnsafePtr()).Dispose();
            }
        }
    }

    public unsafe static class AsyncISOReader
    {
        public static JobHandle Read(string path, out NativeReference<UnsafeByteArray> header, int streamSize = 8192, JobHandle depends = default)
        {
            header = new NativeReference<UnsafeByteArray>(Allocator.Persistent);

            FileInfoResult fileInfo;
            AsyncReadManager.GetFileInfo(path, &fileInfo).JobHandle.Complete();

            if (fileInfo.FileState == FileState.Absent)
            {
                Debug.LogError($"File at {path} does not exist");
                return default;
            }

            var fileHandle = AsyncReadManager.OpenFileAsync(path);

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

            var jobContext = new MediaJobContext
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

            ReadHandle readHandle;
            var handle = JobHandle.CombineDependencies(fileHandle.JobHandle, depends);
            var readCommandsPtr = &((MediaIOContext*)refIOContext.GetUnsafePtr())->Commands;
            for (int i = 0; i < readCount; i++)
            {
                readHandle = AsyncReadManager.ReadDeferred(fileHandle, readCommandsPtr, handle);
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
                Header = header
            }
            .Schedule(handle);

            readHandle = AsyncReadManager.ReadDeferred(fileHandle, readCommandsPtr, handle);
            handle = JobHandle.CombineDependencies(handle, readHandle.JobHandle);
            handle = new DisposeReadHandleJob
                { Job = readHandle }.Schedule(handle);

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
