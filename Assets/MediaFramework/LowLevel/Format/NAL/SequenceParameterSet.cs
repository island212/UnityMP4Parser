using Unity.Collections;
using Unity.MediaFramework.LowLevel.Codecs;
using Unity.MediaFramework.LowLevel.Unsafe;

namespace Unity.MediaFramework.LowLevel.Format.NAL
{
    /// <summary>
    /// Sequence Parameter Set ITU-T H.264 08/2021 7.3.2.1.1
    /// https://www.itu.int/ITU-T/recommendations/rec.aspx?rec=14659&lang=en
    /// </summary>
    public unsafe struct SequenceParameterSet
    {
        public byte profile_idc;
        public byte profile_iop;
        public byte level_idc;

        public byte seq_parameter_set_id;

        public byte chroma_format_idc;
        public byte bit_depth_luma_minus8;
        public byte bit_depth_chroma_minus8;
        public byte log2_max_frame_num_minus4;

        public byte pic_order_cnt_type;

        public uint max_num_ref_frames;
        public uint pic_width_in_mbs_minus_1;
        public uint pic_height_in_map_units_minus_1;

        public uint frame_crop_left_offset;
        public uint frame_cropping_rect_right_offset;
        public uint frame_cropping_rect_top_offset;
        public uint frame_cropping_rect_bottom_offset;

        public byte aspect_ratio_idc;
        public ushort sar_width, sar_heigth;

        public byte video_format;
        public byte colour_primaries;
        public byte transfer_characteristics;
        public byte matrix_coefficients;

        public byte chroma_sample_loc_type_top_field;       // [0..5]
        public byte chroma_sample_loc_type_bottom_field;    // [0..5]

        public uint num_units_in_tick;
        public uint time_scale;

        public BitField32 flags;

        public H264Profile Profile => H264Utility.GetProfile(profile_idc, profile_iop);

        public uint FullProfileLevelID => (uint)profile_idc << 16 | (uint)profile_iop << 8 | level_idc;

        public int MaxFrameNum => 1 << (log2_max_frame_num_minus4 + 4);

        public uint PicWidth => (pic_width_in_mbs_minus_1 + 1) << 4;

        public uint PicHeigth => ((MbAdaptiveFrameField ? 2u : 1u) * (pic_height_in_map_units_minus_1 + 1)) << 4;

        public uint FrameRate => num_units_in_tick > 0 ? time_scale / num_units_in_tick : 0;

        public bool Interlaced => !FrameMbsOnly;

        public bool SeperateColourPlane { get => flags.IsSet(01); set => flags.SetBits(01, value); }

        public bool LosslessQPPrime { get => flags.IsSet(02); set => flags.SetBits(02, value); }

        public bool ScalingMatrixPresent { get => flags.IsSet(03); set => flags.SetBits(03, value); }

        public bool GapsInFrameNumValueAllowed { get => flags.IsSet(04); set => flags.SetBits(04, value); }

        public bool FrameMbsOnly { get => flags.IsSet(05); set => flags.SetBits(05, value); }

        public bool MbAdaptiveFrameField { get => flags.IsSet(06); set => flags.SetBits(06, value); }

        public bool Direct8x8Inference { get => flags.IsSet(07); set => flags.SetBits(07, value); }

        public bool FrameCropping { get => flags.IsSet(08); set => flags.SetBits(08, value); }

        public bool VUIParametersPresent { get => flags.IsSet(09); set => flags.SetBits(09, value); }

        public bool AspectRatioInfoPresent { get => flags.IsSet(10); set => flags.SetBits(10, value); }

        public bool OverscanInfoPresent { get => flags.IsSet(11); set => flags.SetBits(11, value); }

        public bool OverscanAppropriate { get => flags.IsSet(12); set => flags.SetBits(12, value); }

        public bool VideoSignalTypePresent { get => flags.IsSet(13); set => flags.SetBits(13, value); }

        public bool VideoFullRange { get => flags.IsSet(14); set => flags.SetBits(14, value); }

        public bool ColourDescriptionPresent { get => flags.IsSet(15); set => flags.SetBits(15, value); }

        public bool ChromaLocInfoPresent { get => flags.IsSet(16); set => flags.SetBits(16, value); }

        public bool TimingInfoPresent { get => flags.IsSet(17); set => flags.SetBits(17, value); }

        public bool FixedFrameRate { get => flags.IsSet(18); set => flags.SetBits(18, value); }

        public static bool Parse(byte* buffer, out SequenceParameterSet sps)
        {
            sps = new SequenceParameterSet();

            var forbidden_zero_bit = (buffer[0] & 0x80) == 0x80;
            if (forbidden_zero_bit)
                UnityEngine.Debug.LogWarning("NALU error: invalid NALU header");

            var nal_ref_id = buffer[0] & 0x60 >> 5;
            var nal_unit_type = buffer[0] & 0x1F;
            if (nal_unit_type != 7)
                UnityEngine.Debug.LogWarning("SPS error: not SPS");
         
            sps.profile_idc = buffer[1];
            sps.profile_iop = buffer[2];
            sps.level_idc = buffer[3];

            var reader = new BitReader(buffer + 4);

            sps.seq_parameter_set_id = (byte)reader.ReadUExpGolomb();
            if (sps.seq_parameter_set_id > 31)
                UnityEngine.Debug.LogWarning("SPS error: seq_parameter_set_id must be 31 or less");

            if (H264Utility.HasChroma(sps.profile_idc))
            {
                sps.chroma_format_idc = (byte)reader.ReadUExpGolomb();
                if (sps.chroma_format_idc > 3)
                    UnityEngine.Debug.LogWarning("SPS error: chroma_format_idc must be 3 or less");

                if (sps.chroma_format_idc == 3)
                    sps.SeperateColourPlane = reader.ReadBool();

                sps.bit_depth_luma_minus8 = (byte)reader.ReadUExpGolomb();
                if (sps.bit_depth_luma_minus8 > 6)
                    UnityEngine.Debug.LogWarning("SPS error: bit_depth_luma_minus8 must be 6 or less");

                sps.bit_depth_chroma_minus8 = (byte)reader.ReadUExpGolomb();
                if (sps.bit_depth_chroma_minus8 > 6)
                    UnityEngine.Debug.LogWarning("SPS error: bit_depth_chroma_minus8 must be 6 or less");

                sps.LosslessQPPrime = reader.ReadBool();

                sps.ScalingMatrixPresent = reader.ReadBool();
                if (sps.ScalingMatrixPresent)
                {
                    var scalingLength = sps.chroma_format_idc != 3 ? 8 : 12;
                    for (int i = 0; i < scalingLength; i++)
                    {
                        // We have to read over it to continue parsing
                        var seq_scaling_list_present_flag = reader.ReadBool();
                        if (seq_scaling_list_present_flag)
                        {
                            var sizeOfScalingList = i < 6 ? 16 : 64;

                            var lastScale = 8L;
                            var nextScale = 8L;
                            for (int j = 0; j < sizeOfScalingList; j++)
                            {
                                if (nextScale != 0)
                                {
                                    var deltaScale = reader.ReadSExpGolomb();
                                    nextScale = (lastScale + deltaScale + 256) % 256;
                                }
                                lastScale = nextScale == 0 ? lastScale : nextScale;
                            }
                        }
                    }
                }
            }

            sps.log2_max_frame_num_minus4 = (byte)reader.ReadUExpGolomb();
            if (sps.log2_max_frame_num_minus4 > 12)
                UnityEngine.Debug.LogWarning("SPS error: log2_max_frame_num_minus4 must be 12 or less");

            sps.pic_order_cnt_type = (byte)reader.ReadUExpGolomb();
            if (sps.pic_order_cnt_type > 2)
                UnityEngine.Debug.LogWarning("SPS error: pic_order_cnt_type must be 2 or less");

            switch (sps.pic_order_cnt_type)
            {
                case 0:
                    var log2_max_pic_order_cnt_lsb_minus4 = reader.ReadUExpGolomb();
                    break;
                case 1:
                    var delta_pic_order_always_zero_flag = reader.ReadBit();
                    var offset_for_non_ref_pic = reader.ReadSExpGolomb();
                    var offset_for_top_to_bottom_field = reader.ReadSExpGolomb();
                    var num_ref_frames_in_pic_order_cnt_cycle = reader.ReadUExpGolomb();
                    for (int i = 0; i < num_ref_frames_in_pic_order_cnt_cycle; i++)
                    {
                        var offsetForRefFrame = reader.ReadSExpGolomb();
                    }
                    break;
                case 2:
                    break;
            }

            sps.max_num_ref_frames = reader.ReadUExpGolomb();
            sps.GapsInFrameNumValueAllowed = reader.ReadBool();
            sps.pic_width_in_mbs_minus_1 = reader.ReadUExpGolomb();
            sps.pic_height_in_map_units_minus_1 = reader.ReadUExpGolomb();
            sps.FrameMbsOnly = reader.ReadBool();
            if (!sps.FrameMbsOnly)
                sps.MbAdaptiveFrameField = reader.ReadBool();

            sps.Direct8x8Inference = reader.ReadBool();

            sps.FrameCropping = reader.ReadBool();
            if (sps.FrameCropping)
            {
                sps.frame_crop_left_offset = reader.ReadUExpGolomb();
                sps.frame_cropping_rect_right_offset = reader.ReadUExpGolomb();
                sps.frame_cropping_rect_top_offset = reader.ReadUExpGolomb();
                sps.frame_cropping_rect_bottom_offset = reader.ReadUExpGolomb();
            }

            sps.VUIParametersPresent = reader.ReadBool();
            if (sps.VUIParametersPresent)
            {
                sps.AspectRatioInfoPresent = reader.ReadBool();
                if (sps.AspectRatioInfoPresent)
                {
                    sps.aspect_ratio_idc = (byte)reader.ReadBits(8);
                    if (sps.aspect_ratio_idc == 255) // 255 = Extended_SAR Table E-1
                    {
                        sps.sar_width = (ushort)reader.ReadBits(16);
                        sps.sar_heigth = (ushort)reader.ReadBits(16);
                    }
                }

                sps.OverscanInfoPresent = reader.ReadBool();
                if (sps.OverscanInfoPresent)
                    sps.OverscanAppropriate = reader.ReadBool();

                sps.VideoSignalTypePresent = reader.ReadBool();
                if (sps.VideoSignalTypePresent)
                {
                    sps.video_format = (byte)reader.ReadBits(3);
                    sps.VideoFullRange = reader.ReadBool();
                    sps.ColourDescriptionPresent = reader.ReadBool();
                    if (sps.ColourDescriptionPresent)
                    {
                        sps.colour_primaries = (byte)reader.ReadBits(8);
                        sps.transfer_characteristics = (byte)reader.ReadBits(8);
                        sps.matrix_coefficients = (byte)reader.ReadBits(8);
                    }
                }

                sps.ChromaLocInfoPresent = reader.ReadBool();
                if (sps.ChromaLocInfoPresent)
                {
                    sps.chroma_sample_loc_type_top_field = (byte)reader.ReadUExpGolomb();
                    sps.chroma_sample_loc_type_bottom_field = (byte)reader.ReadUExpGolomb();
                }

                sps.TimingInfoPresent = reader.ReadBool();
                if (sps.TimingInfoPresent)
                {
                    sps.num_units_in_tick = reader.ReadBits(32);
                    sps.time_scale = reader.ReadBits(32);
                    sps.FixedFrameRate = reader.ReadBool();
                }

                // Done parsing for now. Remaining parameters
                // nal_hrd_parameters
                // vcl_hrd_parameters
                // bitstream_restriction
            }

            return true;
        }
    }
}
