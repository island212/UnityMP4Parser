using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel.Unsafe;

namespace Unity.MediaFramework.Format.ISOBMFF
{
    public static class ISOByteWriterExtensions
    {
        public static void WriteMVHD(this ByteWriter writer, in MVHDBox box)
        {
            writer.WriteUInt32((uint)MVHDBox.GetSize(box.Version));
            writer.WriteUInt32((uint)ISOBoxType.MVHD);
            writer.WriteUInt8(box.Version);
            writer.WriteBytes(3, 0);

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
            writer.WriteBytes(10, 0);

            writer.WriteInt32(box.Matrix.value.c0.x);
            writer.WriteInt32(box.Matrix.value.c0.y);
            writer.WriteInt32(box.Matrix.value.c0.z);
            writer.WriteInt32(box.Matrix.value.c1.x);
            writer.WriteInt32(box.Matrix.value.c1.y);
            writer.WriteInt32(box.Matrix.value.c1.z);
            writer.WriteInt32(box.Matrix.value.c2.x);
            writer.WriteInt32(box.Matrix.value.c2.y);
            writer.WriteInt32(box.Matrix.value.c2.z);

            writer.WriteBytes(4 * 6, 0);
            writer.WriteUInt32(box.NextTrackID);
        }

        public static void WriteFTYP(this ByteWriter writer, in FTYPBox box)
        {
            writer.WriteUInt32((uint)FTYPBox.GetSize(box.BrandCount));
            writer.WriteUInt32((uint)ISOBoxType.FTYP);
            writer.WriteUInt32((uint)box.MajorBrand);
            writer.WriteUInt32(box.MinorVersion);

            switch (math.min(box.BrandCount, FTYPBox.MaxCachedBrands))
            {
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
            }
        }

        public static void WriteFTYP(this ByteWriter writer, ISOBrand major, uint minor, NativeArray<ISOBrand> brands)
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
    }
}
