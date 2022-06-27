using System;
using System.Collections.Generic;
using System.Text;
using Unity.MediaFramework.LowLevel.Format.ISOBMFF;
using Unity.MediaFramework.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.MediaFramework.Video
{
    public static class DebugTools
    {
        public static void Print(in ISOBox box)
        {
            Debug.Log($"Type={BigEndian.ConvertToString((uint)box.Type)} Size={box.Size}");
        }

        public static void Print(in MVHDBox box)
        {
            var sb = new StringBuilder("[MVHDBox] ");
            sb.Append($"Creation Time: {box.CreationTime} ");
            sb.Append($"Modification Time: {box.ModificationTime} ");
            sb.Append($"Timescale: {box.Timescale} ");
            sb.Append($"Duration: {box.Duration} ");
            sb.Append($"Rate: {box.Rate.value} ({box.Rate.ConvertDouble()}) ");
            sb.Append($"Volume: {box.Volume.value} ({box.Volume.ConvertDouble()}) ");
            sb.Append($"Next Track ID: {box.NextTrackID} ");
            sb.Append($"Matrix: ");
            var matrix = box.Matrix.value;
            sb.Append($"{matrix.c0.x}, {matrix.c0.y}, {matrix.c0.z}, {matrix.c1.x}, {matrix.c1.y}, {matrix.c1.z}, {matrix.c2.x}, {matrix.c2.y}, {matrix.c2.z}\n\n");

            sb.Append($"View\n");
            sb.Append($"---------------------------\n");
            sb.Append($"Creation Time: {box.CreationTime}\n");
            sb.Append($"Modification Time: {box.ModificationTime}\n");
            sb.Append($"Timescale: {box.Timescale}\n");
            sb.Append($"Duration: {box.Duration}\n");
            sb.Append($"Rate: {box.Rate.value} ({box.Rate.ConvertDouble()})\n");
            sb.Append($"Volume: {box.Volume.value} ({box.Volume.ConvertDouble()})\n");
            sb.Append($"Next Track ID: {box.NextTrackID}\n");
            sb.Append($"Matrix:\n");
            sb.Append($"| {FixedPoint1616.ConvertDouble(matrix.c0.x)}, {FixedPoint1616.ConvertDouble(matrix.c0.y)}, {FixedPoint1616.ConvertDouble(matrix.c0.z)} |\n");
            sb.Append($"| {FixedPoint1616.ConvertDouble(matrix.c1.x)}, {FixedPoint1616.ConvertDouble(matrix.c1.y)}, {FixedPoint1616.ConvertDouble(matrix.c1.z)} |\n");
            sb.Append($"| {FixedPoint1616.ConvertDouble(matrix.c2.x)}, {FixedPoint1616.ConvertDouble(matrix.c2.y)}, {FixedPoint1616.ConvertDouble(matrix.c2.z)} |\n");
            Debug.Log(sb.ToString());
        }
    }
}
