using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel.Codecs;
using Unity.MediaFramework.LowLevel.Unsafe;

namespace Unity.MediaFramework.LowLevel.Format.NAL
{
    public enum ChromaSubsampling : byte
    {
        YUV400 = 0,
        YUV420 = 1,
        YUV422 = 2,
        YUV444 = 3
    }

    public enum ColourPrimaries
    {
        BT709 = 1,
        UNSPECIFIED = 2,
        BT470M = 4,
        BT470BG = 5, // BT601 625
        SMPTE170M = 6, // BT601 525
        SMPTE240M = 7,
        FILM = 8,
        BT2020 = 9,
        SMPTE428 = 10,
        SMPTE431 = 11,
        SMPTE432 = 12,
        EBU3213 = 22
    }

    public enum SPSError
    {
        None,
        InvalidLength,
        ForbiddenZeroBit,
        InvalidRefID,
        InvalidUnitType,
        ReaderOverflow,
        ReaderOutOfRange,
        ReaderUnknown,
        InvalidSeqParamaterSetId,
        InvalidChromaFormat,
        InvalidBitDepthLuma,
        InvalidBitDepthChroma,
        InvalidScalingMatrixDeltaScale,
        InvalidMaxFrameNum,
        InvalidPicOrderCntType,
        InvalidMaxPictureOrderCntLsb,
        InvalidNumRefFramesInCycle,
        InvalidMaxNumRefFrames,
        InvalidMbWidth,
        InvalidMbHeight,
        InvalidCrop,
        InvalidAspectIndicator,
        InvalidVideoFormat,
        InvalidChromaLocTypeField,
        InvalidTimeInfo,
    }

    public struct ScalingMatrix
    {
        public ScalingMatrix4x4 matrix00;
        public ScalingMatrix4x4 matrix01;
        public ScalingMatrix4x4 matrix02;
        public ScalingMatrix4x4 matrix03;
        public ScalingMatrix4x4 matrix04;
        public ScalingMatrix4x4 matrix05;
        public ScalingMatrix8x8 matrix06;
        public ScalingMatrix8x8 matrix07;

        public ScalingMatrix8x8 matrix08;
        public ScalingMatrix8x8 matrix09;
        public ScalingMatrix8x8 matrix10;
        public ScalingMatrix8x8 matrix11;

        public unsafe SPSError Parse(ref BitReader reader, bool isYUV444)
        {
            fixed (ScalingMatrix4x4* default4Intra = &ScalingMatrix4x4.DefaultIntra)
            fixed (ScalingMatrix4x4* default4Inter = &ScalingMatrix4x4.DefaultInter)
            fixed (ScalingMatrix8x8* default8Intra = &ScalingMatrix8x8.DefaultIntra)
            fixed (ScalingMatrix8x8* default8Inter = &ScalingMatrix8x8.DefaultInter)
            {
                var mat00 = (uint*)UnsafeUtility.AddressOf(ref matrix00);
                var mat01 = (uint*)UnsafeUtility.AddressOf(ref matrix01);
                var mat02 = (uint*)UnsafeUtility.AddressOf(ref matrix02);
                var mat03 = (uint*)UnsafeUtility.AddressOf(ref matrix03);
                var mat04 = (uint*)UnsafeUtility.AddressOf(ref matrix04);
                var mat05 = (uint*)UnsafeUtility.AddressOf(ref matrix05);
                var mat06 = (uint*)UnsafeUtility.AddressOf(ref matrix06);
                var mat07 = (uint*)UnsafeUtility.AddressOf(ref matrix07);

                SPSError error;
                error = ParseScalingMatrices(ref reader, mat00, 16, (uint*)default4Intra, (uint*)default4Intra);    // Intra Y
                if (error != SPSError.None) return error;
                error = ParseScalingMatrices(ref reader, mat01, 16, (uint*)default4Intra, mat00);                   // Intra Cr
                if (error != SPSError.None) return error;
                error = ParseScalingMatrices(ref reader, mat02, 16, (uint*)default4Intra, mat01);                   // Intra Cb
                if (error != SPSError.None) return error;
                error = ParseScalingMatrices(ref reader, mat03, 16, (uint*)default4Inter, (uint*)default4Inter);    // Inter Y
                if (error != SPSError.None) return error;
                error = ParseScalingMatrices(ref reader, mat04, 16, (uint*)default4Intra, mat03);                   // Inter Cr
                if (error != SPSError.None) return error;
                error = ParseScalingMatrices(ref reader, mat05, 16, (uint*)default4Intra, mat04);                   // Inter Cb
                if (error != SPSError.None) return error;
                error = ParseScalingMatrices(ref reader, mat06, 64, (uint*)default8Intra, (uint*)default8Intra);    // Intra Y
                if (error != SPSError.None) return error;
                error = ParseScalingMatrices(ref reader, mat07, 64, (uint*)default8Inter, (uint*)default8Inter);    // Inter Y
                if (error != SPSError.None) return error;
                if (isYUV444)
                {
                    var mat08 = (uint*)UnsafeUtility.AddressOf(ref matrix08);
                    var mat09 = (uint*)UnsafeUtility.AddressOf(ref matrix09);
                    var mat10 = (uint*)UnsafeUtility.AddressOf(ref matrix10);
                    var mat11 = (uint*)UnsafeUtility.AddressOf(ref matrix11);

                    error = ParseScalingMatrices(ref reader, mat08, 64, (uint*)default8Intra, mat06);               // Intra Cr
                    if (error != SPSError.None) return error;
                    error = ParseScalingMatrices(ref reader, mat09, 64, (uint*)default8Inter, mat07);               // Inter Cr
                    if (error != SPSError.None) return error;
                    error = ParseScalingMatrices(ref reader, mat10, 64, (uint*)default8Intra, mat08);               // Intra Cb
                    if (error != SPSError.None) return error;
                    error = ParseScalingMatrices(ref reader, mat11, 64, (uint*)default8Inter, mat09);               // Inter Cb
                    if (error != SPSError.None) return error;
                }

                return SPSError.None;
            }
        }

        private unsafe static SPSError ParseScalingMatrices(ref BitReader reader, uint* scalingList, int size, uint* defaultMatrix, uint* fallback)
        {
            if (reader.CheckForBits(1))
                return SPSError.ReaderOutOfRange;

            var seq_scaling_list_present_flag = reader.ReadBool();
            if (seq_scaling_list_present_flag)
            {
                var lastScale = 8;
                var nextScale = 8;

                var deltaScale = reader.ReadSExpGolomb();
                if (reader.Error != ReaderError.None)
                    return SequenceParameterSet.ConvertError(reader.Error);

                if (deltaScale < -128 || deltaScale > 127)
                    return SPSError.InvalidScalingMatrixDeltaScale;

                nextScale = (lastScale + deltaScale) & 0xFF;

                if (nextScale == 0)
                {
                    UnsafeUtility.MemCpy(scalingList, defaultMatrix, size * sizeof(uint));
                }

                lastScale = nextScale;
                scalingList[0] = (uint)lastScale;

                for (int j = 1; j < size; j++)
                {
                    if (nextScale != 0)
                    {
                        deltaScale = reader.ReadSExpGolomb();
                        if (reader.Error != ReaderError.None)
                            return SequenceParameterSet.ConvertError(reader.Error);

                        if (deltaScale < -128 || deltaScale > 127)
                            return SPSError.InvalidScalingMatrixDeltaScale;

                        nextScale = (lastScale + deltaScale) & 0xFF;
                    }

                    lastScale = nextScale == 0 ? lastScale : nextScale;
                    scalingList[j] = (uint)lastScale;
                }
            }
            else
            {
                UnsafeUtility.MemCpy(scalingList, fallback, size * sizeof(uint));
            }

            return SPSError.None;
        }
    }

    public struct ScalingMatrix4x4
    {
        public static readonly ScalingMatrix4x4 DefaultIntra = new()
        {
            value = new(6, 13, 13, 20, 20, 20, 28, 28, 28, 28, 32, 32, 32, 37, 37, 42)
        };

        public static readonly ScalingMatrix4x4 DefaultInter = new()
        {
            value = new(10, 14, 14, 20, 20, 20, 24, 24, 24, 24, 27, 27, 27, 30, 30, 34)
        };

        public uint4x4 value;
    }

    public struct ScalingMatrix8x8
    {
        public static readonly ScalingMatrix8x8 DefaultIntra = new()
        {
            value0 = new(06, 10, 10, 13, 11, 13, 16, 16, 16, 16, 18, 18, 18, 18, 18, 23),
            value1 = new(23, 23, 23, 23, 23, 25, 25, 25, 25, 25, 25, 25, 27, 27, 27, 27),
            value2 = new(27, 27, 27, 27, 29, 29, 29, 29, 29, 29, 29, 31, 31, 31, 31, 31),
            value3 = new(31, 33, 33, 33, 33, 33, 36, 36, 36, 36, 38, 38, 38, 40, 40, 42)
        };

        public static readonly ScalingMatrix8x8 DefaultInter = new()
        {
            value0 = new(09, 13, 13, 15, 13, 15, 17, 17, 17, 17, 19, 19, 19, 19, 19, 21),
            value1 = new(21, 21, 21, 21, 21, 22, 22, 22, 22, 22, 22, 22, 24, 24, 24, 24),
            value2 = new(24, 24, 24, 24, 25, 25, 25, 25, 25, 25, 25, 27, 27, 27, 27, 27),
            value3 = new(27, 28, 28, 28, 28, 28, 30, 30, 30, 30, 32, 32, 32, 33, 33, 35)
        };

        public uint4x4 value0;
        public uint4x4 value1;
        public uint4x4 value2;
        public uint4x4 value3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public struct SPSProfile
    {
        public H264Profile FullProfile => H264Utility.GetProfile(Value, Constraints);

        public uint ProfileLevelId => (uint)Value << 16 | (uint)Constraints << 8 | Level;

        public byte Value;
        public byte Constraints;
        public byte Level;
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public unsafe struct SPSChromaFormat
    {
        [Flags]
        public enum Flag : byte
        {
            None = 0x00,
            SeparateColourPlane = 0x01, // separate_colour_plane_flag
            TransformBypass = 0x02, // qpprime_y_zero_transform_bypass_flag
        }

        public Flag Flags;
        public ChromaSubsampling Format;
        public byte BitDepthLuma;
        public byte BitDepthChroma;

        public bool SeparateColourPlane => (Flags & Flag.SeparateColourPlane) == Flag.SeparateColourPlane;

        public bool TransformBypass => (Flags & Flag.TransformBypass) == Flag.TransformBypass;

        public uint RawMbBits => 256u * BitDepthLuma + 2u * MbWidthC * MbHeightC * BitDepthChroma;

        public uint MbWidthC => 16 / SubWidthC;

        public uint MbHeightC => 16 / SubHeightC;

        public uint SubWidthC => Format switch
        {
            ChromaSubsampling.YUV420 or ChromaSubsampling.YUV422 => 2u,
            ChromaSubsampling.YUV444 => !SeparateColourPlane ? 1u : 0u,
            _ => 0u,
        };

        public uint SubHeightC => Format switch
        {
            ChromaSubsampling.YUV420 => 2u,
            ChromaSubsampling.YUV422 => 1u,
            ChromaSubsampling.YUV444 => !SeparateColourPlane ? 1u : 0u,
            _ => 0u,
        };
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct SPSPicOrderCnt
    {
        [FieldOffset(00)] public byte Type;
        [FieldOffset(01)] public byte MaxNumRefFrames;

        [FieldOffset(04)] public uint MaxLsb;

        [FieldOffset(08)] public int OffsetForNonRefPic;
        [FieldOffset(12)] public int OffsetForTopToBottomField;
        [FieldOffset(16)] public int NumRefFramesInCycle;
        [FieldOffset(20)] public int* OffsetRefFrame;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SPSAspectRatio
    {
        public const byte Extend_SAR = 255;

        [FieldOffset(00)] public byte Indicator;
        [FieldOffset(04)] public ushort SARWidth, SARHeigth;

        public static Tuple<ushort, ushort> GetSAR(byte indicator) => indicator switch
        {
            1 => new(1, 1),
            2 => new(12, 11),
            3 => new(10, 11),
            4 => new(16, 11),
            5 => new(40, 33),
            6 => new(24, 11),
            7 => new(20, 11),
            8 => new(32, 11),
            9 => new(80, 33),
            10 => new(18, 11),
            11 => new(15, 11),
            12 => new(64, 33),
            13 => new(160, 99),
            14 => new(4, 3),
            15 => new(3, 2),
            16 => new(2, 1),
            _ => new(0, 1)
        };
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public struct ChromaSampleLocType
    {
        public byte TopField;
        public byte BottomField;
    }

    public struct SPSTime
    {
        public uint NumUnitsInTick;
        public uint TimeScale;
    }

    /// <summary>
    /// Sequence Parameter Set ITU-T H.264 08/2021 7.3.2.1.1
    /// https://www.itu.int/ITU-T/recommendations/rec.aspx?rec=14659&lang=en
    /// </summary>
    public unsafe struct SequenceParameterSet : IDisposable
    {
        public Allocator Allocator;

        public int ID;
        public SPSProfile Profile;
        public SPSChromaFormat Chroma;
        public ScalingMatrix* ScalingMatrix;

        public uint MaxFrameNum;
        public SPSPicOrderCnt PicOrderCnt;

        public BitField32 Flags;
        public uint MbWidth, MbHeigth;
        public uint CropLeft, CropRight;
        public uint CropTop, CropBottom;

        public SPSAspectRatio AspectRatio;

        public byte VideoFormat;
        public byte ColourPrimaries;
        public byte TransferCharacteristics;
        public byte MatrixCoefficients;

        public ChromaSampleLocType LocType;
        public SPSTime Time;

        public bool DeltaAlwaysZero { get => Flags.IsSet(00); set => Flags.SetBits(00, value); }
        public bool GapsInFrameNumValueAllowed { get => Flags.IsSet(01); set => Flags.SetBits(01, value); }
        public bool FrameMbsOnly { get => Flags.IsSet(02); set => Flags.SetBits(02, value); }
        public bool MbAdaptiveFrameField { get => Flags.IsSet(03); set => Flags.SetBits(03, value); }
        public bool Direct8x8Inference { get => Flags.IsSet(04); set => Flags.SetBits(04, value); }
        public bool OverscanAppropriate { get => Flags.IsSet(05); set => Flags.SetBits(05, value); }
        public bool VideoFullRange { get => Flags.IsSet(06); set => Flags.SetBits(06, value); }
        public bool FixedFrameRate { get => Flags.IsSet(07); set => Flags.SetBits(07, value); }

        public unsafe SPSError Parse(byte* buffer, int length, Allocator allocator)
        {
            if (length < 4)
                return SPSError.InvalidLength;

            // forbidden_zero_bit
            if ((buffer[0] & 0x80) == 0x80)
                return SPSError.ForbiddenZeroBit;

            // nal_ref_id
            if ((buffer[0] & 0x60) == 0)
                return SPSError.InvalidRefID;

            // nal_unit_type
            if ((buffer[0] & 0x1F) != 7)
                return SPSError.InvalidUnitType;

            Profile.Value = buffer[1];          // profile_idc
            Profile.Constraints = buffer[2];    // profile_iop
            Profile.Level = buffer[3];          // level_idc

            length = RemoveEmulationPreventionBytes(buffer + 4, length - 4);

            var reader = new BitReader(buffer + 4, length - 4);

            var seq_parameter_set_id = reader.ReadUExpGolomb();
            if (reader.Error != ReaderError.None)
                return ConvertError(reader.Error);

            if (seq_parameter_set_id > 31)
                return SPSError.InvalidSeqParamaterSetId;

            ID = (byte)seq_parameter_set_id;

            if (H264Utility.HasChroma(Profile.Value))
            {
                var chroma_format = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                if (chroma_format > 3)
                    return SPSError.InvalidChromaFormat;

                Chroma.Format = (ChromaSubsampling)chroma_format;

                if (Chroma.Format == ChromaSubsampling.YUV444)
                {
                    if (reader.CheckForBits(1))
                        return SPSError.ReaderOutOfRange;

                    Chroma.Flags |= reader.ReadBool() ? SPSChromaFormat.Flag.SeparateColourPlane : 0;
                }

                var bit_depth_luma_minus8 = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                if (bit_depth_luma_minus8 > 6)
                    return SPSError.InvalidBitDepthLuma;

                Chroma.BitDepthLuma = (byte)(bit_depth_luma_minus8 + 8);

                var bit_depth_chroma_minus8 = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                if (bit_depth_chroma_minus8 > 6)
                    return SPSError.InvalidBitDepthChroma;

                Chroma.BitDepthChroma = (byte)(bit_depth_chroma_minus8 + 8);

                if (reader.CheckForBits(1))
                    return SPSError.ReaderOutOfRange;

                Chroma.Flags |= reader.ReadBool() ? SPSChromaFormat.Flag.TransformBypass : 0; // qpprime_y_zero_transform_bypass_flag

                if (reader.CheckForBits(1))
                    return SPSError.ReaderOutOfRange;

                if (reader.ReadBool()) // seq_scaling_matrix_present_flag
                {
                    Allocator = allocator;
                    if (ScalingMatrix == null)
                        ScalingMatrix = (ScalingMatrix*)UnsafeUtility.Malloc(sizeof(ScalingMatrix), 4, allocator);

                    var error = ScalingMatrix->Parse(ref reader, Chroma.Format == ChromaSubsampling.YUV444);
                    if (error != SPSError.None)
                        return error;
                }
            }
            else
            {
                // If not present, it shall be inferred to be equal to 1 (4:2:0 chroma format).
                Chroma.Format = ChromaSubsampling.YUV420;
                Chroma.BitDepthLuma = 8;
                Chroma.BitDepthChroma = 8;
            }

            var log2_max_frame_num_minus4 = reader.ReadUExpGolomb();
            if (reader.Error != ReaderError.None)
                return ConvertError(reader.Error);

            if (log2_max_frame_num_minus4 > 12)
                return SPSError.InvalidMaxFrameNum;

            MaxFrameNum = 1u << (int)(log2_max_frame_num_minus4 + 4);

            var pic_order_cnt_type = reader.ReadUExpGolomb();
            if (reader.Error != ReaderError.None)
                return ConvertError(reader.Error);

            if (pic_order_cnt_type > 1)
                return SPSError.InvalidPicOrderCntType;

            PicOrderCnt.Type = (byte)pic_order_cnt_type;

            switch (PicOrderCnt.Type)
            {
                case 0:
                    var log2_max_pic_order_cnt_lsb_minus4 = reader.ReadUExpGolomb();
                    if (reader.Error != ReaderError.None)
                        return ConvertError(reader.Error);

                    if (log2_max_pic_order_cnt_lsb_minus4 > 12)
                        return SPSError.InvalidMaxPictureOrderCntLsb;

                    PicOrderCnt.MaxLsb = 1u << (int)(log2_max_pic_order_cnt_lsb_minus4 + 4);
                    break;
                case 1:
                    DeltaAlwaysZero = reader.ReadBool();

                    var offset_for_non_ref_pic = reader.ReadSExpGolomb();
                    if (reader.Error != ReaderError.None)
                        return ConvertError(reader.Error);

                    PicOrderCnt.OffsetForNonRefPic = offset_for_non_ref_pic;

                    var offset_for_top_to_bottom_field = reader.ReadSExpGolomb();
                    if (reader.Error != ReaderError.None)
                        return ConvertError(reader.Error);

                    PicOrderCnt.OffsetForTopToBottomField = offset_for_top_to_bottom_field;

                    var num_ref_frames_in_pic_order_cnt_cycle = reader.ReadUExpGolomb();
                    if (reader.Error != ReaderError.None)
                        return ConvertError(reader.Error);

                    if (num_ref_frames_in_pic_order_cnt_cycle > 255)
                        return SPSError.InvalidNumRefFramesInCycle;

                    PicOrderCnt.NumRefFramesInCycle = (byte)num_ref_frames_in_pic_order_cnt_cycle;
                    if (num_ref_frames_in_pic_order_cnt_cycle > 0)
                    {
                        Allocator = allocator;
                        if (PicOrderCnt.OffsetRefFrame == null)
                            PicOrderCnt.OffsetRefFrame = (int*)UnsafeUtility.Malloc(num_ref_frames_in_pic_order_cnt_cycle * sizeof(int), 4, allocator);

                        for (int i = 0; i < num_ref_frames_in_pic_order_cnt_cycle; i++)
                        {
                            var offsetForRefFrame = reader.ReadSExpGolomb();
                            if (reader.Error != ReaderError.None)
                                return ConvertError(reader.Error);

                            PicOrderCnt.OffsetRefFrame[i] = offsetForRefFrame;
                        }
                    }
                    break;
            }

            var max_num_ref_frames = reader.ReadUExpGolomb();
            if (reader.Error != ReaderError.None)
                return ConvertError(reader.Error);

            if (max_num_ref_frames > 16)
                return SPSError.InvalidMaxNumRefFrames;

            PicOrderCnt.MaxNumRefFrames = (byte)max_num_ref_frames;

            GapsInFrameNumValueAllowed = reader.ReadBool();

            //public uint PicWidth => (Value.pic_width_in_mbs_minus_1 + 1) << 4;

            //public uint PicHeigth => ((Value.MbAdaptiveFrameField ? 2u : 1u) * (Value.pic_height_in_map_units_minus_1 + 1)) << 4;

            var pic_width_in_mbs_minus_1 = reader.ReadUExpGolomb();
            if (reader.Error != ReaderError.None)
                return ConvertError(reader.Error);

            if (pic_width_in_mbs_minus_1 + 1 >= ushort.MaxValue)
                return SPSError.InvalidMbHeight;

            MbWidth = pic_width_in_mbs_minus_1 + 1;

            var pic_height_in_map_units_minus_1 = reader.ReadUExpGolomb();
            if (reader.Error != ReaderError.None)
                return ConvertError(reader.Error);

            if (pic_height_in_map_units_minus_1 + 1 >= ushort.MaxValue)
                return SPSError.InvalidMbHeight;

            MbHeigth = pic_height_in_map_units_minus_1 + 1;

            if (reader.CheckForBits(1))
                return SPSError.ReaderOutOfRange;

            FrameMbsOnly = reader.ReadBool();

            if (!FrameMbsOnly)
            {
                MbHeigth *= 2;

                if (reader.CheckForBits(1))
                    return SPSError.ReaderOutOfRange;

                MbAdaptiveFrameField = reader.ReadBool();
            }

            if (reader.CheckForBits(1))
                return SPSError.ReaderOutOfRange;

            Direct8x8Inference = reader.ReadBool();

            if (reader.CheckForBits(1))
                return SPSError.ReaderOutOfRange;

            if (reader.ReadBool()) //frame_cropping
            {
                var frame_crop_left_offset = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                CropLeft = frame_crop_left_offset;

                var frame_cropping_rect_right_offset = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                CropRight = frame_cropping_rect_right_offset;

                var frame_cropping_rect_top_offset = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                CropTop = frame_cropping_rect_top_offset;

                var frame_cropping_rect_bottom_offset = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                CropBottom = frame_cropping_rect_bottom_offset;

                uint width = MbWidth << 4;
                uint height = MbHeigth << 4;

                if (CropLeft + CropRight > width || CropTop + CropBottom > height)
                    return SPSError.InvalidCrop;
            }

            if (reader.CheckForBits(1))
                return SPSError.ReaderOutOfRange;

            if (reader.ReadBool()) // vui_parameters_present_flag
            {
                if (reader.CheckForBits(1))
                    return SPSError.ReaderOutOfRange;

                if (reader.ReadBool()) // aspect_ratio_info_present_flag
                {
                    if (reader.CheckForBits(8))
                        return SPSError.ReaderOutOfRange;

                    var aspect_ratio_idc = reader.ReadBits(8);
                    if (aspect_ratio_idc > 16 && aspect_ratio_idc != 255)
                        return SPSError.InvalidAspectIndicator;

                    AspectRatio.Indicator = (byte)aspect_ratio_idc;
                    if (AspectRatio.Indicator == SPSAspectRatio.Extend_SAR)
                    {
                        if (reader.CheckForBits(32))
                            return SPSError.ReaderOutOfRange;

                        AspectRatio.SARWidth = (ushort)reader.ReadBits(16);
                        AspectRatio.SARHeigth = (ushort)reader.ReadBits(16);
                    }
                    else
                    {
                        var sar = SPSAspectRatio.GetSAR(AspectRatio.Indicator);
                        AspectRatio.SARWidth = sar.Item1;
                        AspectRatio.SARHeigth = sar.Item2;
                    }
                }

                if (reader.CheckForBits(1))
                    return SPSError.ReaderOutOfRange;

                if (reader.ReadBool()) // overscan_info_present_flag
                {
                    if (reader.CheckForBits(1))
                        return SPSError.ReaderOutOfRange;

                    OverscanAppropriate = reader.ReadBool();
                }

                if (reader.CheckForBits(1))
                    return SPSError.ReaderOutOfRange;

                if (reader.ReadBool()) // video_signal_type_present_flag
                {
                    if (reader.CheckForBits(5))
                        return SPSError.ReaderOutOfRange;

                    var video_format = reader.ReadBits(3);
                    if (video_format > 5)
                        return SPSError.InvalidVideoFormat;

                    VideoFormat = (byte)video_format;

                    VideoFullRange = reader.ReadBool();
                    if (reader.ReadBool()) // colour_description_present_flag
                    {
                        if (reader.CheckForBits(24))
                            return SPSError.ReaderOutOfRange;

                        var colour_primaries = (byte)reader.ReadBits(8);
                        var transfer_characteristics = (byte)reader.ReadBits(8);
                        var matrix_coefficients = (byte)reader.ReadBits(8);

                        ColourPrimaries = colour_primaries;
                        TransferCharacteristics = transfer_characteristics;
                        MatrixCoefficients = matrix_coefficients;
                    }
                    else
                    {
                        ColourPrimaries = 2;
                        TransferCharacteristics = 2;
                        MatrixCoefficients = 2;
                    }
                }
                else
                {
                    VideoFormat = 5;
                    ColourPrimaries = 2;
                    TransferCharacteristics = 2;
                    MatrixCoefficients = 2;
                }


                if (reader.CheckForBits(1))
                    return SPSError.ReaderOutOfRange;

                if (reader.ReadBool()) // chroma_loc_info_present_flag
                {
                    var chroma_sample_loc_type_top_field = reader.ReadUExpGolomb();
                    if (reader.Error != ReaderError.None)
                        return ConvertError(reader.Error);

                    if (chroma_sample_loc_type_top_field > 5)
                        return SPSError.InvalidChromaLocTypeField;

                    LocType.TopField = (byte)chroma_sample_loc_type_top_field;

                    var chroma_sample_loc_type_bottom_field = (byte)reader.ReadUExpGolomb();
                    if (reader.Error != ReaderError.None)
                        return ConvertError(reader.Error);

                    if (chroma_sample_loc_type_bottom_field > 5)
                        return SPSError.InvalidChromaLocTypeField;

                    LocType.BottomField = (byte)chroma_sample_loc_type_bottom_field;
                }

                if (reader.CheckForBits(1))
                    return SPSError.ReaderOutOfRange;

                if (reader.ReadBool()) // timing_info_present_flag
                {
                    if (reader.CheckForBits(65))
                        return SPSError.ReaderOutOfRange;

                    var num_units_in_tick = reader.ReadBits(32);
                    if (num_units_in_tick == 0)
                        return SPSError.InvalidTimeInfo;

                    var time_scale = reader.ReadBits(32);
                    if (time_scale == 0)
                        return SPSError.InvalidTimeInfo;

                    FixedFrameRate = reader.ReadBool();
                }

                // Done parsing for now. Remaining parameters
                // nal_hrd_parameters
                // vcl_hrd_parameters
                // bitstream_restriction
            }

            return SPSError.None;
        }

        public unsafe static int RemoveEmulationPreventionBytes(byte* buffer, int length)
        {
            int newLength = 1, i = 1;
            while (i + 2 < length)
            {
                if (buffer[i] == 0 && buffer[i + 1] == 0 && buffer[i + 2] == 3)
                {
                    buffer[newLength++] = buffer[i++];
                    buffer[newLength++] = buffer[i++];
                    i++;
                }
                else
                    buffer[newLength++] = buffer[i++];
            }

            while (i < length)
                buffer[newLength++] = buffer[i++];

            return newLength;
        }

        public static SPSError ConvertError(ReaderError error) => error switch
        {
            ReaderError.Overflow => SPSError.ReaderOverflow,
            ReaderError.OutOfRange => SPSError.ReaderOutOfRange,
            ReaderError.None => SPSError.None,
            _ => SPSError.ReaderUnknown
        };

        public void Dispose()
        {
            if (Allocator == Allocator.Invalid && Allocator == Allocator.None)
                return;

            if (ScalingMatrix != null)
                UnsafeUtility.Free(ScalingMatrix, Allocator);

            if (PicOrderCnt.OffsetRefFrame != null)
                UnsafeUtility.Free(PicOrderCnt.OffsetRefFrame, Allocator);
        }
    }
}
