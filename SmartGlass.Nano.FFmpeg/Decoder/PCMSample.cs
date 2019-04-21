using System;

namespace SmartGlass.Nano.FFmpeg.Decoder
{
    public class PCMSample
    {
        public byte[] SampleData;
        public PCMSample(byte[] sampleData)
        {
            SampleData = sampleData;
        }
    }
}