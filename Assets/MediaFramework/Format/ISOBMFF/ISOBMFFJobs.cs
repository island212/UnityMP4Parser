using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.MediaFramework.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.MediaFramework.Format.ISOBMFF
{
    /// <summary>
    /// Find all parent boxes type and copy them in the buffer
    /// </summary>
    [BurstCompile]
    public unsafe struct ISOExtractAllParentBoxes : IJob
    {
        [ReadOnly] public NativeArray<byte> Stream;

        public NativeList<ISOBox> Boxes;
        public NativeList<ulong> ExtendedSizes;

        public void Execute()
        {
            long position = 0;
            var buffer = (byte*)Stream.GetUnsafeReadOnlyPtr();

            do
            {
                var box = ISOUtility.GetISOBox(buffer + position);
                Boxes.Add(box);

                if (box.size == 1)
                {
                    ExtendedSizes.Add(BigEndian.GetUInt64(buffer + ISOBox.Stride + position));
                    return;
                }

                position = math.min(box.size + position, Stream.Length - 1);
            }
            while (Stream.Length - position - 1 >= ISOBox.Stride);
        }
    }

    /// <summary>
    /// Find all boxes type and the ptr to the data
    /// </summary>
    [BurstCompile]
    public unsafe struct ISOGetTableContent : IJob
    {
        [ReadOnly] public NativeArray<byte> Stream;

        public NativeList<ISOBoxType> BoxeTypes;
        public NativeList<int> BoxOffsets;

        public void Execute()
        {
            int position = 0;
            var buffer = (byte*)Stream.GetUnsafeReadOnlyPtr();

            do
            {
                var box = ISOUtility.GetISOBox(buffer + position);
                // Check if size == 0 if at the end of the file
                // Don't need to check for 1 as too big to fit in the job
                box.size = box.size >= ISOBox.Stride ? box.size : (uint)(Stream.Length - position);

                BoxeTypes.Add(box.type);
                BoxOffsets.Add(position);

                // Check first if the current box can be a parent.
                // If yes, let's peek and check if the children type is valid.
                // It is necessary to do that because some childrens are optional so,
                // it is possible that a box can be a parent, but is currently not.
                int seek = ISOUtility.CanBeParent(box.type) && 
                           ISOUtility.HasValidISOBoxType(buffer + ISOBox.Stride + position) 
                           ? ISOBox.Stride : (int)box.size;

                position = math.min(seek + position, Stream.Length - 1);
            }
            while (Stream.Length - position - 1 >= ISOBox.Stride);
        }
    }
}
