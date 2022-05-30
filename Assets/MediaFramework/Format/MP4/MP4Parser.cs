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
        public static void Parse(string path)
        { 
            FileInfoResult fileInfo;
            AsyncReadManager.GetFileInfo(path, &fileInfo).JobHandle.Complete();

            UnityEngine.Debug.Log($"Opening file {path} ({fileInfo.FileState}) Size={fileInfo.FileSize}");

            FileHandle fileHandle = AsyncReadManager.OpenFileAsync(path);

            long bufferSize = 2048;
            using var fileBuffer = new NativeArray<byte>((int)bufferSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            using var boxes = new NativeList<ISOBox>(128, Allocator.TempJob);
            using var extendedSize = new NativeList<ulong>(4, Allocator.TempJob);

            long offset = 0;
            int sizeIndex = 0, boxIndex = 0;

            // Until we find MDAT continue seeking
            while (offset < fileInfo.FileSize)
            {
                ReadCommand cmd;
                cmd.Offset = offset;
                cmd.Size = math.min(bufferSize, fileInfo.FileSize - offset);
                cmd.Buffer = fileBuffer.GetUnsafeReadOnlyPtr();

                UnityEngine.Debug.Log($"ReadCommand Offset={cmd.Offset} Size={cmd.Size}");

                ReadHandle readHandle = AsyncReadManagerEx.Read(fileHandle, cmd);

                var extractJob = new ISOExtractAllParentBoxes()
                {
                    Stream = new BitStream((byte*)cmd.Buffer, (int)cmd.Size),
                    Boxes = boxes,
                    ExtendedSizes = extendedSize
                };

                readHandle.JobHandle.Complete();
                extractJob.Run();

                while (boxIndex < boxes.Length)
                {
                    var box = boxes[boxIndex];
                    DebugTools.Print(box);

                    switch (box.type)
                    {
                        case ISOBoxType.MOOV:
                            //Start job to parse MOOV header here
                            break;
                    }

                    if(box.size == 1)
                        UnityEngine.Debug.Log($"Extended={extendedSize[sizeIndex]}");
                    if(box.size == 0)
                        UnityEngine.Debug.Log($"EOF={fileInfo.FileSize - offset}");

                    offset += box.size > 1 ? box.size : box.size == 1 ? (long)extendedSize[sizeIndex++] : fileInfo.FileSize - offset;
                    boxIndex++;
                }
            }       

            fileHandle.Close().Complete();
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
