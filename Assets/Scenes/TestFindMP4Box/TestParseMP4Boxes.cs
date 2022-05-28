using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Video;
using Unity.MediaFramework.Video;
using Unity.MediaFramework.Format.MP4;
using BitStream = Unity.MediaFramework.Video.BitStream;

public unsafe class TestParseMP4Boxes : MonoBehaviour
{
    public VideoClip clip;

    void Start()
    {
        FileInfoResult fileInfo;

        ReadHandle fileInfoHandle = AsyncReadManager.GetFileInfo(clip.originalPath, &fileInfo);

        fileInfoHandle.JobHandle.Complete();

        Debug.Log($"[Path] {clip.originalPath} ({fileInfo.FileState}) Size={fileInfo.FileSize}");

        ReadCommand cmd;
        cmd.Offset = 0;
        cmd.Size = fileInfo.FileSize;
        cmd.Buffer = (byte*)UnsafeUtility.Malloc(cmd.Size, 16, Allocator.TempJob);

        FileHandle fileHandle = AsyncReadManager.OpenFileAsync(clip.originalPath);

        ReadCommandArray readCmdArray;
        readCmdArray.ReadCommands = &cmd;
        readCmdArray.CommandCount = 1;

        ReadHandle readHandle = AsyncReadManager.Read(fileHandle, readCmdArray);

        var searchedBoxes = new NativeList<uint>(16, Allocator.TempJob);
        searchedBoxes.Add((uint)MP4BoxType.FTYP);
        searchedBoxes.Add((uint)MP4BoxType.STTS);
        searchedBoxes.Add((uint)MP4BoxType.MDHD);
        searchedBoxes.Add((uint)MP4BoxType.HDLR);
        searchedBoxes.Add((uint)MP4BoxType.STSD);
        searchedBoxes.Add((uint)MP4BoxType.STCO);
        searchedBoxes.Add((uint)MP4BoxType.STBL);

        var foundBoxes = new NativeList<byte>((int)fileInfo.FileSize, Allocator.TempJob);

        var findBoxesJob = new MP4Parser.FindBoxes()
        {
            Stream = new BitStream()
            {
                buffer = (byte*)cmd.Buffer,
                length = (ulong)fileInfo.FileSize
            },
            SearchedBoxes = searchedBoxes.AsArray(),
            FoundBoxes = foundBoxes
        };

        var parseFTYPJob = new MP4Parser.ParseFTYP()
        {
            Stream = new BitStream(foundBoxes),
            Result = new NativeReference<FTYPBox>(Allocator.TempJob)
        };

        //findBoxesJob.Run();
        //parseFTYPJob.Run();
        //fileHandle.Close(readHandle.JobHandle).Complete();

        var findBoxesJobHandle = findBoxesJob.Schedule(readHandle.JobHandle);
        var parseFTYPJobHandle = parseFTYPJob.Schedule(findBoxesJobHandle);
        var closeJob = fileHandle.Close(parseFTYPJobHandle);

        parseFTYPJobHandle.Complete();
        closeJob.Complete();

        var stream = new BitStream(foundBoxes);
        while (!stream.EndOfStream())
        {
            var box = stream.PeekMP4Box();
            stream.Seek(box.GetExtendedSize(stream));

            DebugTools.Print(box);
        }

        DebugTools.Print(parseFTYPJob.Result.Value);

        parseFTYPJob.Result.Dispose();
        searchedBoxes.Dispose();
        foundBoxes.Dispose();
        readHandle.Dispose();

        for (int i = 0; i < readCmdArray.CommandCount; i++)
            UnsafeUtility.Free(readCmdArray.ReadCommands[i].Buffer, Allocator.TempJob);
    }
}
