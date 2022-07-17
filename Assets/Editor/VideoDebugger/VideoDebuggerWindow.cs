using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using UnityEngine.Video;
using System.Linq;
using System.IO;
using Unity.MediaFramework.Format.MP4;
using System.Text;
using Unity.MediaFramework.LowLevel.Format.ISOBMFF;
using System;
using System.Collections.ObjectModel;
using Unity.MediaFramework.LowLevel.Codecs;
using Unity.MediaFramework.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

internal class ResourcePaths
{
    public const string ResourcePath = "Assets/Editor/VideoDebugger/Packages Resources/";
    public const string UXMLPath = ResourcePath + "UXML/";
    public const string StylesPath = ResourcePath + "StyleSheets/";

    public const string VideoDebuggerWindowUXML = UXMLPath + "VideoDebuggerWindow.uxml";
    public const string SummaryWindowUXML = UXMLPath + "SummaryWindow.uxml";
    public const string TrackContentUXML = UXMLPath + "TrackContent.uxml";
    public const string TrackSubContentUXML = UXMLPath + "TrackSubContent.uxml";

    public const string VideoDebuggerWindowUSS = StylesPath + "VideoDebuggerWindow.uss";
}

public class VideoDebuggerWindow : EditorWindow
{
    [MenuItem("Window/Analysis/Video Debugger")]
    public static void ShowExample()
    {
        VideoDebuggerWindow wnd = GetWindow<VideoDebuggerWindow>();
        wnd.titleContent = new GUIContent("Video Debugger");
    }

    private string ProjectFolder => Path.GetDirectoryName(Application.dataPath);

    private IMediaContainer SelectedVideo;
    private SummaryPanel Summary;

    // Called when the windows open and after domain reload
    public void CreateGUI()
    {
        var videoDebugger = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResourcePaths.VideoDebuggerWindowUXML);

        var fullWindows = videoDebugger.Instantiate();

        rootVisualElement.Add(fullWindows);

        Summary = new SummaryPanel(fullWindows);

        SelectionChanged();

        Selection.selectionChanged += SelectionChanged;
    }

    // Called when the windows close and before domain reload
    private void OnDisable()
    {
        if (SelectedVideo != null)
        {
            SelectedVideo.Dispose();
            SelectedVideo = null;
        }

        Selection.selectionChanged -= SelectionChanged;
    }

    internal void SelectionChanged()
    {
        if (Selection.assetGUIDs.Length == 0)
            return;

        LoadGUID(Selection.assetGUIDs[0]);
        Summary.Refresh(SelectedVideo);
    }

    private void LoadGUID(string guid)
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        if (Path.GetExtension(path).ToLower() != ".mp4")
            return;

        if (SelectedVideo != null)
            SelectedVideo.Dispose();

        SelectedVideo = new MP4Container(guid, Path.Combine(ProjectFolder, path));
    }

    public interface IMediaContainer : IDisposable
    {
        public string GUID { get; }

        public string Path { get; }

        public ReadOnlyCollection<MediaTrack> Tracks { get; }
    }

    public class MediaTrack
    {
        public string Title;
        public string Description;
    }

    public class MP4Container : IMediaContainer
    {
        public string GUID { get; private set; }

        public string Path { get; private set; }

        public ReadOnlyCollection<MediaTrack> Tracks => TrackList.AsReadOnly();

        private MP4Header MP4File;
        private List<MediaTrack> TrackList;

        public unsafe MP4Container(string guid, string path)
        {
            GUID = guid;
            Path = path;

            MP4Parser.Parse(path, out MP4File).Complete();

            var tracks = MP4File.Table.Value.Tracks;

            TrackList = new List<MediaTrack>(tracks.Length);
            foreach (var track in tracks)
            {
                switch (track.Handler)
                {
                    case ISOHandler.VIDE:
                        FillTrackVideoContent(track);
                        break;
                    case ISOHandler.SOUN:
                        FillTrackAudioContent(track);
                        break;
                    default:
                        var mTrack = new MediaTrack();
                        mTrack.Title = "Unknown";
                        TrackList.Add(mTrack);
                        break;
                }
            }
        }

        public void Dispose()
        {
            MP4File.Dispose();
        }

        private unsafe void FillTrackVideoContent(ISOTrackTable ttable)
        {
            var mdhd = ttable.MDHD.Parse();
            var tkhd = ttable.TKHD.Parse();
            var stts = ttable.STTS.Parse();
            var stsd = ttable.STSD.ParseVideo();

            var spsFound = false;
            using var sps = new SequenceParameterSet();
            if (stsd.DecoderConfigurationBox != null)
            {
                switch (stsd.Codec)
                {
                    case Unity.MediaFramework.VideoCodec.AVC1:
                        var decoder = AVCDecoderConfigurationRecord.Parse(stsd.DecoderConfigurationBox);
                        if (decoder.SPS != null)
                        {
                            var numSPS = decoder.SPS[0] & 0b00011111;
                            var spsLength = BigEndian.ReadUInt16(decoder.SPS + 1);

                            var error = sps.Parse(decoder.SPS + 3, spsLength, Allocator.Temp);
                            if (error != SPSError.None)
                            {
                                Debug.LogError("SPSError: " + error);
                            }
                            spsFound = error == SPSError.None;
                        }
                        break;
                }
            }

            uint width, height;
            if (spsFound)
            {
                width = sps.PictureWidth - sps.CropLeft - sps.CropRight;
                height = sps.PictureHeigth - sps.CropTop - sps.CropBottom;
            }
            else
            {
                width = (uint)tkhd.Width.ConvertDouble();
                height = (uint)tkhd.Height.ConvertDouble();
            }

            var framecount = stts.SamplesTable[0].Count;
            var deltaTime = stts.SamplesTable[0].Delta * framecount;

            bool fixedFrame = stts.EntryCount == 1;
            for (var i = 1; i < stts.EntryCount; i++)
            {
                ref var sample = ref UnsafeUtility.AsRef<SampleGroup>(stts.SamplesTable + i);

                framecount += sample.Count;
                deltaTime += sample.Delta * sample.Count;
            }
             
            var framerate = (double)mdhd.Timescale / deltaTime * framecount;
            var time = TimeSpan.FromSeconds((double)mdhd.Duration / mdhd.Timescale);

            var mTrack = new MediaTrack();
            var sBuild = new FixedString4096Bytes();
            sBuild.Append($"Video #{tkhd.TrackID}: {width}x{height}");
            mTrack.Title = sBuild.ToString();
            sBuild.Clear();

            sBuild.Append($"Video: {framerate:0.00} fps");
            if (!fixedFrame)
                sBuild.Append($" (Variable)");

            sBuild.Append($", {framecount} frames, {time:hh\\:mm\\:ss\\.fff}");
            if (spsFound)
            {
                var codecName = BigEndian.ConvertToString((uint)stsd.Codec);
                sBuild.Append($", {codecName} {sps.Profile}");
                sBuild.Append($", {sps.Chroma.Format}({sps.VideoFormat}, ");
                if (sps.ColourPrimaries == sps.TransferCharacteristics && sps.TransferCharacteristics == sps.MatrixCoefficients)
                    sBuild.Append($" {sps.ColourPrimaries})");
                else
                    sBuild.Append($" {sps.ColourPrimaries}/{sps.TransferCharacteristics}/{sps.MatrixCoefficients})");

                sBuild.Append($" {sps.Chroma.BitDepthLuma} bits");
                sBuild.Append($", {sps.PictureWidth}x{sps.PictureHeigth} ({sps.CropLeft},{sps.CropTop},{sps.CropRight},{sps.CropBottom})");
                sBuild.Append($", par {sps.AspectRatio.Width}:{sps.AspectRatio.Height}");
            }

            mTrack.Description = sBuild.ToString();

            TrackList.Add(mTrack);
        }

        private unsafe void FillTrackAudioContent(ISOTrackTable ttable)
        {
            var mdhd = ttable.MDHD.Parse();
            var tkhd = ttable.TKHD.Parse();
            var stsd = ttable.STSD.ParseAudio();

            var mTrack = new MediaTrack();
            var sBuild = new FixedString4096Bytes();
            sBuild.Append($"Audio #{tkhd.TrackID}: {stsd.SampleRate} Hz");

            switch (stsd.ChannelCount)
            {
                case 1: 
                    sBuild.Append($", Mono"); 
                    break;
                case 2:
                    sBuild.Append($", Stereo");
                    break;
                case 5:
                    sBuild.Append($", 5.1 Surround");
                    break;
                case 7:
                    sBuild.Append($", 7.1 Surround");
                    break;
                default: 
                    sBuild.Append($", {stsd.ChannelCount} Channels"); 
                    break;
            }

            mTrack.Title = sBuild.ToString();

            sBuild.Clear();

            var time = TimeSpan.FromSeconds((double)mdhd.Duration / mdhd.Timescale);
            sBuild.Append($"Audio: {time:hh\\:mm\\:ss\\.fff}");

            mTrack.Description = sBuild.ToString();
            TrackList.Add(mTrack);
        }
    }

    public class SummaryPanel
    {
        public readonly VisualTreeAsset UXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResourcePaths.SummaryWindowUXML);

        public TemplateContainer Panel;

        public List<TrackContent> TrackContentList;

        public SummaryPanel(VisualElement root)
        {
            TrackContentList = new List<TrackContent>();
            Panel = UXML.Instantiate();
            root.Add(Panel);
        }

        public void Refresh(IMediaContainer video)
        {
            if (TrackContentList.Count < video.Tracks.Count)
            {
                for (int i = TrackContentList.Count; i < video.Tracks.Count; i++)
                {
                    TrackContentList.Add(new TrackContent(Panel));
                }
            }
            else
            {
                for (int i = video.Tracks.Count; i < TrackContentList.Count; i++)
                {
                    TrackContentList[i].RootContent.style.display = DisplayStyle.None;
                }
            }

            var sb = new StringBuilder(1096);
            for (int i = 0; i < video.Tracks.Count; i++)
            {
                TrackContentList[i].RootContent.style.display = DisplayStyle.Flex;
                TrackContentList[i].Foldout.text = video.Tracks[i].Title;
                TrackContentList[i].Content.text = video.Tracks[i].Description;
            }
            //assetSize.text = fileInfo.Length > Gigabytes ?
            //    $"{(decimal)fileInfo.Length / Gigabytes:0.00} GB" :
            //    $"{(decimal)fileInfo.Length / Megabytes:0.00} MB";

        }

        public const long Gigabytes = 1 << 30;
        public const long Megabytes = 1 << 20;
    }

    public class TrackContent
    {
        public readonly VisualTreeAsset UXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResourcePaths.TrackContentUXML);

        public TemplateContainer RootContent;

        public Foldout Foldout;
        public Label Content;

        public TrackContent(VisualElement root)
        {
            RootContent = UXML.Instantiate();
            Content = RootContent.Q<Label>(classes: "track-content__label");
            Foldout = RootContent.Q<Foldout>();
            root.Add(RootContent);
        }
    }
}

public class VideoClipListItem
{
    public string GUID;
    public string Path;
    public VideoClip Clip;
}