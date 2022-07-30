using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public class ParseMethod
{
    [Test, Performance]
    public unsafe void CompareParseMethodWithAndWithoutCheck()
    {
        byte[] spsSmall =
        {
            0x67, 0x42, 0xC0, 0x1E, 0x9E, 0x21, 0x81, 0x18, 0x53, 0x4D, 0x40, 0x40,
            0x40, 0x50, 0x00, 0x00, 0x03, 0x00, 0x10, 0x00, 0x00, 0x03, 0x03, 0xC8,
            0xF1, 0x62, 0xEE
        };

        var measurementCount = 20;
        var warmupCount = 20;
        var iterationsPerMeasurement = 4000;

        using var sps = new SPS();

        Measure.Method(() =>
        {
            fixed (byte* ptr = spsSmall)
            {
                sps.Parse(ptr, spsSmall.Length, Allocator.Temp);
            }
        })
        .SampleGroup("SPS with check")
        .WarmupCount(warmupCount)
        .IterationsPerMeasurement(iterationsPerMeasurement)
        .MeasurementCount(measurementCount)
        .Run();

        Measure.Method(() =>
        {
            fixed (byte* ptr = spsSmall)
            {
                sps.ParseWithoutCheck(ptr, spsSmall.Length, Allocator.Temp);
            }
        })
        .SampleGroup("SPS without check")
        .WarmupCount(warmupCount)
        .IterationsPerMeasurement(iterationsPerMeasurement)
        .MeasurementCount(measurementCount)
        .Run();
    }

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

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public struct SPSProfile
    {
        public uint ProfileLevelId => (uint)Type << 16 | (uint)Constraints << 8 | Level;

        public byte Type;
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

        public byte ArrayType => SeparateColourPlane ? (byte)0 : (byte)Format;

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

    public struct SPSAspectRatio
    {
        public const byte Extend_SAR = 255;

        public ushort Width, Height;

        public SPSAspectRatio(ushort width, ushort height)
        {
            Width = width;
            Height = height;
        }

        public static SPSAspectRatio GetSAR(byte indicator) => indicator switch
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

    public struct SPSFramerate
    {
        public double Value => TimeScale > 0 ? (double)NumUnitsInTick / TimeScale : 0;

        public uint NumUnitsInTick;
        public uint TimeScale;
    }

    /// <summary>
    /// Sequence Parameter Set ITU-T H.264 08/2021 7.3.2.1.1
    /// https://www.itu.int/ITU-T/recommendations/rec.aspx?rec=14659&lang=en
    /// </summary>
    public unsafe struct SPS : IDisposable
    {
        public Allocator Allocator;

        public uint ID;
        public SPSProfile Profile;
        public SPSChromaFormat Chroma;

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
        public SPSFramerate Framerate;

        public uint PictureWidth => MbWidth << 4;

        public uint PictureHeigth => MbHeigth << 4;

        public bool DeltaAlwaysZero { get => Flags.IsSet(00); set => Flags.SetBits(00, value); }

        public bool GapsInFrameNumValueAllowed { get => Flags.IsSet(01); set => Flags.SetBits(01, value); }

        public bool FrameMbsOnly { get => Flags.IsSet(02); set => Flags.SetBits(02, value); }

        public bool MbAdaptiveFrameField { get => Flags.IsSet(03); set => Flags.SetBits(03, value); }

        public bool Direct8x8Inference { get => Flags.IsSet(04); set => Flags.SetBits(04, value); }

        public bool OverscanAppropriate { get => Flags.IsSet(05); set => Flags.SetBits(05, value); }

        public bool VideoFullRange { get => Flags.IsSet(06); set => Flags.SetBits(06, value); }

        public bool FixedFrameRate { get => Flags.IsSet(07); set => Flags.SetBits(07, value); }

        public SPS(byte* buffer, int length, Allocator allocator)
        {
            this = default;
            Parse(buffer, length, allocator);
        }

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

            Profile.Type = buffer[1];          // profile_idc
            Profile.Constraints = buffer[2];    // profile_iop
            Profile.Level = buffer[3];          // level_idc

            length = RemoveEmulationPreventionBytes(buffer, length);

            var reader = new BitReader(buffer + 4, length - 4);

            var seq_parameter_set_id = reader.ReadUExpGolomb();
            if (reader.Error != ReaderError.None)
                return ConvertError(reader.Error);

            if (seq_parameter_set_id > 31)
                return SPSError.InvalidSeqParamaterSetId;

            ID = seq_parameter_set_id;

            if (HasChroma(Profile.Type))
            {
                var chroma_format = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                if (chroma_format > 3)
                    return SPSError.InvalidChromaFormat;

                Chroma.Format = (ChromaSubsampling)chroma_format;

                if (Chroma.Format == ChromaSubsampling.YUV444)
                {
                    if (!reader.HasEnoughBits(1))
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

                if (!reader.HasEnoughBits(1))
                    return SPSError.ReaderOutOfRange;

                Chroma.Flags |= reader.ReadBool() ? SPSChromaFormat.Flag.TransformBypass : 0; // qpprime_y_zero_transform_bypass_flag

                if (!reader.HasEnoughBits(1))
                    return SPSError.ReaderOutOfRange;

                if (reader.ReadBool()) // seq_scaling_matrix_present_flag
                {
                    // Simpler for the test without ScalingMatrix
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

            if (pic_order_cnt_type > 2)
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

            if (!reader.HasEnoughBits(1))
                return SPSError.ReaderOutOfRange;

            GapsInFrameNumValueAllowed = reader.ReadBool();

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

            if (!reader.HasEnoughBits(1))
                return SPSError.ReaderOutOfRange;

            FrameMbsOnly = reader.ReadBool();

            if (!FrameMbsOnly)
            {
                MbHeigth *= 2;

                if (!reader.HasEnoughBits(1))
                    return SPSError.ReaderOutOfRange;

                MbAdaptiveFrameField = reader.ReadBool();
            }

            if (!reader.HasEnoughBits(1))
                return SPSError.ReaderOutOfRange;

            Direct8x8Inference = reader.ReadBool();

            if (!reader.HasEnoughBits(1))
                return SPSError.ReaderOutOfRange;

            if (reader.ReadBool()) //frame_cropping
            {
                var cropUnitX = Chroma.ArrayType == 0 ? 1 : Chroma.SubWidthC;
                var cropUnitY = Chroma.ArrayType == 0 ? 1 : Chroma.SubHeightC;
                cropUnitY *= FrameMbsOnly ? 1u : 2u;

                var frame_crop_left_offset = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                CropLeft = frame_crop_left_offset * cropUnitX;

                var frame_crop_right_offset = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                CropRight = frame_crop_right_offset * cropUnitX;

                var frame_crop_top_offset = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                CropTop = frame_crop_top_offset * cropUnitY;

                var frame_crop_bottom_offset = reader.ReadUExpGolomb();
                if (reader.Error != ReaderError.None)
                    return ConvertError(reader.Error);

                CropBottom = frame_crop_bottom_offset * cropUnitY;

                uint width = MbWidth << 4;
                uint height = MbHeigth << 4;

                if (CropLeft + CropRight > width || CropTop + CropBottom > height)
                    return SPSError.InvalidCrop;
            }

            if (!reader.HasEnoughBits(1))
                return SPSError.ReaderOutOfRange;

            if (reader.ReadBool()) // vui_parameters_present_flag
            {
                if (!reader.HasEnoughBits(1))
                    return SPSError.ReaderOutOfRange;

                if (reader.ReadBool()) // aspect_ratio_info_present_flag
                {
                    if (!reader.HasEnoughBits(8))
                        return SPSError.ReaderOutOfRange;

                    var aspect_ratio_idc = reader.ReadBits(8);
                    if (aspect_ratio_idc > 16 && aspect_ratio_idc != 255)
                        return SPSError.InvalidAspectIndicator;

                    if (aspect_ratio_idc == SPSAspectRatio.Extend_SAR)
                    {
                        if (!reader.HasEnoughBits(32))
                            return SPSError.ReaderOutOfRange;

                        AspectRatio.Width = (ushort)reader.ReadBits(16);
                        AspectRatio.Height = (ushort)reader.ReadBits(16);
                    }
                    else
                    {
                        AspectRatio = SPSAspectRatio.GetSAR((byte)aspect_ratio_idc);
                    }
                }

                if (!reader.HasEnoughBits(1))
                    return SPSError.ReaderOutOfRange;

                if (reader.ReadBool()) // overscan_info_present_flag
                {
                    if (!reader.HasEnoughBits(1))
                        return SPSError.ReaderOutOfRange;

                    OverscanAppropriate = reader.ReadBool();
                }

                if (!reader.HasEnoughBits(1))
                    return SPSError.ReaderOutOfRange;

                if (reader.ReadBool()) // video_signal_type_present_flag
                {
                    if (!reader.HasEnoughBits(5))
                        return SPSError.ReaderOutOfRange;

                    var video_format = reader.ReadBits(3);
                    if (video_format > 5)
                        return SPSError.InvalidVideoFormat;

                    VideoFormat = (byte)video_format;

                    VideoFullRange = reader.ReadBool();
                    if (reader.ReadBool()) // colour_description_present_flag
                    {
                        if (!reader.HasEnoughBits(24))
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


                if (!reader.HasEnoughBits(1))
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

                if (!reader.HasEnoughBits(1))
                    return SPSError.ReaderOutOfRange;

                if (reader.ReadBool()) // timing_info_present_flag
                {
                    if (!reader.HasEnoughBits(65))
                        return SPSError.ReaderOutOfRange;

                    var num_units_in_tick = reader.ReadBits(32);
                    if (num_units_in_tick == 0)
                        return SPSError.InvalidTimeInfo;

                    Framerate.NumUnitsInTick = num_units_in_tick;

                    var time_scale = reader.ReadBits(32);
                    if (time_scale == 0)
                        return SPSError.InvalidTimeInfo;

                    Framerate.TimeScale = time_scale;

                    FixedFrameRate = reader.ReadBool();
                }

                // Done parsing for now. Remaining parameters
                // nal_hrd_parameters
                // vcl_hrd_parameters
                // bitstream_restriction
            }

            return SPSError.None;
        }

        public unsafe SPSError ParseWithoutCheck(byte* buffer, int length, Allocator allocator)
        {
            Profile.Type = buffer[1];          // profile_idc
            Profile.Constraints = buffer[2];    // profile_iop
            Profile.Level = buffer[3];          // level_idc

            length = RemoveEmulationPreventionBytes(buffer, length);

            var reader = new BitReader(buffer + 4, length - 4);

            var seq_parameter_set_id = reader.ReadUExpGolomb();

            ID = seq_parameter_set_id;

            if (HasChroma(Profile.Type))
            {
                var chroma_format = reader.ReadUExpGolomb();

                Chroma.Format = (ChromaSubsampling)chroma_format;

                if (Chroma.Format == ChromaSubsampling.YUV444)
                {
                    Chroma.Flags |= reader.ReadBool() ? SPSChromaFormat.Flag.SeparateColourPlane : 0;
                }

                var bit_depth_luma_minus8 = reader.ReadUExpGolomb();

                Chroma.BitDepthLuma = (byte)(bit_depth_luma_minus8 + 8);

                var bit_depth_chroma_minus8 = reader.ReadUExpGolomb();

                Chroma.BitDepthChroma = (byte)(bit_depth_chroma_minus8 + 8);

                Chroma.Flags |= reader.ReadBool() ? SPSChromaFormat.Flag.TransformBypass : 0; // qpprime_y_zero_transform_bypass_flag

                if (reader.ReadBool()) // seq_scaling_matrix_present_flag
                {
                    // Simpler for the test without ScalingMatrix
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

            MaxFrameNum = 1u << (int)(log2_max_frame_num_minus4 + 4);

            var pic_order_cnt_type = reader.ReadUExpGolomb();

            PicOrderCnt.Type = (byte)pic_order_cnt_type;

            switch (PicOrderCnt.Type)
            {
                case 0:
                    var log2_max_pic_order_cnt_lsb_minus4 = reader.ReadUExpGolomb();

                    PicOrderCnt.MaxLsb = 1u << (int)(log2_max_pic_order_cnt_lsb_minus4 + 4);
                    break;
                case 1:
                    DeltaAlwaysZero = reader.ReadBool();

                    var offset_for_non_ref_pic = reader.ReadSExpGolomb();

                    PicOrderCnt.OffsetForNonRefPic = offset_for_non_ref_pic;

                    var offset_for_top_to_bottom_field = reader.ReadSExpGolomb();

                    PicOrderCnt.OffsetForTopToBottomField = offset_for_top_to_bottom_field;

                    var num_ref_frames_in_pic_order_cnt_cycle = reader.ReadUExpGolomb();

                    PicOrderCnt.NumRefFramesInCycle = (byte)num_ref_frames_in_pic_order_cnt_cycle;
                    if (num_ref_frames_in_pic_order_cnt_cycle > 0)
                    {
                        Allocator = allocator;
                        if (PicOrderCnt.OffsetRefFrame == null)
                            PicOrderCnt.OffsetRefFrame = (int*)UnsafeUtility.Malloc(num_ref_frames_in_pic_order_cnt_cycle * sizeof(int), 4, allocator);

                        for (int i = 0; i < num_ref_frames_in_pic_order_cnt_cycle; i++)
                        {
                            var offsetForRefFrame = reader.ReadSExpGolomb();

                            PicOrderCnt.OffsetRefFrame[i] = offsetForRefFrame;
                        }
                    }
                    break;
            }

            var max_num_ref_frames = reader.ReadUExpGolomb();

            PicOrderCnt.MaxNumRefFrames = (byte)max_num_ref_frames;

            GapsInFrameNumValueAllowed = reader.ReadBool();

            var pic_width_in_mbs_minus_1 = reader.ReadUExpGolomb();

            MbWidth = pic_width_in_mbs_minus_1 + 1;

            var pic_height_in_map_units_minus_1 = reader.ReadUExpGolomb();

            MbHeigth = pic_height_in_map_units_minus_1 + 1;

            FrameMbsOnly = reader.ReadBool();

            if (!FrameMbsOnly)
            {
                MbHeigth *= 2;

                MbAdaptiveFrameField = reader.ReadBool();
            }

            Direct8x8Inference = reader.ReadBool();

            if (reader.ReadBool()) //frame_cropping
            {
                var cropUnitX = Chroma.ArrayType == 0 ? 1 : Chroma.SubWidthC;
                var cropUnitY = Chroma.ArrayType == 0 ? 1 : Chroma.SubHeightC;
                cropUnitY *= FrameMbsOnly ? 1u : 2u;

                var frame_crop_left_offset = reader.ReadUExpGolomb();

                CropLeft = frame_crop_left_offset * cropUnitX;

                var frame_crop_right_offset = reader.ReadUExpGolomb();

                CropRight = frame_crop_right_offset * cropUnitX;

                var frame_crop_top_offset = reader.ReadUExpGolomb();

                CropTop = frame_crop_top_offset * cropUnitY;

                var frame_crop_bottom_offset = reader.ReadUExpGolomb();

                CropBottom = frame_crop_bottom_offset * cropUnitY;
            }

            if (reader.ReadBool()) // vui_parameters_present_flag
            {
                if (reader.ReadBool()) // aspect_ratio_info_present_flag
                {
                    var aspect_ratio_idc = reader.ReadBits(8);

                    if (aspect_ratio_idc == SPSAspectRatio.Extend_SAR)
                    {
                        AspectRatio.Width = (ushort)reader.ReadBits(16);
                        AspectRatio.Height = (ushort)reader.ReadBits(16);
                    }
                    else
                    {
                        AspectRatio = SPSAspectRatio.GetSAR((byte)aspect_ratio_idc);
                    }
                }

                if (reader.ReadBool()) // overscan_info_present_flag
                {
                    OverscanAppropriate = reader.ReadBool();
                }

                if (reader.ReadBool()) // video_signal_type_present_flag
                {
                    var video_format = reader.ReadBits(3);

                    VideoFormat = (byte)video_format;

                    VideoFullRange = reader.ReadBool();
                    if (reader.ReadBool()) // colour_description_present_flag
                    {
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

                if (reader.ReadBool()) // chroma_loc_info_present_flag
                {
                    var chroma_sample_loc_type_top_field = reader.ReadUExpGolomb();

                    LocType.TopField = (byte)chroma_sample_loc_type_top_field;

                    var chroma_sample_loc_type_bottom_field = (byte)reader.ReadUExpGolomb();

                    LocType.BottomField = (byte)chroma_sample_loc_type_bottom_field;
                }

                if (reader.ReadBool()) // timing_info_present_flag
                {
                    var num_units_in_tick = reader.ReadBits(32);

                    Framerate.NumUnitsInTick = num_units_in_tick;

                    var time_scale = reader.ReadBits(32);

                    Framerate.TimeScale = time_scale;

                    FixedFrameRate = reader.ReadBool();
                }

                // Done parsing for now. Remaining parameters
                // nal_hrd_parameters
                // vcl_hrd_parameters
                // bitstream_restriction
            }

            return SPSError.None;
        }

        public static bool HasChroma(byte profile_idc) => profile_idc switch
        {
            44 or 83 or 86 or 100 or 110 or 118 or 122 or 128 or 134 or 135 or 138 or 139 or 244 => true,
            _ => false
        };

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

            if (PicOrderCnt.OffsetRefFrame != null)
                UnsafeUtility.Free(PicOrderCnt.OffsetRefFrame, Allocator);
        }
    }

    public enum ReaderError
    {
        None,
        Overflow,
        OutOfRange
    }

    public unsafe struct BitReader
    {
        public ReaderError Error;
        public int Position;
        public int Length;
        public byte* Stream;

        public BitReader(byte* buffer, int length)
        {
            Error = ReaderError.None;

            Position = 0;
            Stream = buffer;
            Length = length * 8;
        }

        public bool HasEnoughBits(int bits) => Length - Position >= bits;

        public byte ReadBit()
        {
            var p = (Position >> 3);
            var o = 0x07 - (Position & 0x07);
            var val = (Stream[p] >> o) & 0x01;
            Position++;
            return (byte)val;
        }

        public uint ReadBits(int bits)
        {
            uint value = 0;
            for (int i = 0; i < bits; i++)
            {
                value = (value << 1) | ReadBit();
            }
            return value;
        }

        public bool ReadBool() => ReadBit() == 1;

        public uint ReadUExpGolomb()
        {
            if (!HasEnoughBits(1))
            {
                Error = ReaderError.OutOfRange;
                return 0;
            }

            var zeros = 0;
            while (ReadBit() == 0)
            {
                zeros++;
                if (!HasEnoughBits(zeros * 2 + 1))
                {
                    Error = ReaderError.OutOfRange;
                    return 0;
                }

                if (zeros > 32)
                {
                    Error = ReaderError.Overflow;
                    return 0;
                }
            }

            var value = 1u << zeros;
            for (var i = zeros - 1; i >= 0; i--)
                value |= (uint)(ReadBit() << i);

            return value - 1;
        }

        public int ReadSExpGolomb()
        {
            var value = ReadUExpGolomb();
            return value == 0 || Error != ReaderError.None ? 0 :
                (value & 0x01) == 0 ? 1 + (int)(value >> 1) : -(int)(value >> 1);
        }
    }
}
