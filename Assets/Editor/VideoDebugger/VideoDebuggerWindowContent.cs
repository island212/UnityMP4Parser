using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.MediaFramework.LowLevel.Unsafe;

namespace VideoDebugger.Editor
{
    public class ResourcePaths
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

    public class TextContent
    { 
        public const string SPSParseError = "Error while parsing the SPS. {0}";
        public const string AVCCBoxDontIncludeSPS = "The avcC box didn't include a SPS";
        public const string CodecNameNotSupported = "{0} is currently not supported";
        public const string DecoderConfigurationBoxNull = "Couldn't find the DecoderConfigurationBox inside the STSD box";
    }

    public class Stringify
    {
        public static string GetChannelName(int channelCount) => channelCount switch
        {
            1 => "Mono",
            2 => "Stereo",
            5 => "5.1 Surround",
            7 => "7.1 Surround",
            _ => $"{channelCount} Channels",
        };

        public static string GetCodecName(uint codec)
            => BigEndian.ConvertToString(codec);
    }
}
