using System;

namespace SmartGlass.Nano.FFmpeg.Decoder
{
    public class YUVFrame
    {
        public byte[][] FrameData;
        public int[] LineSizes;
        public YUVFrame(byte[][] frameData, int[] linesizes)
        {
            FrameData = frameData;
            LineSizes = linesizes;
        }
    }
}