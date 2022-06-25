using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Unity.MediaFramework
{
    public struct BigRational
    {
        public long num, denom;
    }

    public struct Rational
    {
        public int num, denom;
    }

    public enum AudioCodec : uint
    {
        MP4A = 0x6d703461
    }

    public enum VideoCodec : uint
    {
        AVC1 = 0x61766331
    }
}
