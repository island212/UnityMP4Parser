using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.MediaFramework.Format.ISOBMFF;
using Unity.MediaFramework.Video;

namespace Unity.MediaFramework.Format.MP4
{
    // Useful link
    // https://xhelmboyx.tripod.com/formats/mp4-layout.txt
    // https://developer.apple.com/library/archive/documentation/QuickTime/QTFF/QTFFChap1/qtff1.html#//apple_ref/doc/uid/TP40000939-CH203-BBCGDDDF
    // https://b.goeswhere.com/ISO_IEC_14496-12_2015.pdf

    public unsafe static class MP4Parser
    {
        public static MP4Handle Create(string path, int chunkSize)
        { 
            FileInfoResult fileInfo;

            AsyncReadManager.GetFileInfo(path, &fileInfo).JobHandle.Complete();

            var handle = new MP4Handle()
            {
                FileHandle = AsyncReadManager.OpenFileAsync(path),
                FileSize = fileInfo.FileSize,
                Boxes = new NativeList<ISOBox>(128, Allocator.Persistent),
                ExtendedSizes = new NativeList<ulong>(4, Allocator.Persistent)
            };

            long bufferSize = math.min(chunkSize, fileInfo.FileSize);
            using var fileBuffer = new NativeArray<byte>((int)bufferSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            long offset = 0;
            int sizeIndex = 0, boxIndex = 0;

            while (offset < fileInfo.FileSize)
            {
                ReadCommand cmd;
                cmd.Offset = offset;
                cmd.Size = math.min(chunkSize, fileInfo.FileSize - offset);
                cmd.Buffer = fileBuffer.GetUnsafeReadOnlyPtr();

                UnityEngine.Debug.Log($"ReadCommand Offset={cmd.Offset} Size={cmd.Size}");

                ReadHandle readHandle = AsyncReadManagerEx.Read(handle.FileHandle, cmd);

                var extractJob = new ISOExtractAllParentBoxes()
                {
                    Stream = new BitStream((byte*)cmd.Buffer, (int)cmd.Size),
                    Boxes = handle.Boxes,
                    ExtendedSizes = handle.ExtendedSizes
                };

                readHandle.JobHandle.Complete();
                extractJob.Run();

                while (boxIndex < handle.Boxes.Length)
                {
                    var box = handle.Boxes[boxIndex];
                    offset += box.size > 1 ? box.size : box.size == 1 ? 
                        (long)handle.ExtendedSizes[sizeIndex++] : fileInfo.FileSize - offset;

                    boxIndex++;
                }
            }

            return handle;
        }

        public static JobHandle Parse(in MP4Handle handle)
        {
            JobHandle jobHandle = default;



            return jobHandle;
        }
    }

    public struct MP4Handle : IDisposable
    {
        public FileHandle FileHandle;
        public long FileSize;

        public NativeList<ISOBox> Boxes;
        public NativeList<ulong> ExtendedSizes;

        public void Dispose()
        {
            FileHandle.Close().Complete();

            Boxes.Dispose();
            ExtendedSizes.Dispose();
        }
    }

    public enum MP4Brand : uint
    {
        AVC1 = 0x61766331,
        ISO2 = 0x69736f32,
        ISO3 = 0x69736f33,
        ISO4 = 0x69736f34,
        ISO5 = 0x69736f35,
        ISO6 = 0x69736f36,
        ISO7 = 0x69736f37,
        ISO8 = 0x69736f38,
        ISO9 = 0x69736f39,
        ISOM = 0x69736f6d,
        MP71 = 0x6d703731,
    }
}
