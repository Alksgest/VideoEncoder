using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

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
        private const string EncodeArgsFormat = "-y -i {0} -b {2} -r {3} -vf scale={4}x{5},setsar=1:1,setdar={6} -ac {7} -ar {8} {1}";
        private const string EncoderName = "ffmpeg.exe";
        private readonly string EncoderPath = Directory.GetCurrentDirectory().ToString() + "\\" + EncoderName;
        private Process CurrentProcess = null;
        private Process GetInfoProcess = null;

        public FFMpegWorker() { }

      
        public Task<bool> EncodeAsync(MediaRecords mediaRecords)
        {
            return Task.Run(() =>
            {
                ProcessStartInfo info = SetupInfo(GetArgs(mediaRecords));

                using (CurrentProcess = Process.Start(info))
                {
                    CurrentProcess.WaitForExit();

                    int code = CurrentProcess.ExitCode;

                    if (code == 0)
                    {
                        //await LogOutputAsync(process.StandardOutput);
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


