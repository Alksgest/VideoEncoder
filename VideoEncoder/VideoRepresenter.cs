using System;
using System.IO;
using FFMpegWrapper;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace VideoEncoder
{
    public class VideoRepresenter
    {
        private FFMpegWorker worker;

        public string Title { get; }
        public string FullPath { get; }
        public string Extension { get; }
        public long Length { get; }
        public int Duration { get; }
        public string StringLength { get; }
        public TimeSpan TimeDuratrion { get; }
        public string FirstFrame { get; private set; }

        public VideoRepresenter(string fullPath)
        {
            worker = new FFMpegWorker();
            FileInfo info = new FileInfo(fullPath);
            Extension = info.Extension;
            Title = info.Name;
            FullPath = info.FullName;
            Length = info.Length;
            Duration = worker.GetVideoLength(FullPath);
            StringLength = (Length / 1024).ToString() + " kb";
            TimeDuratrion = new TimeSpan(0, 0, Duration);

        }
        public Task GetFirstFrameAsync()
        {
            return Task.Run(async () =>
            {
                FirstFrame = await worker.GetFrameAsync(FullPath);
            });
        }

        public void GetFirstFrame()
        {
            FirstFrame = worker.GetFrame(FullPath);
        }

        BitmapImage BitmapToImageSource(System.Drawing.Bitmap bitmap)
        {
            BitmapImage bitmapimage;
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.DecodePixelWidth = 32;
                bitmapimage.DecodePixelWidth = 32;
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
            }
            return bitmapimage;
        }
    }
}
