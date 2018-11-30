using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoEncoder
{
    static class Defines
    {
        public static readonly List<string> FramerateList = new List<string>() { "5", "10", "12", "15", "24", "25", "30" };
        public static readonly Dictionary<string, string> ChannelsDictionary = new Dictionary<string, string>()
        {
            { "Mono", "1" },
            { "Stereo", "2"}
        };
        public static readonly Dictionary<string, string> SamplerateDictionary = new Dictionary<string, string>()
        {
            { "22050 Hz", "22050"},
            { "24000 Hz", "24000"},
            { "32000 Hz", "32000"},
            { "44100 Hz", "44100"},
            { "48000 Hz", "48000"}

        };
        public static readonly Dictionary<string, uint> BitrateDictionary = new Dictionary<string, uint>()
        {
            { "1 Mbit/s", 1000000},
            { "2.5 Mbit/s", 2500000},
            { "5 Mbit/s", 5000000},
            { "8 Mbit/s", 8000000},
            { "16 Mbit/s", 16000000}
        };
    }
}
