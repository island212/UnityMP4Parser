using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.MediaFramework.Format.ISOBMFF;
using Unity.MediaFramework.Format.MP4;
using Unity.MediaFramework.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.MediaFramework.Video
{
    public static class DebugTools
    {
        public static void Print(in ISOBox box)
        {
            Debug.Log($"Type={BigEndian.ConvertToString((uint)box.type)} Size={box.size}");
        }

        public static void Print(in ISOReader reader)
        {
            StringBuilder sb = new StringBuilder("Boxes: ");

            for (int i = 0; i < reader.BoxeTypes.Length; i++)
            {
                sb.Append($"[{BigEndian.ConvertToString((uint)reader.BoxeTypes[i])}, {reader.BoxOffsets[i]}], ");
            }
            sb.Remove(sb.Length - 2, 2);

            Debug.Log($"FileSize={reader.FileSize}");
            Debug.Log(sb.ToString());
        }
    }
}
