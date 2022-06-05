using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.MediaFramework.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.IO.LowLevel.Unsafe;

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

    /// <summary>
    /// Find all parent boxes type and copy them in the buffer
    /// </summary>
    [BurstCompile]
    public unsafe struct AsyncISOExtractAllParentBoxes : IJob
    {
        [ReadOnly] public NativeArray<byte> Stream;

        public NativeReference<ReadCommandArray> Commands;

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
    public unsafe struct AsyncISOSearchAndReadMetaContent : IJob
    {
        public long FileSize;

        [ReadOnly] public NativeArray<byte> Stream;
        [ReadOnly] public NativeArray<ISOBoxType> Search;

        public NativeReference<ReadCommandArray> Commands;

        public NativeList<ISOBoxType> BoxTypes;
        public NativeList<int> BoxOffsets;
        public NativeList<byte> RawData;

        public void Execute()
        {
            // Nothing was read, we can just exit 
            if (Commands.Value.CommandCount == 0)
                return;

            long length = (uint)Stream.Length;

            long position = 0;
            var buffer = (byte*)Stream.GetUnsafeReadOnlyPtr();

            do
            {
                var box = ISOUtility.GetISOBox(buffer + position);

                // Check first if the current box can be a parent.
                // If yes, let's peek and check if the children type is valid.
                // It is necessary to do that because some childrens are optional so,
                // it is possible that a box can be a parent, but is currently not.
                bool hasChildren =
                    ISOUtility.CanBeParent(box.type) &&
                    ISOUtility.HasValidISOBoxType(buffer + ISOBox.Stride + position);

                long size = box.size >= ISOBox.Stride ? box.size : 
                    box.size == 1 ? (long)BigEndian.GetUInt64(buffer + position + ISOBox.Stride) : FileSize - Commands.Value.ReadCommands[0].Offset - position;


                // Even if too big we can still copy. FIX THIS
                if (size + position >= length && !hasChildren)
                {
                    SetNextReadCommand(IsBoxNeeded(box.type) ? position : size);
                    return;
                }

                if (IsBoxNeeded(box.type))
                {
                    BoxTypes.Add(box.type);
                    BoxOffsets.Add(RawData.Length);

                    // TODO: Should be safe as you should not ask for container this big but we should check anyway
                    RawData.AddRange(buffer, (int)box.size);
                }

                position += hasChildren ? ISOBox.Stride : box.size;
            }
            while (position + ISOBox.Stride < length);

            SetNextReadCommand(position);
        }

        public void SetNextReadCommand(long offset)
        {
            var readCommand = Commands.Value.ReadCommands[0];

            readCommand.Offset += offset;

            if (readCommand.Offset + ISOBox.Stride < FileSize)
            {
                // We still have buffer to read
                readCommand.Size = math.min(readCommand.Size, FileSize - readCommand.Offset);
                Commands.Value.ReadCommands[0] = readCommand;
            }
            else
            {
                // We finished reading the file
                // We set CommandCount to 0 so any scheduled job end early
                EndRead();
            }
        }

        public void EndRead()
        {
            var readhandler = Commands.Value;
            readhandler.CommandCount = 0;
            Commands.Value = readhandler;
        }

        bool IsBoxNeeded(ISOBoxType type)
        {
            foreach (var boxType in BoxTypes)
                if(boxType == type)
                    return true;

            return false;
        }
    }

    /// <summary>
    /// Find all boxes type and the ptr to the data
    /// </summary>
    [BurstCompile]
    public unsafe struct AsyncISOParseContent : IJob
    {
        public long FileSize;

        [ReadOnly] public NativeArray<byte> Stream;
        [ReadOnly] public NativeArray<ISOBoxType> Search;

        public NativeReference<FixedString512Bytes> Error;

        public NativeReference<ReadCommandArray> Commands;

        public NativeReference<ISOHeader> Header;
        public NativeList<ISOBoxType> BoxTypes;
        public NativeList<int> BoxOffsets;
        public NativeList<byte> RawData;

        public void Execute()
        {
            // Nothing was read, we can just exit 
            if (Commands.Value.CommandCount == 0 || Error.Value.IsEmpty)
                return;

            ref var error = ref UnsafeUtility.AsRef<FixedString512Bytes>(Error.GetUnsafePtr());

            var length = Stream.Length;
            var buffer = (byte*)Stream.GetUnsafeReadOnlyPtr();
            ref var header = ref UnsafeUtility.AsRef<ISOHeader>(Header.GetUnsafePtr());

            long position = 0;
            while (position + ISOBox.Stride < length)
            {
                var box = ISOUtility.GetISOBox(buffer + position);

                long size = box.size >= ISOBox.Stride ? box.size :
                            box.size == 1 ? (long)BigEndian.GetUInt64(buffer + position + ISOBox.Stride) : 
                                            FileSize - Commands.Value.ReadCommands[0].Offset - position;

                switch (box.type)
                {
                    case ISOBoxType.MVHD:
                        if (header.MVHD.IsValid())
                        {   
                            error = $"";
                            return;
                        }

                        header.MVHD = new MVHDBox(buffer);
                        position += size;
                        break;
                    case ISOBoxType.FREE:
                    case ISOBoxType.MDAT:
                        position += size;
                        break;
                    default:
                        // Check first if the current box can be a parent.
                        // If yes, let's peek and check if the children type is valid.
                        // It is necessary to do that because some childrens are optional so,
                        // it is possible that a box can be a parent, but is currently not.
                        bool hasChildren =
                            ISOUtility.CanBeParent(box.type) &&
                            ISOUtility.HasValidISOBoxType(buffer + ISOBox.Stride + position);

                        if (IsBoxNeeded(box.type))
                        {
                            BoxTypes.Add(box.type);
                            BoxOffsets.Add(RawData.Length);

                            // TODO: Should be safe as you should not ask for container this big but we should check anyway
                            RawData.AddRange(buffer, (int)box.size);
                        }
                        position += hasChildren ? ISOBox.Stride : size;
                        break;
                }
            }

            SetNextReadCommand(position);
        }

        public void SetNextReadCommand(long offset)
        {
            ref var commands = ref UnsafeUtility.AsRef<ReadCommandArray>(Commands.GetUnsafePtr());

            commands.ReadCommands[0].Offset += offset;

            if (commands.ReadCommands[0].Offset + ISOBox.Stride < FileSize)
            {
                // We still have buffer to read
                commands.ReadCommands[0].Size = math.min(commands.ReadCommands[0].Size, FileSize - commands.ReadCommands[0].Offset);
            }
            else
            {
                // We finished reading the file
                // We set CommandCount to 0 so any scheduled job end early
                commands.CommandCount = 0;
            }
        }

        bool IsBoxNeeded(ISOBoxType type)
        {
            foreach (var boxType in BoxTypes)
                if (boxType == type)
                    return true;

            return false;
        }
    }
}
