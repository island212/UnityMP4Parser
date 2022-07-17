using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.MediaFramework.Format.MP4;
using Unity.MediaFramework.LowLevel.Format.ISOBMFF;
using UnityEngine;
using UnityEngine.Video;

public class ParseMP4File : MonoBehaviour
{
    public VideoClip clip;

    void Start()
    {
        var handle = MP4Parser.Parse(clip.originalPath, out var mp4);

        handle.Complete();

        mp4.Dispose();
    }
}
