using UnityEngine;
using UnityEngine.Video;
using Unity.MediaFramework.Format.MP4;
using Unity.MediaFramework.Video;
using Unity.MediaFramework.Format.ISOBMFF;
using Unity.Jobs;

public unsafe class TestParseMP4Boxes : MonoBehaviour
{
    public VideoClip clip;

    void Start()
    {
        Debug.Log($"Opening file {clip.originalPath}");
        JobHandle job = ISOReader.GetContentTable(clip.originalPath, out ISOReader reader);

        job.Complete();

        DebugTools.Print(reader);

        reader.Dispose();
    }
}
