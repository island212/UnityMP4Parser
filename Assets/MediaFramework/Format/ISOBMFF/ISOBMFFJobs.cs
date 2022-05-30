using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.MediaFramework.Video;

namespace Unity.MediaFramework.Format.ISOBMFF
{
    /// <summary>
    /// Find all parent boxes type and copy them in the buffer
    /// </summary>
    [BurstCompile]
    public unsafe struct ISOExtractAllParentBoxes : IJob
    {
        public BitStream Stream;

        public NativeList<ISOBox> Boxes;
        public NativeList<ulong> ExtendedSizes;

        public void Execute()
        {
            uint seek = 0;
            do
            {
                // We can safely cast seek to int because Remains will never be over int.MaxValue
                Stream.Seek((int)seek);

                var box = Stream.PeekISOBox();
                Boxes.Add(box);

                if (box.size == 1)
                    ExtendedSizes.Add(Stream.PeekUInt64(8));

                // Check first if the current box can be a parent.
                // If yes, let's peek and check if the children type is valid.
                // It is necessary to do that because some childrens are optional so,
                // it is possible that a box can be a parent, but is currently not.
                //seek = box.type.CanBeParent() && Stream.HasValidISOBoxType(offset) ? offset : (long)size;
                seek = box.size;
            }
            while (seek > 1 && Stream.Remains() > seek + 12);
        }
    }

    /// <summary>
    /// Find all boxes type and copy them in the buffer
    /// </summary>
    [BurstCompile]
    public unsafe struct ISOExtractAllBoxes : IJob
    {
        public BitStream Stream;

        public NativeList<ISOBox> Boxes;
        public NativeList<ulong> ExtendedSizes;

        public void Execute()
        {
            uint seek = 0;
            do
            {
                // We can safely cast seek to int because Remains will never be over int.MaxValue
                Stream.Seek((int)seek);

                var box = Stream.PeekISOBox();
                Boxes.Add(box);

                int offset = 8;
                if (box.size == 1)
                {
                    ExtendedSizes.Add(Stream.PeekUInt64(8));
                    offset += 4;
                }

                // Check first if the current box can be a parent.
                // If yes, let's peek and check if the children type is valid.
                // It is necessary to do that because some childrens are optional so,
                // it is possible that a box can be a parent, but is currently not.
                seek = box.type.CanBeParent() && Stream.HasValidISOBoxType(offset) ? (uint)offset : box.size;
            }
            while (seek > 1 && Stream.Remains() > seek + 12);
        }
    }
}
