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

    public struct MediaIOContext
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
    public struct MediaJobContext
    {
        public bool HasError => Error.Type != ErrorType.None;

        public ReadCommand ReadCommand;
        public ErrorMessage Error;
    }

    public unsafe struct ISOTable
    {

    }

    [BurstCompile]
    public struct FindTopBoxes : IJob
    {
        [ReadOnly] public NativeArray<byte> Stream;

        public NativeReference<MediaJobContext> JobContext;
        public NativeReference<MediaIOContext> IOContext;

        [WriteOnly] public NativeList<BoxChunk> BoxChunks;

        public unsafe void Execute()
        {
            if (IOContext.Value.Commands.CommandCount == 0)
                return;

            ref var ioContext = ref UnsafeUtility.AsRef<MediaIOContext>(IOContext.GetUnsafePtrWithoutChecks());
            ref var context = ref UnsafeUtility.AsRef<MediaJobContext>(JobContext.GetUnsafePtrWithoutChecks());

            ioContext.Commands.CommandCount = 0;

            var buffer = (byte*)Stream.GetUnsafeReadOnlyPtr();

            long position = 0;
            while (position + ISOBox.ByteNeeded < context.ReadCommand.Size)
            {
                var box = ISOBox.Parse(buffer + position);

                long size = box.Size >= ISOBox.ByteNeeded ? box.Size :
                            box.Size == 1 ? (long)BigEndian.ReadUInt64(buffer + position + ISOBox.ByteNeeded) :
                                            ioContext.FileSize - context.ReadCommand.Offset - position;

                switch (box.Type)
                {
                    case ISOBoxType.FTYP:
                    case ISOBoxType.PDIN:
                    case ISOBoxType.MOOV:
                    case ISOBoxType.MOOF:
                    case ISOBoxType.MFRA:
                    case ISOBoxType.META:
                    case ISOBoxType.MECO:
                    case ISOBoxType.STYP:
                    case ISOBoxType.SIDX:
                    case ISOBoxType.SSIX:
                    case ISOBoxType.PRFT:
                        BoxChunks.Add(new BoxChunk
                        {
                            Offset = context.ReadCommand.Offset + position,
                            Size = size
                        });
                        break;
                }

                position += size;
            }

            if (context.ReadCommand.Offset + position < ioContext.FileSize)
            {
                context.ReadCommand.Offset += position;
                ioContext.AddReadCommandJob(ref context.ReadCommand);
            }
        }
    }

    [BurstCompile(Debug = true)]
    public struct ReadAndLoadInMemoryAllBoxChunks : IJob
    {
        [ReadOnly] public NativeList<BoxChunk> BoxChunks;

        [WriteOnly] public NativeReference<MediaIOContext> IOContext;
        [WriteOnly] public NativeReference<UnsafeRawArray> RawBuffer;

        public unsafe void Execute()
        {
            if (BoxChunks.Length == 0)
                return;

            var rawData = new UnsafeRawArray();
            foreach (var chunk in BoxChunks)
                rawData.Length += (int)chunk.Size;

            rawData.Allocator = Allocator.Persistent;
            rawData.Ptr = (byte*)UnsafeUtility.Malloc(rawData.Length, 1, rawData.Allocator);

            RawBuffer.Value = rawData;

            var buffer = rawData.Ptr;
            ref var ioContext = ref UnsafeUtility.AsRef<MediaIOContext>(IOContext.GetUnsafePtrWithoutChecks());

            ReadCommand readCommand; 
            var combinedChunk = BoxChunks[0];
            for (int i = 1; i < BoxChunks.Length; i++)
            {
                if (combinedChunk.Offset + combinedChunk.Size == BoxChunks[i].Offset)
                {
                    combinedChunk.Size += BoxChunks[i].Size;
                }
                else
                {
                    readCommand = new ReadCommand
                    {
                        Buffer = buffer,
                        Offset = combinedChunk.Offset,
                        Size = combinedChunk.Size
                    };

                    ioContext.AddReadCommandJob(ref readCommand);
                    buffer += (int)combinedChunk.Size;
                    combinedChunk = BoxChunks[i];
                }          
            }
        }
    }

    [BurstCompile]
    public struct AsyncTryParseISOBMFFContent : IJob
    {
        [ReadOnly] public NativeArray<byte> Stream;

        public NativeReference<MediaIOContext> IOContext;
        public NativeReference<MediaJobContext> Context;

        public NativeReference<ISOTable> Table;

        public unsafe void Execute()
        {
            if (Context.Value.HasError)
                return;

            ref var ioContext = ref UnsafeUtility.AsRef<MediaIOContext>(IOContext.GetUnsafePtrWithoutChecks());

            ioContext.Commands.CommandCount = 0;

            if (Context.Value.ReadCommand.Size == 0)
                return;

            ref var context = ref UnsafeUtility.AsRef<MediaJobContext>(Context.GetUnsafePtrWithoutChecks());

            long position = 0;
            if (ExecuteMainLoop(ref ioContext, ref context, ref position))
            {
                context.ReadCommand.Offset += position;
                ioContext.AddReadCommandJob(ref context.ReadCommand);
            }
            else
            {
                context.ReadCommand.Offset = ioContext.FileSize;
                context.ReadCommand.Size = 0;
            }
        }

        public unsafe bool ExecuteMainLoop(ref MediaIOContext ioContext, ref MediaJobContext context, ref long position)
        {
            var buffer = (byte*)Stream.GetUnsafeReadOnlyPtr();

            while (position + ISOBox.ByteNeeded < context.ReadCommand.Size)
            {
                var box = ISOBox.Parse(buffer + position);

                //if(box.Size < ISOBox.ByteNeeded && box.Size > 1)
                //    { SetError(ErrorType.InvalidSize, $"Found a {box.Type} box with an invalid size of {box.Size}"); return; }

                long size = box.Size >= ISOBox.ByteNeeded ? box.Size :
                            box.Size == 1 ? (long)BigEndian.ReadUInt64(buffer + position + ISOBox.ByteNeeded) :
                                            ioContext.FileSize - context.ReadCommand.Offset - position;

                position += size;
            }

            return context.ReadCommand.Offset + position < ioContext.FileSize;
        }

        public unsafe static void MemCpyOverflow(ref MediaIOContext ioContext, in ReadCommand readCommand, in byte* destination, in byte* stream, in long position, in long size)
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

        public unsafe static void ThrowError(ref MediaIOContext ioContext, ref MediaJobContext context, in ErrorType type, in FixedString128Bytes message)
        {
            context.Error = new()
            {
                Message = message,
                Type = type
            };

            ioContext.Commands.CommandCount = 0;
        }
    }
}
