using Unity.IO.LowLevel.Unsafe;

namespace Unity.MediaFramework.Video
{
    public unsafe static class AsyncReadManagerEx
    {
        public static ReadHandle Read(in FileHandle fileHandle, ReadCommand cmd)
        {
            ReadCommandArray readCmdArray;
            readCmdArray.ReadCommands = &cmd;
            readCmdArray.CommandCount = 1;

            return AsyncReadManager.Read(fileHandle, readCmdArray);
        }
    }
}
