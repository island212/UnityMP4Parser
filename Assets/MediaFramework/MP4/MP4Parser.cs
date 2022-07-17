using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel.Format.ISOBMFF;

namespace Unity.MediaFramework.Format.MP4
{
    // Useful link
    // https://xhelmboyx.tripod.com/formats/mp4-layout.txt
    // https://developer.apple.com/library/archive/documentation/QuickTime/QTFF/QTFFChap1/qtff1.html#//apple_ref/doc/uid/TP40000939-CH203-BBCGDDDF
    // https://b.goeswhere.com/ISO_IEC_14496-12_2015.pdf

    public struct MP4Header
    {
        public NativeReference<UnsafeByteArray> Header;
        public NativeReference<ISOTable> Table;

        public JobHandle Dispose(JobHandle depends = default)
        {
            if (!Header.IsCreated || !Table.IsCreated)
                return depends;

            depends = new DisposeJob { 
                Header = Header,
                Table = Table,
            }.Schedule(depends);

            depends = Header.Dispose(depends);
            depends = Table.Dispose(depends);
            return depends;
        }

        public struct DisposeJob : IJob
        {
            public NativeReference<UnsafeByteArray> Header;
            public NativeReference<ISOTable> Table;

            public unsafe void Execute()
            {
                UnsafeUtility.AsRef<UnsafeByteArray>(Header.GetUnsafePtr()).Dispose();
                UnsafeUtility.AsRef<ISOTable>(Table.GetUnsafePtr()).Tracks.Dispose();
            }
        }
    }

    public unsafe static class MP4Parser
    {
        public static JobHandle Parse(string path, out MP4Header mp4)
        {
            mp4 = new MP4Header();
            var handle = AsyncISOReader.Read(path, out mp4.Header);

            mp4.Table = new NativeReference<ISOTable>(Allocator.Persistent);

            var createTableJob = new CreateISOTable
            {
                Header = mp4.Header,
                Table = mp4.Table,
            };

            return createTableJob.Schedule(handle);
        }
    }
}
