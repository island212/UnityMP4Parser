using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.MediaFramework.LowLevel.Format.NAL;
using UnityEngine;

public class SPSParseTests
{
    readonly byte[] spsSmall =
    {
        0x67, 0x42, 0xC0, 0x1E, 0x9E, 0x21, 0x81, 0x18, 0x53, 0x4D, 0x40, 0x40,
        0x40, 0x50, 0x00, 0x00, 0x03, 0x00, 0x10, 0x00, 0x00, 0x03, 0x03, 0xC8,
        0xF1, 0x62, 0xEE
    };

    readonly byte[] spsBasic =
    {
            0x67, 0x42, 0xC0, 0x0D, 0x95, 0xB0, 0x50, 0x6F, 0xE5, 0xC0, 0x44,
            0x00, 0x00, 0x03, 0x00, 0x04, 0x00, 0x00, 0x03, 0x00, 0xF0, 0x36, 0x82,
            0x21, 0x1B
    };

    [Test]
    public unsafe void ISOBoxParse_ValidSPS_ReturnExactValue()
    {
        var test = spsSmall;
        fixed (byte* ptr = test)
        {
            // With using, the SPS will be disposed after we go out of the scope
            using var sps = new SequenceParameterSet();
            var error = sps.Parse(ptr, test.Length, Allocator.Temp);

            Assert.AreEqual(SPSError.None, error, "SPSError");

            Assert.AreEqual(66, sps.Profile.Type, "Profile");
            Assert.AreEqual(192, sps.Profile.Constraints, "Constraints");
            Assert.AreEqual(30, sps.Profile.Level, "Level");

            Assert.AreEqual(0, sps.ID, "ID");

            Assert.AreEqual(ChromaSubsampling.YUV420, sps.Chroma.Format, "ChromaFormat");
            Assert.AreEqual(8, sps.Chroma.BitDepthLuma, "BitDepthLuma");
            Assert.AreEqual(8, sps.Chroma.BitDepthChroma, "BitDepthChroma");

            Assert.IsTrue(sps.ScalingMatrix == null, "ScalingMatrix");

            Assert.AreEqual(1024, sps.MaxFrameNum, "MaxFrameNum");

            Assert.AreEqual(0, sps.PicOrderCnt.Type, "PicOrderCntType");
            Assert.AreEqual(2, sps.PicOrderCnt.MaxNumRefFrames, "MaxNumRefFrames");
            Assert.AreEqual(2048, sps.PicOrderCnt.MaxLsb, "MaxLsb");
            Assert.AreEqual(0, sps.PicOrderCnt.OffsetForNonRefPic, "OffsetForNonRefPic");
            Assert.AreEqual(0, sps.PicOrderCnt.OffsetForTopToBottomField, "OffsetForTopToBottomField");
            Assert.AreEqual(0, sps.PicOrderCnt.NumRefFramesInCycle, "NumRefFramesInCycle");
            Assert.IsTrue(sps.PicOrderCnt.OffsetRefFrame == null, "OffsetRefFrame");

            Assert.IsFalse(sps.GapsInFrameNumValueAllowed, "GapsInFrameNumValueAllowed");       
            Assert.AreEqual(35, sps.MbWidth, "MbWidth");
            Assert.AreEqual(20, sps.MbHeigth, "MbHeigth");
            Assert.IsTrue(sps.FrameMbsOnly, "FrameMbsOnly");
            Assert.IsFalse(sps.MbAdaptiveFrameField, "MbAdaptiveFrameField");
            Assert.IsTrue(sps.Direct8x8Inference, "Direct8x8Inference");

            Assert.AreEqual(0, sps.CropLeft, "CropLeft");
            Assert.AreEqual(0, sps.CropRight, "CropRight");
            Assert.AreEqual(0, sps.CropTop, "CropTop");
            Assert.AreEqual(0, sps.CropBottom, "CropBottom");

            Assert.AreEqual(0, sps.AspectRatio.Type, "AspectRatio");
            Assert.AreEqual(0, sps.AspectRatio.SARWidth, "SARWidth");
            Assert.AreEqual(0, sps.AspectRatio.SARHeigth, "SARHeigth");

            Assert.AreEqual(5, sps.VideoFormat, "VideoFormat");
            Assert.IsFalse(sps.VideoFullRange, "VideoFullRange");
            Assert.AreEqual(1, sps.ColourPrimaries, "ColourPrimaries");
            Assert.AreEqual(1, sps.TransferCharacteristics, "TransferCharacteristics");
            Assert.AreEqual(1, sps.MatrixCoefficients, "MatrixCoefficients");

            Assert.AreEqual(0, sps.LocType.TopField, "ChromaSampleLocType TopField");
            Assert.AreEqual(0, sps.LocType.BottomField, "ChromaSampleLocType BottomField");

            Assert.AreEqual(1, sps.Time.NumUnitsInTick, "NumUnitsInTick");
            Assert.AreEqual(60, sps.Time.TimeScale, "TimeScale");
        }
    }
}
