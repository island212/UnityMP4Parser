using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.MediaFramework.LowLevel.Codecs
{
    public enum H264Profile : ushort
    {
        Baseline = H264ProfileCode.Baseline << 8,
        ConstrainedBaseline = H264ProfileCode.Baseline << 8 | 0x40,
        Extended = H264ProfileCode.Extended << 8,
        Main = H264ProfileCode.Main << 8,
        High = H264ProfileCode.High << 8,
        ProgressiveHigh = H264ProfileCode.High << 8 | 0x08,
        ConstrainedHigh = H264ProfileCode.High << 8 | 0x0C,
        High10 = H264ProfileCode.High10 << 8,
        High10Intra = H264ProfileCode.High10 << 8 | 0x10,
        High422 = H264ProfileCode.High422 << 8,
        High422Intra = H264ProfileCode.High422 << 8 | 0x10,
        High444Predictive = H264ProfileCode.High444 << 8,
        High444Intra = H264ProfileCode.High444 << 8 | 0x10,
        CAVLC444Intra = H264ProfileCode.CAVLC444Intra << 8,
        ScalableBaseline = H264ProfileCode.ScalableBaseline << 8,
        ScalableConstrainedBaseline = H264ProfileCode.ScalableBaseline << 8 | 0x04,
        ScalableHigh = H264ProfileCode.ScalableHigh << 8,
        ScalableConstrainedHigh = H264ProfileCode.ScalableHigh << 8 | 0x04,
        ScalableHighIntra = H264ProfileCode.ScalableHigh << 8 | 0x10,
        StereoHighProfile = H264ProfileCode.StereoHighProfile << 8,
        MultiviewHighProfile = H264ProfileCode.MultiviewHighProfile << 8,
        MFCHigh = H264ProfileCode.MFCHigh << 8,
        MFCDepthHigh = H264ProfileCode.MFCDepthHigh << 8,
        MultiviewDepthHigh = H264ProfileCode.MultiviewDepthHigh << 8,
        EnhancedMultiviewDepthHigh = H264ProfileCode.EnhancedMultiviewDepthHigh << 8,
    }

    public enum H264ProfileCode : byte
    {
        Baseline = 66,
        Extended = 88,
        Main = 77,
        High = 100,
        High10 = 110,
        High422 = 122,
        High444 = 244,
        CAVLC444Intra = 44,
        ScalableBaseline = 83,
        ScalableHigh = 86,
        StereoHighProfile = 128,
        MultiviewHighProfile = 118,
        MFCHigh = 134,
        MFCDepthHigh = 135,
        MultiviewDepthHigh = 138,
        EnhancedMultiviewDepthHigh = 139
    }

    public static class H264Utility
    {
        public static H264Profile GetProfile(byte profile_idc, byte profile_iop)
        {
            switch ((H264ProfileCode)profile_idc)
            {
                case H264ProfileCode.Baseline:
                    if ((profile_iop & 0x40) == 0x40)
                        return H264Profile.ConstrainedBaseline;
                    return H264Profile.Baseline;

                case H264ProfileCode.Extended:
                    if ((profile_iop & 0xC0) == 0xC0)
                        return H264Profile.ConstrainedBaseline;
                    if ((profile_iop & 0x80) == 0x80)
                        return H264Profile.Baseline;
                    return H264Profile.Extended;

                case H264ProfileCode.Main:
                    if ((profile_iop & 0x80) == 0x80)
                        return H264Profile.ConstrainedBaseline;
                    return H264Profile.Main;

                case H264ProfileCode.High:
                    if ((profile_iop & 0x0C) == 0x0C)
                        return H264Profile.ConstrainedHigh;
                    if ((profile_iop & 0x08) == 0x08)
                        return H264Profile.ProgressiveHigh;
                    return H264Profile.High;

                case H264ProfileCode.High10:
                    if ((profile_iop & 0x04) == 0x04)
                        return H264Profile.High10Intra;
                    return H264Profile.High10;

                case H264ProfileCode.High422:
                    if ((profile_iop & 0x04) == 0x04)
                        return H264Profile.High422Intra;
                    return H264Profile.High422;

                case H264ProfileCode.High444:
                    if ((profile_iop & 0x04) == 0x04)
                        return H264Profile.High444Intra;
                    return H264Profile.High444Predictive;

                case H264ProfileCode.CAVLC444Intra:
                    return H264Profile.CAVLC444Intra;

                case H264ProfileCode.ScalableBaseline:
                    if ((profile_iop & 0x04) == 0x04)
                        return H264Profile.ScalableConstrainedBaseline;
                    return H264Profile.ScalableBaseline;

                case H264ProfileCode.ScalableHigh:
                    if ((profile_iop & 0x10) == 0x10)
                        return H264Profile.ScalableHighIntra;
                    if ((profile_iop & 0x04) == 0x04)
                        return H264Profile.ScalableConstrainedHigh;
                    return H264Profile.ScalableHigh;

                case H264ProfileCode.StereoHighProfile:
                    return H264Profile.StereoHighProfile;

                case H264ProfileCode.MultiviewHighProfile:
                    return H264Profile.MultiviewHighProfile;

                case H264ProfileCode.MFCHigh:
                    return H264Profile.MFCHigh;

                case H264ProfileCode.MFCDepthHigh:
                    return H264Profile.MFCDepthHigh;

                case H264ProfileCode.MultiviewDepthHigh:
                    return H264Profile.MultiviewDepthHigh;

                case H264ProfileCode.EnhancedMultiviewDepthHigh:
                    return H264Profile.EnhancedMultiviewDepthHigh;

                default:
                    return (H264Profile)(profile_idc << 8 | profile_iop);
            }
        }

        public static bool HasChroma(byte profile_idc) => profile_idc switch
        {
            44 or 83 or 86 or 100 or 110 or 118 or 122 or 128 or 134 or 135 or 138 or 139 or 244 => true,
            _ => false
        };
    }
}
