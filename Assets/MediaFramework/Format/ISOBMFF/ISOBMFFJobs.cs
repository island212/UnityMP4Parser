using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.MediaFramework.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.IO.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using Unity.Jobs.LowLevel.Unsafe;
using System.Threading;

namespace Unity.MediaFramework.Format.ISOBMFF
{
    public enum ErrorType
    {
        None,
        MissingBox,
        DuplicateBox,
        InvalidSize,
    }

    public struct BoxChunk
    {
        public long Offset;
        public long Size;
    }

    public unsafe struct UnsafeRawArray
    {
        public Allocator Allocator;

        public int Length;
        public byte* Ptr;

        public UnsafeRawArray(byte* ptr, int length, Allocator allocator)
        {
            Allocator = allocator;
            Length = length;
            Ptr = ptr;
        }
    }

    public struct ErrorMessage
    {
        public ErrorType Type;
        public FixedString128Bytes Message;
    }

    public struct IOContext
    {
        public long FileSize;
        public ReadCommandArray Commands;

        public int JobCount => Commands.CommandCount;

        public unsafe int AddReadCommandJob(ref ReadCommand command)
        {
            command.Size = math.min(command.Size, FileSize - command.Offset);

            int index = Commands.CommandCount++;
            Commands.ReadCommands[index] = command;
            return index;
        }
    }
    public struct JobContext
    {
        public bool HasError => Error.Type != ErrorType.None;

        public ReadCommand ReadCommand;

        public BoxChunk MDAT;
        public BoxChunk MOOV;

        public ErrorMessage Error;
    }

    public unsafe struct ISOTrack
    {
        public ISOHandler Handler;
        public TrackHeaderBox TrackHeader;
        public MediaHeaderBox MediaHeader;

        public UnsafeRawArray SampleDescriptionRaw;
        public UnsafeRawArray TimeToSampleRaw;
        public UnsafeRawArray SyncSampleRaw;
    }


    [BurstCompile]
    public struct AsyncTryParseISOBMFFContent : IJob
    {
        [ReadOnly] public NativeArray<byte> Stream;

        public NativeReference<IOContext> IOContext;
        public NativeReference<JobContext> Context;
        public NativeList<byte> MemCopyBuffer;

        public NativeList<ISOTrack> Tracks;

        public unsafe void Execute()
        {
            if (Context.Value.HasError)
                return;

            ref var ioContext = ref UnsafeUtility.AsRef<IOContext>(IOContext.GetUnsafePtrWithoutChecks());

            ioContext.Commands.CommandCount = 0;

            if (Context.Value.ReadCommand.Size == 0)
                return;

            ref var context = ref UnsafeUtility.AsRef<JobContext>(Context.GetUnsafePtrWithoutChecks());

            long position = 0;
            if (ExecuteMainLoop(ref ioContext, ref context, ref position))
            {
                context.ReadCommand.Offset += position;
                ioContext.AddReadCommandJob(ref context.ReadCommand);
            }
            else
            {
                context.ReadCommand.Size = 0;
            }
        }

        public unsafe bool ExecuteMainLoop(ref IOContext ioContext, ref JobContext context, ref long position)
        {
            var trackIdx = Tracks.Length - 1;
            var buffer = (byte*)Stream.GetUnsafeReadOnlyPtr();

            while (position + ISOBox.ByteNeeded < context.ReadCommand.Size)
            {
                var box = ISOBox.Parse(buffer + position);

                //if(box.Size < ISOBox.ByteNeeded && box.Size > 1)
                //    { SetError(ErrorType.InvalidSize, $"Found a {box.Type} box with an invalid size of {box.Size}"); return; }

                long size = box.Size >= ISOBox.ByteNeeded ? box.Size :
                            box.Size == 1 ? (long)BigEndian.ReadUInt64(buffer + position + ISOBox.ByteNeeded) :
                                            ioContext.FileSize - context.ReadCommand.Offset - position;

                switch (box.Type)
                {
                    case ISOBoxType.STBL:
                    case ISOBoxType.MINF:
                    case ISOBoxType.MDIA:
                        size = ISOBox.ByteNeeded;
                        break;
                    case ISOBoxType.MOOV:
                        context.MOOV = new BoxChunk
                        {
                            Offset = context.ReadCommand.Offset + position,
                            Size = size,
                        };
                        size = ISOBox.ByteNeeded;
                        break;
                    case ISOBoxType.TRAK:
                        trackIdx = Tracks.Length;
                        Tracks.Add(new ISOTrack());
                        size = ISOBox.ByteNeeded;
                        break;
                    case ISOBoxType.HDLR:
                        if (context.ReadCommand.Size - position < size)
                            return true;

                        Tracks.ElementAt(trackIdx).Handler = HandlerBox.GetHandlerType(buffer + position);
                        break;
                    case ISOBoxType.TKHD:
                        if (context.ReadCommand.Size - position < size)
                            return true;

                        Tracks.ElementAt(trackIdx).TrackHeader = TrackHeaderBox.Parse(buffer + position);
                        break;
                    case ISOBoxType.MDHD:
                        if (context.ReadCommand.Size - position < size)
                            return true;

                        Tracks.ElementAt(trackIdx).MediaHeader = MediaHeaderBox.Parse(buffer + position);
                        break;
                    case ISOBoxType.STSD:
                        ref var sampleDes = ref Tracks.ElementAt(trackIdx).SampleDescriptionRaw;
                        {
                            if (!MemCopyBuffer.TryAllocateBuffer(size, out byte* ptr))
                            {
                                ptr = (byte*)UnsafeUtility.Malloc(size, 1, Allocator.Persistent);
                                sampleDes = new UnsafeRawArray(ptr, size, Allocator.Persistent);
                            }
                            else
                                sampleDes = new UnsafeRawArray(ptr, size, Allocator.None);
                        }
                        MemCopy(ref ioContext, context.ReadCommand, sampleDes.Ptr, buffer, position, size);
                        break;
                    case ISOBoxType.STTS:
                        ref var timeToSample = ref Tracks.ElementAt(trackIdx).TimeToSampleRaw;
                        {
                            if (!MemCopyBuffer.TryAllocateBuffer(size, out byte* ptr))
                            {
                                ptr = (byte*)UnsafeUtility.Malloc(size, 1, Allocator.Persistent);
                                timeToSample = new UnsafeRawArray(ptr, size, Allocator.Persistent);
                            }
                            else
                                timeToSample = new UnsafeRawArray(ptr, size, Allocator.None);
                        }
                        MemCopy(ref ioContext, context.ReadCommand, timeToSample.Ptr, buffer, position, size);
                        break;
                    case ISOBoxType.STSS:
                        ref var syncSample = ref Tracks.ElementAt(trackIdx).SyncSampleRaw;
                        {
                            if (!MemCopyBuffer.TryAllocateBuffer(size, out byte* ptr))
                            {
                                ptr = (byte*)UnsafeUtility.Malloc(size, 1, Allocator.Persistent);
                                syncSample = new UnsafeRawArray(ptr, size, Allocator.Persistent);
                            }
                            else
                                syncSample = new UnsafeRawArray(ptr, size, Allocator.None);
                        }
                        MemCopy(ref ioContext, context.ReadCommand, syncSample.Ptr, buffer, position, size);
                        break;
                    case ISOBoxType.MDAT:
                        context.MDAT = new BoxChunk
                        {
                            Offset = context.ReadCommand.Offset + position,
                            Size = size,
                        };

                        // The MOOV box has already been parsed, so we can stop here
                        if (context.MOOV.Size != 0)
                            return false;

                        break;
                }

                position += size;
            }

            return context.ReadCommand.Offset + position < ioContext.FileSize;
        }

        public unsafe static void MemCopy(ref IOContext ioContext, in ReadCommand readCommand, in byte* destination, in byte* stream, in long position, in long size)
        {
            long buffered = math.min(size, readCommand.Size - position);
            UnsafeUtility.MemCpy(destination, stream + position, buffered);
            if (buffered < size)
            {
                var command = new ReadCommand
                {
                    Buffer = destination + buffered,
                    Offset = readCommand.Offset + position + buffered,
                    Size = size - buffered
                };

                ioContext.AddReadCommandJob(ref command);
            }
        }

        public unsafe static void ThrowError(ref IOContext ioContext, ref JobContext context, in ErrorType type, in FixedString128Bytes message)
        {
            context.Error = new()
            {
                Message = message,
                Type = type
            };

            ioContext.Commands.CommandCount = 0;
        }
    }

    public struct ConvertISOTrackToMediaAttributesJob : IJobParallelFor
    {
        [ReadOnly] public NativeList<ISOTrack> Tracks;

        [WriteOnly] public NativeList<VideoTrack>.ParallelWriter VideoTracks;
        [WriteOnly] public NativeList<AudioTrack>.ParallelWriter AudioTracks;

        [WriteOnly] public NativeArray<byte> TableBuffer;

        public long TableBufferLength;

        public void Execute(int index)
        {
            ref var isoTrack = ref Tracks.ElementAt(index);
            switch (isoTrack.Handler)
            {
                case ISOHandler.VIDE: ConvertVideoTrack(ref isoTrack); break;
                case ISOHandler.SOUN:
                    break;
            }
        }

        public unsafe void ConvertVideoTrack(ref ISOTrack isoTrack)
        {
            var videoTrack = new VideoTrack
            {
                TrackID = isoTrack.TrackHeader.TrackID,
                TimeScale = isoTrack.MediaHeader.Timescale,
                Duration = isoTrack.MediaHeader.Duration,
                Width = (int)isoTrack.TrackHeader.Width.ConvertDouble(),
                Height = (int)isoTrack.TrackHeader.Height.ConvertDouble(),
            };

            var tablePtr = (byte*)TableBuffer.GetUnsafePtr();
            {
                ref long length = ref isoTrack.TimeToSampleRaw.Length;
                long idx = Interlocked.Add(ref TableBufferLength, length) - length;

                var buffer = (SampleGoup*)(tablePtr + idx);
                for (; idx < length; idx+=8, buffer++)
                {
                    *buffer = new SampleGoup
                    {
                        Count = BigEndian.ReadUInt32(tablePtr + idx),
                        Delta = BigEndian.ReadUInt32(tablePtr + idx + 4)
                    };
                }

                videoTrack.TimeToSampleTable = new TimeToSampleTable
                {
                    EntryCount = (int)length / 2,
                    SamplesTable = buffer
                };
            }

            VideoTracks.AddNoResize(videoTrack);
        }
    }
}
