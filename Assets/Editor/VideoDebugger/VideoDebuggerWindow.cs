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

namespace VideoDebugger.Editor
{
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

            if (TryLoadGUID(Selection.assetGUIDs[0]))
            {
                Summary.Refresh(SelectedVideo);
            }
        }

        private bool TryLoadGUID(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetExtension(path).ToLower() != ".mp4")
                return false;

            if (SelectedVideo != null)
                SelectedVideo.Dispose();

            SelectedVideo = new MP4Container(guid, Path.Combine(ProjectFolder, path));
            return true;

        }

        public interface IMediaContainer : IDisposable
        {
            public string GUID { get; }

            public string Path { get; }

            public ReadOnlyCollection<MediaTrack> Tracks { get; }
        }

        public class MediaTrack
        {
            public string HelpBoxMessage;

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

                var mTrack = new MediaTrack();

                var width = (uint)tkhd.Width.ConvertDouble();
                var height = (uint)tkhd.Height.ConvertDouble();
                mTrack.Title = $"Video #{tkhd.TrackID}: {width}x{height}";

                var codecName = Stringify.GetCodecName((uint)stsd.Codec);
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
                                    mTrack.HelpBoxMessage = string.Format(TextContent.SPSParseError, error);
                            }
                            else
                            {
                                mTrack.HelpBoxMessage = TextContent.AVCCBoxDontIncludeSPS;
                            }
                            break;
                        default:
                            mTrack.HelpBoxMessage = string.Format(TextContent.CodecNameNotSupported, codecName);
                            break;
                    }
                }
                else
                {
                    mTrack.HelpBoxMessage = TextContent.DecoderConfigurationBoxNull;
                }

                if (mTrack.HelpBoxMessage == null)
                {
                    var framecount = stts.SamplesTable[0].Count;
                    var deltaTime = stts.SamplesTable[0].Delta * framecount;

                    for (var i = 1; i < stts.EntryCount; i++)
                    {
                        ref var sample = ref UnsafeUtility.AsRef<SampleGroup>(stts.SamplesTable + i);

                        framecount += sample.Count;
                        deltaTime += sample.Delta * sample.Count;
                    }

                    var framerate = (double)mdhd.Timescale / deltaTime * framecount;
                    var time = TimeSpan.FromSeconds((double)mdhd.Duration / mdhd.Timescale);

                    var sBuild = new FixedString4096Bytes();
                    sBuild.Append($"Video: {codecName} {sps.Profile}");
                    sBuild.Append($", {sps.PictureWidth}x{sps.PictureHeigth} ({sps.CropLeft},{sps.CropTop},{sps.CropRight},{sps.CropBottom})");
                    sBuild.Append($", par {sps.AspectRatio.Width}:{sps.AspectRatio.Height}, {framerate:0.00} fps");

                    // If there is more than one sample, it means the video has a variable rate
                    if (stts.EntryCount > 1)
                        sBuild.Append($" (Variable)");

                    sBuild.Append($", {framecount} frames, {time:hh\\:mm\\:ss\\.fff}");
                    sBuild.Append($", {sps.Chroma.Format}({sps.VideoFormat}, ");
                    if (sps.ColourPrimaries == sps.TransferCharacteristics && sps.TransferCharacteristics == sps.MatrixCoefficients)
                        sBuild.Append($" {sps.ColourPrimaries})");
                    else
                        sBuild.Append($" {sps.ColourPrimaries}/{sps.TransferCharacteristics}/{sps.MatrixCoefficients})");

                    sBuild.Append($" {sps.Chroma.BitDepthLuma} bits");

                    mTrack.Description = sBuild.ToString();
                }

                TrackList.Add(mTrack);
            }

            private unsafe void FillTrackAudioContent(ISOTrackTable ttable)
            {
                var mdhd = ttable.MDHD.Parse();
                var tkhd = ttable.TKHD.Parse();
                var stsd = ttable.STSD.ParseAudio();

                var esds = new AudioSpecificConfig();
                if (stsd.ESDS != null)
                {
                    esds = AudioSpecificConfig.Parse(stsd.ESDS);
                }

                var mTrack = new MediaTrack();
                var sBuild = new FixedString4096Bytes();
                sBuild.Append($"Audio #{tkhd.TrackID}: {esds.SampleRate} Hz, ");
                sBuild.Append($"{esds.ChannelConfiguration} Channels");
                mTrack.Title = sBuild.ToString();

                sBuild.Clear();

                var codecName = Stringify.GetCodecName((uint)stsd.Codec);
                var time = TimeSpan.FromSeconds((double)mdhd.Duration / mdhd.Timescale);
                sBuild.Append($"Audio: {esds.ObjectType} {time:hh\\:mm\\:ss\\.fff}");

                mTrack.Description = sBuild.ToString();
                TrackList.Add(mTrack);
            }
        }

        public class SummaryPanel
        {
            public readonly VisualTreeAsset UXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResourcePaths.SummaryWindowUXML);

            public TemplateContainer Panel;

            public List<TrackInfoProvider> TrackContentList;

            public SummaryPanel(VisualElement root)
            {
                TrackContentList = new List<TrackInfoProvider>();
                Panel = UXML.Instantiate();
                root.Add(Panel);
            }

            public void Refresh(IMediaContainer video)
            {
                if (TrackContentList.Count < video.Tracks.Count)
                    for (int i = TrackContentList.Count; i < video.Tracks.Count; i++)
                        TrackContentList.Add(new TrackInfoProvider(Panel));
                else
                    for (int i = video.Tracks.Count; i < TrackContentList.Count; i++)
                        TrackContentList[i].Root.style.display = DisplayStyle.None;

                for (int i = 0; i < video.Tracks.Count; i++)
                {
                    TrackContentList[i].Root.style.display = DisplayStyle.Flex;
                    TrackContentList[i].Show(video.Tracks[i]);
                }
                //assetSize.text = fileInfo.Length > Gigabytes ?
                //    $"{(decimal)fileInfo.Length / Gigabytes:0.00} GB" :
                //    $"{(decimal)fileInfo.Length / Megabytes:0.00} MB";

            }

            public const long Gigabytes = 1 << 30;
            public const long Megabytes = 1 << 20;
        }

        public class TrackInfoProvider
        {
            public readonly VisualTreeAsset UXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResourcePaths.TrackContentUXML);

            public TemplateContainer Root;

            public Foldout Foldout;

            public VisualElement LabelContainer;
            public Label LabelMessage;

            public HelpBox HelpBox;

            public TrackInfoProvider(VisualElement root)
            {
                Root = UXML.Instantiate();
                LabelContainer = Root.Q<VisualElement>(classes: "track-content__container");
                LabelMessage = Root.Q<Label>(classes: "track-content__label");
                Foldout = Root.Q<Foldout>();
                root.Add(Root);
            }

            public void Show(MediaTrack track)
            {
                if (track.HelpBoxMessage != null)
                {
                    LabelContainer.style.display = DisplayStyle.None;

                    Foldout.text = track.Title;
                    if (HelpBox == null)
                    {
                        HelpBox = new HelpBox(track.HelpBoxMessage, HelpBoxMessageType.Error);
                        Foldout.Add(HelpBox);
                    }
                    else
                    {
                        HelpBox.text = track.HelpBoxMessage;
                        HelpBox.style.display = DisplayStyle.Flex;
                    }
                }
                else
                {
                    if (HelpBox != null)
                        HelpBox.style.display = DisplayStyle.None;

                    Foldout.text = track.Title;
                    LabelMessage.text = track.Description;
                    LabelContainer.style.display = DisplayStyle.Flex;
                }
            }
        }
    }

    public class VideoClipListItem
    {
        public string GUID;
        public string Path;
        public VideoClip Clip;
    }
}