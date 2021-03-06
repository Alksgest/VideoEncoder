﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace FFMpegWrapper
{
    public class MediaRecords
    {
        public static readonly Dictionary<string, string> ResolutionDictionary = new Dictionary<string, string>()
        {
            { "1920x1080", "16:9" }, //1080p
            { "1280x720", "16:9" },    //720p
            { "1366x768", "16:9" },

            { "1280x800", "16x10" },
            { "1920x1200", "16:10" },

            { "1600x1200", "4:3" }, //old format
            { "1024x768", "4:3" },

            { "1280x1024", "5:4" },
            { "2160x1440", "3:2" },
            { "2560x1700", "3:2" },

            { "2560x1080", "21:9" },
            { "3440x1440", "21:9" },
            { "3840x1080", "32:9" },
            { "5120x1440", "32:9" }
        };

        public string InPath { get; set; }
        public string OutPath { get; set; }
        public uint Bitrate { get; set; }
        public byte Framerate { get; set; }
        public  uint Width { get; set; }
        public uint Height { get; set; }
        public string DAR { get; set; }
        public byte Chanels { get; set; }
        public uint Samplerate { get; set; }

        public MediaRecords(string inPath, string outPath, uint bitrate, byte framerate, uint width, uint height, byte chanels, uint samplerate)
        {
            InPath = inPath;
            OutPath = outPath;
            Bitrate = bitrate;
            Framerate = framerate;
            Width = width;
            Height = height;
            DAR = GetDAR();
            Chanels = chanels;
            Samplerate = samplerate;

            ValidateRecords();
        }
        public MediaRecords(string inPath, string outPath, uint bitrate, byte framerate, uint width, uint height, string dar, byte chanels, uint samplerate)
        {
            InPath = inPath;
            OutPath = outPath;
            Bitrate = bitrate;
            Framerate = framerate;
            Width = width;
            Height = height;
            DAR = dar;
            Chanels = chanels;
            Samplerate = samplerate;

            ValidateRecords();
        }

        private string GetDAR()
        {
            try
            {
                return ResolutionDictionary[$"{Width}x{Height}"];
            }
            catch
            {
               return "5:4";
            }
        }

        private void ValidateRecords()
        {
            //throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return obj is MediaRecords records &&
                   InPath == records.InPath &&
                   OutPath == records.OutPath &&
                   Bitrate == records.Bitrate &&
                   Framerate == records.Framerate &&
                   Width == records.Width &&
                   Height == records.Height &&
                   DAR == records.DAR &&
                   Chanels == records.Chanels &&
                   Samplerate == records.Samplerate;
        }

        public override int GetHashCode()
        {
            var hashCode = -1822011962;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(InPath);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(OutPath);
            hashCode = hashCode * -1521134295 + Bitrate.GetHashCode();
            hashCode = hashCode * -1521134295 + Framerate.GetHashCode();
            hashCode = hashCode * -1521134295 + Width.GetHashCode();
            hashCode = hashCode * -1521134295 + Height.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DAR);
            hashCode = hashCode * -1521134295 + Chanels.GetHashCode();
            hashCode = hashCode * -1521134295 + Samplerate.GetHashCode();
            return hashCode;
        }
    }
}


