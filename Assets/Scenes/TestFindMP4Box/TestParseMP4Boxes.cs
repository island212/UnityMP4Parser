using UnityEngine;
using UnityEngine.Video;
using Unity.MediaFramework.Format.MP4;
using Unity.MediaFramework.Video;
using Unity.MediaFramework.Format.ISOBMFF;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public unsafe class TestParseMP4Boxes : MonoBehaviour
{
    public VideoClip clip;

    // Verify https://gpac.github.io/mp4box.js/test/filereader.html

    void Start()
    {
        Debug.Log($"Opening file {clip.originalPath}");
        ISOReader.GetContentTable(clip.originalPath, out ISOReader reader).Complete();

        DebugTools.Print(reader);

        byte* buffer;
        if (reader.TryFindBox(ISOBoxType.MVHD, out buffer))
        {
            DebugTools.Print(new MVHDBox(buffer));
        }

        reader.Dispose();
    }
}
