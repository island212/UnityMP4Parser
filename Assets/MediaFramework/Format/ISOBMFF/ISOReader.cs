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

namespace Unity.MediaFramework.Format.ISOBMFF
{
    public unsafe struct ISOReader
    {
        public FileHandle FileHandle;
        public long FileSize;

        public NativeArray<byte> Stream;

        public NativeList<ISOBoxType> BoxeTypes;
        public NativeList<int> BoxOffsets;

        public static JobHandle GetContentTable(string path, out ISOReader reader)
        {
            FileInfoResult fileInfo;

            AsyncReadManager.GetFileInfo(path, &fileInfo).JobHandle.Complete();

            reader = new ISOReader()
            {
                FileHandle = AsyncReadManager.OpenFileAsync(path),
                FileSize = fileInfo.FileSize
            };

            var preMDAT = new ReadCommand();
            var postMDAT = new ReadCommand();
            {
                long bufferSize = math.min(1024, fileInfo.FileSize);
                using var stream = new NativeArray<byte>((int)bufferSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                using var topBoxes = new NativeList<ISOBox>(32, Allocator.TempJob);
                using var extendedSizes = new NativeList<ulong>(2, Allocator.TempJob);
             
                long offset = 0;
                int sizeIndex = 0, boxIndex = 0;
                while (offset < fileInfo.FileSize)
                {
                    ReadCommand cmd;
                    cmd.Offset = offset;
                    cmd.Size = math.min(1024, fileInfo.FileSize - offset);
                    cmd.Buffer = stream.GetUnsafeReadOnlyPtr();

                    UnityEngine.Debug.Log($"ReadCommand Offset={cmd.Offset} Size={cmd.Size}");

                    ReadCommandArray readChunkCmdArray;
                    readChunkCmdArray.ReadCommands = &cmd;
                    readChunkCmdArray.CommandCount = 1;

                    ReadHandle readChunkJob = AsyncReadManager.Read(reader.FileHandle, readChunkCmdArray);

                    var extractJob = new ISOExtractAllParentBoxes()
                    {
                        Stream = stream,
                        Boxes = topBoxes,
                        ExtendedSizes = extendedSizes
                    };

                    readChunkJob.JobHandle.Complete();
                    extractJob.Run();

                    while (boxIndex < topBoxes.Length)
                    {
                        var box = topBoxes[boxIndex];
                        var size = box.size > 1 ? box.size : box.size == 1 ?
                            (long)extendedSizes[sizeIndex++] : fileInfo.FileSize - offset;

                        if (box.type == ISOBoxType.MDAT)
                        {
                            preMDAT.Offset = 0;
                            preMDAT.Size = offset;

                            postMDAT.Offset = offset + size;
                            postMDAT.Size = fileInfo.FileSize - postMDAT.Offset;
                        }
                        offset += size;
                        boxIndex++;
                    }
                }
            }

            reader.Stream = new NativeArray<byte>((int)(preMDAT.Size + postMDAT.Size), Allocator.Persistent);
            reader.BoxeTypes = new NativeList<ISOBoxType>(128, Allocator.Persistent);
            reader.BoxOffsets = new NativeList<int>(128, Allocator.Persistent);

            preMDAT.Buffer = reader.Stream.GetUnsafePtr();
            postMDAT.Buffer = (byte*)reader.Stream.GetUnsafePtr() + preMDAT.Size;

            var readCmds = new NativeArray<ReadCommand>(2, Allocator.TempJob);
            readCmds[0] = preMDAT;
            readCmds[1] = postMDAT;

            ReadCommandArray readCmdArray;
            readCmdArray.ReadCommands = (ReadCommand*)readCmds.GetUnsafePtr();
            readCmdArray.CommandCount = 2;

            JobHandle handle = AsyncReadManager.Read(reader.FileHandle, readCmdArray).JobHandle;
            handle = readCmds.Dispose(handle);

            //AsyncReadManager.ReadDeferred(reader.FileHandle, &readCmdArray, handle).JobHandle.Complete();
            //handle = readCmds.Dispose(readMetaJob.JobHandle);

            handle = new ISOGetTableContent()
            {
                Stream = reader.Stream,
                BoxeTypes = reader.BoxeTypes,
                BoxOffsets = reader.BoxOffsets
            }.Schedule(handle);

            return handle;
        }

        public void Dispose()
        {
            FileHandle.Close().Complete();

            Stream.Dispose();
            BoxeTypes.Dispose();
            BoxOffsets.Dispose();
        }

        public struct Container
        {
            public long Offset;
            public long Size;
        }
    }
}
