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
            Debug.Log($"Type={BitTools.ConvertToString((uint)box.type)} Size={box.size}");
        }
    }
}
