using System;
using System.IO;
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
