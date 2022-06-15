using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Video;
using Unity.MediaFramework.Video;
using Unity.MediaFramework.Format.ISOBMFF;
using Unity.MediaFramework.LowLevel.Unsafe;

public unsafe class TestFileReadingInJob : MonoBehaviour
{
    public VideoClip clip;

    //[BurstCompile]
    public unsafe struct ExtractAllParentBoxesIO : IJob
    {
        public FileHandle fileHandle;

        public NativeReference<ISOBox> Box;

        public void Execute()
        {
            ReadCommand cmd;
            cmd.Offset = 0;
            cmd.Size = 2048;
            cmd.Buffer = (byte*)UnsafeUtility.Malloc(cmd.Size, 16, Allocator.TempJob);

            ReadCommandArray readCmdArray;
            readCmdArray.ReadCommands = &cmd;
            readCmdArray.CommandCount = 1;

            ReadHandle readHandle = AsyncReadManager.Read(fileHandle, readCmdArray);
            while (!readHandle.JobHandle.IsCompleted) { }

            var buffer = (byte*)readCmdArray.ReadCommands[0].Buffer;
            Box.Value = ISOBox.Parse(buffer);

            readHandle.Dispose();

            for (int i = 0; i < readCmdArray.CommandCount; i++)
                UnsafeUtility.Free(readCmdArray.ReadCommands[i].Buffer, Allocator.TempJob);
        }
    }

    void Start()
    {
        FileInfoResult fileInfo;

        ReadHandle fileInfoHandle = AsyncReadManager.GetFileInfo(clip.originalPath, &fileInfo);

        fileInfoHandle.JobHandle.Complete();

        Debug.Log($"[Path] {clip.originalPath} ({fileInfo.FileState}) Size={fileInfo.FileSize}");

        FileHandle fileHandle = AsyncReadManager.OpenFileAsync(clip.originalPath);

        var extractJob = new ExtractAllParentBoxesIO()
        {
            fileHandle = fileHandle,
            Box = new NativeReference<ISOBox>(Allocator.TempJob)
        };

        extractJob.Run();
        fileHandle.Close().Complete();

        DebugTools.Print(extractJob.Box.Value);

        extractJob.Box.Dispose();
    }
}
