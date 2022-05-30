using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Video;
using Unity.MediaFramework.Video;
using Unity.MediaFramework.Format.MP4;
using BitStream = Unity.MediaFramework.Video.BitStream;
using Unity.Jobs.LowLevel.Unsafe;

public unsafe class TestParseMP4Boxes : MonoBehaviour
{
    public VideoClip clip;

    void Start()
    {
        MP4Parser.Parse(clip.originalPath);
    }
}
