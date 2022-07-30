using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.MediaFramework.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.IO.LowLevel.Unsafe;

namespace Unity.MediaFramework.LowLevel.Format.ISOBMFF
{
    public enum ISOBMFFError
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

    public unsafe struct UnsafeByteArray : System.IDisposable
    {
        public Allocator Allocator;

        public int Length;
        public byte* Ptr;

        public UnsafeByteArray(byte* ptr, int length, Allocator allocator)
        {
            Allocator = allocator;
            Length = length;
            Ptr = ptr;
        }

        public void Dispose()
        {
            if (Ptr == null)
                return;

            UnsafeUtility.Free(Ptr, Allocator);

            Ptr = null;
            Length = 0;
            Allocator = Allocator.Invalid;
        }
    }

    public struct ErrorMessage
    {
        public ISOBMFFError Type;
        public FixedString128Bytes Message;
    }

    public struct MediaIOContext
    {
        public long FileSize;
        public ReadCommandArray Commands;

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
        public bool HasError => Error.Type != ISOBMFFError.None;

        public ReadCommand ReadCommand;
        public ErrorMessage Error;
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

            ref var ioContext = ref UnsafeUtility.AsRef<MediaIOContext>(IOContext.GetUnsafePtr());
            ref var context = ref UnsafeUtility.AsRef<MediaJobContext>(JobContext.GetUnsafePtr());

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
        [WriteOnly] public NativeReference<UnsafeByteArray> Header;

        public unsafe void Execute()
        {
            if (BoxChunks.Length == 0)
                return;

            ref var header = ref UnsafeUtility.AsRef<UnsafeByteArray>(Header.GetUnsafePtr());
            header = new UnsafeByteArray();
            foreach (var chunk in BoxChunks)
                header.Length += (int)chunk.Size;

            header.Allocator = Allocator.Persistent;
            header.Ptr = (byte*)UnsafeUtility.Malloc(header.Length, 1, header.Allocator);

            var buffer = header.Ptr;
            ref var ioContext = ref UnsafeUtility.AsRef<MediaIOContext>(IOContext.GetUnsafePtr());

            ReadCommand readCommand;
            // Reduce the number of read by combining adjacent boxes
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

            readCommand = new ReadCommand
            {
                Buffer = buffer,
                Offset = combinedChunk.Offset,
                Size = combinedChunk.Size
            };

            ioContext.AddReadCommandJob(ref readCommand);
        }
    }

    [BurstCompile]
    public struct CreateISOTable : IJob
    {
        [ReadOnly] public NativeReference<UnsafeByteArray> Header;

        [WriteOnly] public NativeReference<ISOTable> Table;

        public unsafe void Execute()
        {
            ref var table = ref UnsafeUtility.AsRef<ISOTable>(Table.GetUnsafePtr());
            table.Tracks = new UnsafeList<ISOTrackTable>(4, Allocator.Persistent);

            var header = Header.Value;

            uint index = 0;
            while (index < header.Length)
            {
                var box = ISOBox.Parse(header.Ptr + index);

                if (box.Size == 0)
                {
                    UnityEngine.Debug.LogError("box.Size == 0");
                    return;
                }

                //if(box.Size < ISOBox.ByteNeeded && box.Size > 1)
                //    { SetError(ErrorType.InvalidSize, $"Found a {box.Type} box with an invalid size of {box.Size}"); return; }

                uint size = box.Size;

                switch (box.Type)
                {
                    case ISOBoxType.FTYP:
                        table.FTYP = new FTYPBox.Ptr { value = header.Ptr + index };
                        break;
                    case ISOBoxType.MVHD:
                        table.MVHD = new MVHDBox.Ptr { value = header.Ptr + index };
                        break;
                    case ISOBoxType.HDLR:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .Handler = HDLRBox.GetHandlerType(header.Ptr + index);
                        break;
                    case ISOBoxType.TKHD:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .TKHD = new TKHDBox.Ptr { value = header.Ptr + index };
                        break;
                    case ISOBoxType.MDHD:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .MDHD = new MDHDBox.Ptr { value = header.Ptr + index };
                        break;
                    case ISOBoxType.STSD:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .STSD = new STSDBox.Ptr { value = header.Ptr + index };
                        break;
                    case ISOBoxType.STTS:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .STTS = new STTSBox.Ptr { value = header.Ptr + index };
                        break;
                    case ISOBoxType.STSS:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .STSS = new STSSBox.Ptr { value = header.Ptr + index };
                        break;
                    case ISOBoxType.STBL:
                    case ISOBoxType.MINF:
                    case ISOBoxType.MDIA:
                    case ISOBoxType.MOOV:
                        size = ISOBox.ByteNeeded;
                        break;
                    case ISOBoxType.TRAK:
                        table.Tracks.Add(new ISOTrackTable());
                        size = ISOBox.ByteNeeded;
                        break;
                }

                index += size;
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

        public unsafe static void ThrowError(ref MediaIOContext ioContext, ref MediaJobContext context, in ISOBMFFError type, in FixedString128Bytes message)
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
