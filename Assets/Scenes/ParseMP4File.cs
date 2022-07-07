using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel.Format.ISOBMFF;
using UnityEngine;
using UnityEngine.Video;

public class ParseMP4File : MonoBehaviour
{
    public VideoClip clip;

    unsafe void Start()
    {

        var handle = AsyncISOReader.Read(clip.originalPath, out var header);

        handle.Complete();

        var createTableJob = new CreateISOTable
        {
            Header = header.RawBuffer,
            Table = new NativeReference<ISOTable>(Allocator.Persistent)
        };

        handle = createTableJob.Schedule(handle);

        var getAttributesJob = new GetMediaAttributes
        {
            Header = header.RawBuffer,
            Table = createTableJob.Table,
            VideoTracks = new NativeList<VideoTrack>(4, Allocator.Persistent),
            AudioTracks = new NativeList<AudioTrack>(4, Allocator.Persistent),
        };

        handle = getAttributesJob.Schedule(handle);

        handle.Complete();

        header.Dispose();
    }
}
