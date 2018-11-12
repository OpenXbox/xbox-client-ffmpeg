using System;
using SmartGlass.Nano.Consumer;

namespace SmartGlass.Nano.FFmpeg
{
    public class VideoFrameDecodedArgs : EventArgs
    {
        public byte[][] FrameData { get; private set; }
        public int[] LineSizes { get; private set; }

        public VideoFrameDecodedArgs(byte[][] frameData, int[] lineSizes) : base()
        {
            FrameData = frameData;
            LineSizes = lineSizes;
        }
    }
}
