using System;
using System.IO;
using FFMpegWrapper;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Windows.Controls;

namespace VideoEncoder
{
    public class VideoRepresenter
    {
        private FFMpegWorker worker = new FFMpegWorker();

        public string Title { get; }
        public string FullPath { get; }
        public long Length { get; }
        public int Duration { get; }
        public string StringLength { get; }
        public TimeSpan TimeDuratrion { get; }
        public Image Icon { get; }

        public VideoRepresenter(string fullPath)
        {
            FileInfo info = new FileInfo(fullPath);
            Title = info.Name;
            FullPath = info.FullName;
            Length = info.Length;
            Duration = worker.GetVideoLength(FullPath);
            StringLength = (Length / 1024).ToString() + " kb";
            TimeDuratrion = new TimeSpan(0, 0, Duration);

            Icon = new Image
            {
                Source = BitmapToImageSource(ExtractIcon(FullPath))
            };
        }

        private System.Drawing.Bitmap ExtractIcon(string path)
        {
            System.Drawing.Bitmap bmp;
            using (System.Drawing.Icon i = System.Drawing.Icon.ExtractAssociatedIcon(path))
            {
                bmp = i.ToBitmap();
            }          
            return bmp;
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
