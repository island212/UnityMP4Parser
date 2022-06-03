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
            StringBuilder sb = new StringBuilder($"[ISOReader] Size={reader.FileSize} ");
            for (int i = 0; i < reader.BoxeTypes.Length; i++)
            {
                sb.Append($"[{reader.BoxeTypes[i]}, {reader.BoxOffsets[i]}], ");
            }
            sb.Remove(sb.Length - 2, 2);
            Debug.Log(sb.ToString());
        }

        public static void Print(in MVHDBox box)
        {
            var sb = new StringBuilder("[MVHDBox] ");
            sb.Append($"Type: {box.fullBox.type} ");
            sb.Append($"Size: {box.fullBox.size} ");
            sb.Append($"Version: {box.fullBox.version} ");
            sb.Append($"Flags: {box.fullBox.flags} ");
            sb.Append($"Creation Time: {box.creationTime} ");
            sb.Append($"Modification Time: {box.modificationTime} ");
            sb.Append($"Timescale: {box.timescale} ");
            sb.Append($"Duration: {box.duration} ");
            sb.Append($"Rate: {box.rate.value} ({box.rate.ConvertDouble()}) ");
            sb.Append($"Volume: {box.volume.value} ({box.volume.ConvertDouble()}) ");
            sb.Append($"Next Track ID: {box.nextTrackID} ");
            sb.Append($"Matrix: ");
            var matrix = box.matrix.value;
            sb.Append($"{matrix.c0.x}, {matrix.c0.y}, {matrix.c0.z}, {matrix.c1.x}, {matrix.c1.y}, {matrix.c1.z}, {matrix.c2.x}, {matrix.c2.y}, {matrix.c2.z}\n\n");

            sb.Append($"View\n");
            sb.Append($"---------------------------\n");
            sb.Append($"Type: {box.fullBox.type}\n");
            sb.Append($"Size: {box.fullBox.size}\n");
            sb.Append($"Version: {box.fullBox.version}\n");
            sb.Append($"Flags: {box.fullBox.flags}\n");
            sb.Append($"Creation Time: {box.creationTime}\n");
            sb.Append($"Modification Time: {box.modificationTime}\n");
            sb.Append($"Timescale: {box.timescale}\n");
            sb.Append($"Duration: {box.duration}\n");
            sb.Append($"Rate: {box.rate.value} ({box.rate.ConvertDouble()})\n");
            sb.Append($"Volume: {box.volume.value} ({box.volume.ConvertDouble()})\n");
            sb.Append($"Next Track ID: {box.nextTrackID}\n");
            sb.Append($"Matrix:\n");
            sb.Append($"| {FixedPoint1616.ConvertDouble(matrix.c0.x)}, {FixedPoint1616.ConvertDouble(matrix.c0.y)}, {FixedPoint1616.ConvertDouble(matrix.c0.z)} |\n");
            sb.Append($"| {FixedPoint1616.ConvertDouble(matrix.c1.x)}, {FixedPoint1616.ConvertDouble(matrix.c1.y)}, {FixedPoint1616.ConvertDouble(matrix.c1.z)} |\n");
            sb.Append($"| {FixedPoint1616.ConvertDouble(matrix.c2.x)}, {FixedPoint1616.ConvertDouble(matrix.c2.y)}, {FixedPoint1616.ConvertDouble(matrix.c2.z)} |\n");
            Debug.Log(sb.ToString());
        }
    }
}
