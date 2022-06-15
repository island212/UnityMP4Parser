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
                var box = ISOBox.Parse(buffer + position);
                Boxes.Add(box);

                if (box.Size == 1)
                {
                    ExtendedSizes.Add(BigEndian.ReadUInt64(buffer + ISOBox.ByteNeeded + position));
                    return;
                }

                position = math.min(box.Size + position, Stream.Length - 1);
            }
            while (Stream.Length - position - 1 >= ISOBox.ByteNeeded);
        }
    }

    /// <summary>
    /// Find all boxes type and the ptr to the data
    /// </summary>
    [BurstCompile(Debug = true)]
    public unsafe struct AsyncISOParseContent : IJob
    {
        public long FileSize;

        [ReadOnly] public NativeArray<byte> Stream;

        public NativeReference<ErrorValue> Error;
        public NativeReference<ReadCommandArray> Commands;

        public NativeReference<HeaderValue> Header;
        public NativeList<TrackValue> Tracks;

        public void Execute()
        {
            // Nothing was read or there is an error, we can just exit 
            if (Commands.Value.CommandCount == 0 || Error.Value.Type != ErrorType.None)
                return;

            ref var header = ref UnsafeUtility.AsRef<HeaderValue>(Header.GetUnsafePtr());

            int trackIdx = -1;
            var trackPtr = (TrackValue*)Tracks.GetUnsafePtr();

            // Like that, we always know we will have at least one ISOBox
            var length = Commands.Value.ReadCommands[0].Size - ISOBox.ByteNeeded;
            var buffer = (byte*)Stream.GetUnsafeReadOnlyPtr();

            long position = 0;
            while (position < length)
            {
                var box = ISOBox.Parse(buffer + position);

                if(box.Size < ISOBox.ByteNeeded && box.Size > 1)
                    { SetError(ErrorType.InvalidSize, $"Found a {box.Type} box with an invalid size of {box.Size}"); return; }

                long size = box.Size >= ISOBox.ByteNeeded ? box.Size :
                            box.Size == 1 ? (long)BigEndian.ReadUInt64(buffer + position + ISOBox.ByteNeeded) : 
                                            FileSize - Commands.Value.ReadCommands[0].Offset - position;

                switch (box.Type)
                {
                    case ISOBoxType.MVHD:
                        if (length - position + ISOBox.ByteNeeded < size)
                            { SetNextReadCommand(position); return; }

                        if (header.duration != 0)
                            { SetError(ErrorType.DuplicateBox, "Found a second MVHD box in the file"); return; }

                        var version = ISOFullBox.GetVersion(buffer + position);

                        if(MVHDBox.GetSize(version) != size)
                            { SetError(ErrorType.InvalidSize, $"Found a MVHD box with an invalid size of {box.Size} for version {version}"); return; }

                        header.timescale = MVHDBox.GetTimeScale(version, buffer + position);
                        header.duration = MVHDBox.GetDuration(version, buffer + position);
                        position += size;
                        break;
                    case ISOBoxType.TRAK:
                        trackIdx = Tracks.Length;
                        Tracks.Add(new TrackValue());
                        position += ISOBox.ByteNeeded;
                        break;
                    case ISOBoxType.TKHD:
                        if (length - position + ISOBox.ByteNeeded < size)
                            { SetNextReadCommand(position); return; }

                        if (trackIdx < 0 || trackIdx >= Tracks.Length)
                            { SetError(ErrorType.MissingBox, "Found a TKHD box without detecting a TRAK box"); return; }

                        ref var track = ref UnsafeUtility.AsRef<TrackValue>(trackPtr + trackIdx);

                        if (track.duration != 0)
                            { SetError(ErrorType.DuplicateBox, $"Found a second TKHD box in the same TRAK box #{trackIdx}"); return; }

                        var tkhd = TKHDBox.Parse(buffer + position);
                        track.duration = tkhd.Duration;
                        position += size;
                        break;
                    default:
                        position += size;
                        break;
                }
            }

            SetNextReadCommand(position);
        }

        public void SetNextReadCommand(long offset)
        {
            ref var commands = ref UnsafeUtility.AsRef<ReadCommandArray>(Commands.GetUnsafePtr());

            commands.ReadCommands[0].Offset += offset;

            if (commands.ReadCommands[0].Offset + ISOBox.ByteNeeded < FileSize)
            {
                // We still have buffer to read
                commands.ReadCommands[0].Size = math.min(Stream.Length, FileSize - commands.ReadCommands[0].Offset);
            }
            else
            {
                // We finished reading the file
                // We set CommandCount to 0 so any scheduled job end early
                commands.CommandCount = 0;
            }
        }

        public void SetError(ErrorType type, in FixedString128Bytes message)
        {
            ref var error = ref UnsafeUtility.AsRef<ErrorValue>(Error.GetUnsafePtr());

            error.Type = type;
            error.Message = message;

            ref var commands = ref UnsafeUtility.AsRef<ReadCommandArray>(Commands.GetUnsafePtr());
            commands.CommandCount = 0;
        }

        public enum ErrorType
        { 
            None = 0,
            MissingBox,
            DuplicateBox,
            InvalidSize,
        }

        public struct ErrorValue
        {
            public ErrorType Type;
            public FixedString128Bytes Message;
        }

        public struct HeaderValue
        {
            public uint timescale;
            public ulong duration;
        }

        public struct TrackValue
        {
            public ulong duration;
        }
    }
}
