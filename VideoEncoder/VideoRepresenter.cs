using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFMpegWrapper;

namespace VideoEncoder
{
    class VideoRepresenter
    {
        private FFMpegWorker worker = new FFMpegWorker();

        public string Title { get; }
        public string FullPath { get; }
        public long Length { get; }
        public int Duration { get; }
        public string StringLength { get; }
        public TimeSpan TimeDuratrion { get; }

        public VideoRepresenter(string fullPath)
        {
            FileInfo info = new FileInfo(fullPath);
            Title = info.Name;
            FullPath = info.FullName;
            Length = info.Length;
            Duration = worker.GetVideoLength(FullPath);
            StringLength = (Length / 1024).ToString() + " kb";
            TimeDuratrion = new TimeSpan(0, 0, Duration);
        }
    }
}
