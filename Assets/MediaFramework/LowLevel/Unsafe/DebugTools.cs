using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.MediaFramework.Format.ISOBMFF;
using Unity.MediaFramework.Format.MP4;
using UnityEngine;

namespace Unity.MediaFramework.Video
{
    public static class DebugTools
    {
        public static void Print(in FTYPBox box)
        {
            //StringBuilder sb = new StringBuilder();
            //sb.Append($"FTYP MajorBrand={BitTools.ConvertToString(box.majorBrand)} MinorVersion={box.minorVersion} Compatible Brands=[");
            //foreach (var compBrand in MP4Tools.FlagToBrand(box.compatibleBrands))
            //{
            //    sb.Append(BitTools.ConvertToString(compBrand));
            //    sb.Append(", ");
            //}
            //sb.Remove(sb.Length - 2, 2);
            //sb.Append(']');

            //Debug.Log(sb.ToString());
        }

        public static void Print(in ISOBox box)
        {
            Debug.Log($"Type={BitTools.BigEndian.ConvertToString((uint)box.type)} Size={box.size}");
        }

        public static void Print(in MP4Handle handle)
        {
            StringBuilder sb = new StringBuilder("Boxes: ");

            int index = 0;
            foreach (var box in handle.Boxes)
            {
                sb.Append($"{BitTools.BigEndian.ConvertToString((uint)box.type)}");
                if (box.size > 1)
                    sb.Append($"({box.size})");
                else
                    sb.Append($"({handle.ExtendedSizes[index++]})");
                sb.Append(", ");
            }
            sb.Remove(sb.Length - 2, 2);

            Debug.Log(sb.ToString());
        }
    }
}
