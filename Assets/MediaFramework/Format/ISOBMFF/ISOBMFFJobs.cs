using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.MediaFramework.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.IO.LowLevel.Unsafe;

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

    public unsafe struct UnsafeRawArray : System.IDisposable
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
        [WriteOnly] public NativeReference<UnsafeRawArray> Header;

        public unsafe void Execute()
        {
            if (BoxChunks.Length == 0)
                return;

            ref var header = ref UnsafeUtility.AsRef<UnsafeRawArray>(Header.GetUnsafePtr());
            header = new UnsafeRawArray();
            foreach (var chunk in BoxChunks)
                header.Length += (int)chunk.Size;

            header.Allocator = Allocator.Persistent;
            header.Ptr = (byte*)UnsafeUtility.Malloc(header.Length, 1, header.Allocator);

            var buffer = header.Ptr;
            ref var ioContext = ref UnsafeUtility.AsRef<MediaIOContext>(IOContext.GetUnsafePtr());

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

            readCommand = new ReadCommand
            {
                Buffer = buffer,
                Offset = combinedChunk.Offset,
                Size = combinedChunk.Size
            };

            ioContext.AddReadCommandJob(ref readCommand);
        }
    }

    [BurstCompile(Debug = true)]
    public struct CreateISOTable : IJob
    {
        [ReadOnly] public NativeReference<UnsafeRawArray> Header;

        [WriteOnly] public NativeReference<ISOTable> Table;

        public unsafe void Execute()
        {
            ref var table = ref UnsafeUtility.AsRef<ISOTable>(Table.GetUnsafePtr());
            table.Tracks = new UnsafeList<ISOTrackTable>(4, Allocator.Persistent);

            var header = Header.Value;

            uint position = 0;
            while (position < header.Length)
            {
                var box = ISOBox.Parse(header.Ptr + position);

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
                        table.FTYP = new FTYPBox.Ptr { value = header.Ptr + position };
                        break;
                    case ISOBoxType.MVHD:
                        table.MVHD = new MVHDBox.Ptr { value = header.Ptr + position };
                        break;
                    case ISOBoxType.HDLR:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .Handler = HDLRBox.GetHandlerType(header.Ptr + position);
                        break;
                    case ISOBoxType.TKHD:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .TKHD = new TKHDBox.Ptr { value = header.Ptr + position };
                        break;
                    case ISOBoxType.MDHD:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .MDHD = new MDHDBox.Ptr { value = header.Ptr + position };
                        break;
                    case ISOBoxType.STSD:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .STSD = new STSDBox.Ptr { value = header.Ptr + position };
                        break;
                    case ISOBoxType.STTS:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .STTS = new STTSBox.Ptr { value = header.Ptr + position };
                        break;
                    case ISOBoxType.STSS:
                        table.Tracks.ElementAt(table.Tracks.Length - 1)
                            .STSS = new STSSBox.Ptr { value = header.Ptr + position };
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

                position += size;
            }
        }
    }

    //[BurstCompile(Debug = true, CompileSynchronously = true)]
    public struct GetMediaAttributes : IJob
    {
        [ReadOnly] public NativeReference<ISOTable> Table;

        public NativeReference<UnsafeRawArray> Header;

        [WriteOnly] public NativeList<VideoTrack> VideoTracks;
        [WriteOnly] public NativeList<AudioTrack> AudioTracks;

        public unsafe void Execute()
        {
            ref var table = ref UnsafeUtility.AsRef<ISOTable>(Table.GetUnsafeReadOnlyPtr());

            for (int i = 0; i < table.Tracks.Length; i++)
            {
                switch (table.Tracks[i].Handler)
                {
                    case ISOHandler.VIDE: ParseVideo(table.Tracks[i]); break;
                    case ISOHandler.SOUN: ParseAudio(table.Tracks[i]); break;
                }
            }
        }

        public unsafe void ParseVideo(in ISOTrackTable table)
        {
            var videoTrack = new VideoTrack();
            if (table.MDHD.value != null)
            {
                var mdhd = table.MDHD.Parse();
                videoTrack.Duration = mdhd.Duration;
                videoTrack.TimeScale = mdhd.Timescale;
            }

            if (table.TKHD.value != null)
            { 
                var tkhd = table.TKHD.Parse();
                videoTrack.TrackID = tkhd.TrackID;
                videoTrack.Width = (int)tkhd.Width.ConvertDouble();
                videoTrack.Height = (int)tkhd.Height.ConvertDouble();
            }

            if (table.STSD.value != null)
            {
                var stsd = table.STSD.ParseVideo();
                videoTrack.Codec = stsd.Codec;
                videoTrack.PixelWidth = stsd.Width;
                videoTrack.PixelHeight = stsd.Height;
                videoTrack.ColorFormat = new ColorFormat();

                //if (stsd.DecoderConfigurationBox != null)
                //{
                //    switch (stsd.Codec)
                //    {
                //        case VideoCodec.AVC1:
                //            var decoderConfig = AVCDecoderConfigurationRecord.Parse(stsd.DecoderConfigurationBox);
                //            if (decoderConfig.AVCProfileIndicationMeta != null)
                //            { 
                //                var profileMeta = AVCProfileIndicationMeta.Parse(decoderConfig.AVCProfileIndicationMeta);
                //                UnityEngine.Debug.Log($"{profileMeta.ChromaFormat} {profileMeta.BitDepthChromaMinus8 + 8} {profileMeta.BitDepthLumaMinus8 + 8}");
                //            }
                //            break;
                //    }
                //}
            }

            if (table.STTS.value != null)
            {
                var stts = table.STTS.Parse();
                videoTrack.FrameCount = stts.SampleCount;
                videoTrack.TimeToSampleTable = new TimeToSampleTable
                {
                    Length = stts.EntryCount,
                    Samples = stts.SamplesTable
                };
            }

            if (table.STSS.value != null)
            {
                var stss = table.STSS.Parse();
                videoTrack.SyncSampleTable = new SyncSampleTable
                {
                    Length = stss.EntryCount,
                    SampleNumbers = stss.SyncSamplesTable
                };
            }

            VideoTracks.Add(videoTrack);
        }

        public unsafe void ParseAudio(in ISOTrackTable table)
        {
            var audioTrack = new AudioTrack();
            if (table.MDHD.value != null)
            {
                var mdhd = table.MDHD.Parse();
                audioTrack.Duration = mdhd.Duration;
                audioTrack.TimeScale = mdhd.Timescale;
                audioTrack.Language = mdhd.Language;
            }

            if (table.TKHD.value != null)
            {
                var tkhd = table.TKHD.Parse();
                audioTrack.TrackID = tkhd.TrackID;
            }

            if (table.STSD.value != null)
            {
                var stsd = table.STSD.ParseAudio();
                audioTrack.Codec = stsd.Codec;
                audioTrack.ChannelCount = stsd.ChannelCount;
                audioTrack.SampleRate = stsd.SampleRate;
            }

            if (table.STTS.value != null)
            {
                var stts = table.STTS.Parse();
                audioTrack.SampleCount = stts.SampleCount;
                audioTrack.TimeToSampleTable = new TimeToSampleTable
                {
                    Length = stts.EntryCount,
                    Samples = stts.SamplesTable
                };
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
