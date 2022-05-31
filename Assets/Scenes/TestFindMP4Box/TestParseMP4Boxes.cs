using UnityEngine;
using UnityEngine.Video;
using Unity.MediaFramework.Format.MP4;
using Unity.MediaFramework.Video;

public unsafe class TestParseMP4Boxes : MonoBehaviour
{
    public VideoClip clip;

    void Start()
    {
        var mp4Handle = MP4Parser.Create(clip.originalPath, 1024);

        Debug.Log($"Opening file {clip.originalPath} Size={mp4Handle.FileSize}");

        DebugTools.Print(mp4Handle);



        mp4Handle.Dispose();
    }
}
