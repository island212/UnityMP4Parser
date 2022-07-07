using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.MediaFramework.LowLevel.Unsafe
{
    public static class NativeListBufferExtensions
    {
        public const int BytesPerCacheLine = JobsUtility.CacheLineSize / sizeof(byte);

        public unsafe static bool TryAllocateBuffer(this ref NativeList<byte> list, in long size, out byte* ptr)
        {
            if (list.Capacity - list.Length >= size)
            {
                ptr = (byte*)list.GetUnsafePtr() + list.Length;
                // If the size is under one cache line, it could cause false sharing
                // https://en.wikipedia.org/wiki/False_sharing
                list.Length += math.max((int)size, BytesPerCacheLine);
                return true;
            }

            ptr = null;
            return false;
        }
    }
}
