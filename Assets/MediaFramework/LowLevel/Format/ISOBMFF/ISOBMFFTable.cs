using Unity.Collections.LowLevel.Unsafe;

namespace Unity.MediaFramework.LowLevel.Format.ISOBMFF
{
    public struct ISOTable
    {
        public FTYPBox.Ptr FTYP;
        public MVHDBox.Ptr MVHD;

        public UnsafeList<ISOTrackTable> Tracks;
    }

    public struct ISOTrackTable
    {
        public ISOHandler Handler;

        public TKHDBox.Ptr TKHD;
        public MDHDBox.Ptr MDHD;
        public STSDBox.Ptr STSD;
        public STTSBox.Ptr STTS;
        public STSSBox.Ptr STSS;
    }
}
