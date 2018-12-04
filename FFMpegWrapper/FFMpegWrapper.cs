using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace FFMpegWrapper
{
    public class FFMpegWorker
    {
        /*
            {0} - path to "in" file
            {1} - path to "out" file
            {2} - bitrate
            {3} - framerate
            {4}x{5} - width x height
            {6} - DAR
            {7} - chanels
            {8} - samplerate
        */
        private const string EncodeArgsFormat = "-y -i \"{0}\" -b {2} -r {3} -vf scale={4}x{5},setsar=1:1,setdar={6} -ac {7} -ar {8} {1}";
        private const string GetFrameFormat = "-y -i \"{0}\" -vframes 1 {1}";
        private readonly string FramesPath = Directory.GetCurrentDirectory().ToString() + "\\Frames\\"; 
        private const string EncoderName = "ffmpeg.exe";
        private readonly string EncoderPath = Directory.GetCurrentDirectory().ToString() + "\\" + EncoderName;
        private Process CurrentProcess = null;
        private Process GetInfoProcess = null;

        public FFMpegWorker()
        {
            if (!Directory.Exists(FramesPath))
                Directory.CreateDirectory(FramesPath);                
        }


        public Task<bool> JoinVideosAsync(List<MediaRecords> files)
        {
            return Task.Run(() =>
            {
                return JoinVideosHelper(files);
            });
        }

        public Task<string> GetFrameAsync(string path)
        {
            return Task.Run(() =>
           {
               return GetFrame(path);
           });
        }

        public string GetFrame(string path)
        {
            string fileName = GetNameOfFile(new FileInfo(path));
            string filePathForFFMpeg = "\"" + FramesPath + fileName + "_first_frame" + ".jpg" + "\"";
            string filePath = FramesPath + fileName + "_first_frame" + ".jpg";
            string args = String.Format(GetFrameFormat, path, filePathForFFMpeg);

            int exitCode = -1;
            using (CurrentProcess = Process.Start(SetupInfo(args)))
            {
                CurrentProcess.WaitForExit();
                exitCode = CurrentProcess.ExitCode;
            }

            if (exitCode == 0)
            {
                return filePath;
            }
            else
                return null;
        }

        private bool JoinVideosHelper(List<MediaRecords> files)
        {

            if (files == null)
                return false;

            string args = "-y ";
            foreach (var file in files)
            {
                args += "-i " + "\"" + file.InPath + "\"" + " ";
            }
            args += "-filter_complex" + " " + $"\"[0:0] [0:1] [1:0] [1:1] concat=n={files.Count}:v=1:a=1 [v] [a]\"" + " " + "-map" + " " + "\"[v]\"" + " " + "-map" + " " + "\"[a]\"";
            args += " " + @"D:\ffmpeg_test\xxx\joined_videos.mp4";

            ProcessStartInfo info = SetupInfo(args);


            using (CurrentProcess = Process.Start(info))
            {
                CurrentProcess.WaitForExit();

                if (CurrentProcess.ExitCode == 0)
                    return true;
            }
            return false;
        }


        private async Task<List<MediaRecords>> GetFilesForJoinAsync(List<MediaRecords> files, MediaRecords currentRecords)
        {
            bool isEqual = true;
            List<MediaRecords> outList = new List<MediaRecords>();
            foreach(var file in files)
            {
                isEqual = file.Equals(currentRecords);
                if (isEqual)
                    outList.Add(file);
                else
                {
                    MediaRecords tmp = currentRecords;
                    tmp.OutPath = file.OutPath;
                    tmp.InPath = file.InPath;
                    bool res = await EncodeAsync(tmp);
                    tmp.InPath = tmp.OutPath;
                    if (res)
                    {
                        outList.Add(tmp);
                    }
                    else
                        return null;
                }
            }
            return outList;
        }



        public Task<bool> EncodeAsync(MediaRecords mediaRecords)
        {
            return Task.Run(async () =>
            {
                ProcessStartInfo info = SetupInfo(GetArgs(mediaRecords));

                using (CurrentProcess = Process.Start(info))
                {
                    CurrentProcess.WaitForExit();

                    if (CurrentProcess.ExitCode == 0)
                    {
                        //await LogOutputAsync(CurrentProcess.StandardOutput);
                        return true;
                    }
                   // await LogOutputAsync(process.StandardError);
                    return false;
                }
            });
        }
        // Encode video async with current media records
        public bool Encode(MediaRecords mediaRecords)
        {
            ProcessStartInfo info = SetupInfo(GetArgs(mediaRecords));

            using (CurrentProcess = Process.Start(info))
            {
                CurrentProcess.EnableRaisingEvents = true;
                CurrentProcess.WaitForExit();

                int code = CurrentProcess.ExitCode;

                if (code == 0)
                {
                    //LogOutput(process.StandardOutput);
                    return true;
                }

                //LogOutput(process.StandardError);
                return false;
            }
        }
        //Encode video async with current media records
        private static string GetArgs(MediaRecords mediaRecord)
        {
            return string.Format(EncodeArgsFormat,
                mediaRecord.InPath,
                mediaRecord.OutPath,
                mediaRecord.Bitrate,
                mediaRecord.Framerate,
                mediaRecord.Width,
                mediaRecord.Height,
                mediaRecord.DAR,
                mediaRecord.Chanels,
                mediaRecord.Samplerate);
        }

        private ProcessStartInfo SetupInfo(string args, bool getInfoFlag = false)
        {
            return new ProcessStartInfo()
            {
                FileName = EncoderPath,
                RedirectStandardError = getInfoFlag, //break working
                //RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = args,
                ErrorDialog = true,
                WorkingDirectory = Directory.GetCurrentDirectory(),                
            };
        }

        private static async Task LogOutputAsync(StreamReader stream)
        {
            using (StreamReader streamReader = stream)
            {
                string Message = await stream.ReadToEndAsync();
                using (StreamWriter writer = new StreamWriter(@"encoder_logs\log.txt", true))
                    await writer.WriteAsync(Message);
            }
        }

        private static void LogOutput(StreamReader stream)
        {
            using (StreamReader streamReader = stream)
            {
                string Message = stream.ReadToEnd();
                using (StreamWriter writer = new StreamWriter(@"encoder_logs\log.txt", true))
                    writer.Write(Message);
            }
        }

        public int GetVideoLength(string fullPath)
        {
            Regex regex = new Regex("Duration: (\\d{2}):(\\d{2}):(\\d{2})");

            ProcessStartInfo info = SetupInfo($"-i {fullPath}", true); //-hide_banner - loglevel info

            string data;

            using (GetInfoProcess = Process.Start(info))
            {
                GetInfoProcess.WaitForExit();
                using (var stream = GetInfoProcess.StandardError)
                {
                    data = stream.ReadToEnd();
                }
                if(regex.Match(data).Value.Length > 0)
                {
                    string str = regex.Match(data).Value;
                    var splitedStr = str.Split(':');

                    int hr = Int32.Parse(splitedStr[1]);
                    int min = Int32.Parse(splitedStr[2]);
                    int sec = Int32.Parse(splitedStr[3]);

                    return hr * 60 * 60 + min * 60 + sec;
                }
            }
            return 0;
        }

        public static String GetNameOfFile(FileInfo info)
        {
            var name = info.Name;
            var splitedName = name.Split('.');
            string result = "";
            for (int i = 0; i < splitedName.Length - 1; ++i)
            {
                result += splitedName[i];
            }
            return result;
        }


        //public bool CloseFFMpeg()
        //{
        //    int counter = 0;
        //    if (CurrentProcess != null)
        //    {
        //        CurrentProcess.Kill();
        //        ++counter;
        //    }
        //    else
        //        ++counter;
        //    if (GetInfoProcess != null)
        //    {
        //        GetInfoProcess.Kill();
        //        ++counter;
        //    }
        //    else
        //        ++counter;


        //    return counter == 2;
        //}

    }
    
}


