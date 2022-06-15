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
    public unsafe struct AsyncISOReader
    {
        public FileInfoResult FileInfo;

        public NativeList<byte> Stream;

        public NativeList<ISOBoxType> BoxeTypes;
        public NativeList<int> BoxOffsets;

        public static JobHandle GetContent(string path, NativeArray<ISOBoxType> search, out AsyncISOReader reader)
        {
            reader = new AsyncISOReader();

            JobHandle handle = default;

            FileInfoResult fileInfo;
            AsyncReadManager.GetFileInfo(path, &fileInfo).JobHandle.Complete();
            reader.FileInfo = fileInfo;

            if (fileInfo.FileState == FileState.Absent)
                return handle;



            return handle;
        }
    }
}
