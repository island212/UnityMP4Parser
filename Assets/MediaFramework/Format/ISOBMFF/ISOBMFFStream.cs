using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel;

namespace Unity.MediaFramework.Format.ISOBMFF
{
    public static class ISOByteWriterExtensions
    {
        public static void WriteMVHD(this ByteWriter writer, in MVHDBox box)
        {
            writer.WriteUInt32((uint)MVHDBox.GetSize(box.Version));
            writer.WriteUInt32((uint)ISOBoxType.MVHD);
            writer.WriteUInt8(box.Version);
            writer.WriteUInt24(0);

            if (box.Version == 1)
            {
                writer.WriteUInt64(box.CreationTime.value);
                writer.WriteUInt64(box.ModificationTime.value);
                writer.WriteUInt32(box.Timescale);
                writer.WriteUInt64(box.Duration);
            }
            else
            {
                writer.WriteUInt32((uint)box.CreationTime.value);
                writer.WriteUInt32((uint)box.ModificationTime.value);
                writer.WriteUInt32(box.Timescale);
                writer.WriteUInt32((uint)box.Duration);
            }

            writer.WriteInt32(box.Rate.value);
            writer.WriteInt16(box.Volume.value);
            writer.WriteBytes(0, 10);

            writer.WriteInt32(box.Matrix.value.c0.x);
            writer.WriteInt32(box.Matrix.value.c0.y);
            writer.WriteInt32(box.Matrix.value.c0.z);
            writer.WriteInt32(box.Matrix.value.c1.x);
            writer.WriteInt32(box.Matrix.value.c1.y);
            writer.WriteInt32(box.Matrix.value.c1.z);
            writer.WriteInt32(box.Matrix.value.c2.x);
            writer.WriteInt32(box.Matrix.value.c2.y);
            writer.WriteInt32(box.Matrix.value.c2.z);

            writer.WriteBytes(0, 4 * 6);
            writer.WriteUInt32(box.NextTrackID);
        }

        public static void WriteFTYP(this ByteWriter writer, in FTYPBox box)
        {
            int brandCount = math.min(box.BrandCount, FTYPBox.MaxCachedBrands);

            writer.WriteUInt32((uint)FTYPBox.GetSize(brandCount));
            writer.WriteUInt32((uint)ISOBoxType.FTYP);
            writer.WriteUInt32((uint)box.MajorBrand);
            writer.WriteUInt32(box.MinorVersion);

            switch (brandCount)
            {
                case 0: break;
                case 1:
                    writer.WriteUInt32((uint)box.Brand0);
                    break;
                case 2:
                    writer.WriteUInt32((uint)box.Brand0);
                    writer.WriteUInt32((uint)box.Brand1);
                    break;
                case 3:
                    writer.WriteUInt32((uint)box.Brand0);
                    writer.WriteUInt32((uint)box.Brand1);
                    writer.WriteUInt32((uint)box.Brand2);
                    break;
                case 4:
                    writer.WriteUInt32((uint)box.Brand0);
                    writer.WriteUInt32((uint)box.Brand1);
                    writer.WriteUInt32((uint)box.Brand2);
                    writer.WriteUInt32((uint)box.Brand3);
                    break;
                case 5:
                    writer.WriteUInt32((uint)box.Brand0);
                    writer.WriteUInt32((uint)box.Brand1);
                    writer.WriteUInt32((uint)box.Brand2);
                    writer.WriteUInt32((uint)box.Brand3);
                    writer.WriteUInt32((uint)box.Brand4);
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        public static void WriteFTYP(this ByteWriter writer, ISOBrand major, uint minor, in NativeArray<ISOBrand> brands)
        {
            writer.WriteUInt32((uint)FTYPBox.GetSize(brands.Length));
            writer.WriteUInt32((uint)ISOBoxType.FTYP);
            writer.WriteUInt32((uint)major);
            writer.WriteUInt32(minor);

            foreach (var brand in brands)
            {
                writer.WriteUInt32((uint)brand);
            }
        }

        public static void WriteTKHD(this ByteWriter writer, in TKHDBox box)
        {
            writer.WriteUInt32((uint)TKHDBox.GetSize(box.Version));
            writer.WriteUInt32((uint)ISOBoxType.TKHD);
            writer.WriteUInt8(box.Version);
            writer.WriteUInt24((uint)box.Flags);

            if (box.Version == 1)
            {
                writer.WriteUInt64(box.CreationTime.value);
                writer.WriteUInt64(box.ModificationTime.value);
                writer.WriteUInt32(box.TrackID);
                writer.WriteBytes(0, 4);
                writer.WriteUInt64(box.Duration);
            }
            else
            {
                writer.WriteUInt32((uint)box.CreationTime.value);
                writer.WriteUInt32((uint)box.ModificationTime.value);
                writer.WriteUInt32(box.TrackID);
                writer.WriteBytes(0, 4);
                writer.WriteUInt32((uint)box.Duration);
            }

            writer.WriteBytes(0, 8);

            writer.WriteInt16(box.Layer);
            writer.WriteInt16(box.AlternateGroup);
            writer.WriteInt16(box.Volume.value);
            writer.WriteBytes(0, 2);

            writer.WriteInt32(box.Matrix.value.c0.x);
            writer.WriteInt32(box.Matrix.value.c0.y);
            writer.WriteInt32(box.Matrix.value.c0.z);
            writer.WriteInt32(box.Matrix.value.c1.x);
            writer.WriteInt32(box.Matrix.value.c1.y);
            writer.WriteInt32(box.Matrix.value.c1.z);
            writer.WriteInt32(box.Matrix.value.c2.x);
            writer.WriteInt32(box.Matrix.value.c2.y);
            writer.WriteInt32(box.Matrix.value.c2.z);

            writer.WriteUInt32(box.Width.value);
            writer.WriteUInt32(box.Height.value);
        }

        public static void WriteMDHD(this ByteWriter writer, in MDHDBox box)
        {
            writer.WriteUInt32((uint)MDHDBox.GetSize(box.Version));
            writer.WriteUInt32((uint)ISOBoxType.MDHD);
            writer.WriteUInt8(box.Version);
            writer.WriteBytes(0, 3);

            if (box.Version == 1)
            {
                writer.WriteUInt64(box.CreationTime.value);
                writer.WriteUInt64(box.ModificationTime.value);
                writer.WriteUInt32(box.Timescale);
                writer.WriteUInt64(box.Duration);
            }
            else
            {
                writer.WriteUInt32((uint)box.CreationTime.value);
                writer.WriteUInt32((uint)box.ModificationTime.value);
                writer.WriteUInt32(box.Timescale);
                writer.WriteUInt32((uint)box.Duration);
            }

            writer.WriteUInt16(box.Language.value);
            writer.WriteBytes(0, 2);
        }

        public static void WriteHDLR(this ByteWriter writer, in HDLRBox box)
        {
            writer.WriteUInt32((uint)box.GetSize());
            writer.WriteUInt32((uint)ISOBoxType.HDLR);
            writer.WriteBytes(0, 8);
            writer.WriteUInt32((uint)box.Handler);
            writer.WriteBytes(0, 12);

            unsafe
            {
                // The length value does not include the null-terminator byte. So we do Length + 1 to get the null-terminator.
                writer.WriteBytes(box.Name.GetUnsafePtr(), box.Name.Length + 1);
            }    
        }
    }
}
