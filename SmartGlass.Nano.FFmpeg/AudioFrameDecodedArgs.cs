using System;
using SmartGlass.Nano.Consumer;

namespace SmartGlass.Nano.FFmpeg
{
    public class AudioFrameDecodedArgs : EventArgs
    {
        public byte[] FrameData { get; private set; }

        public AudioFrameDecodedArgs(byte[] frameData) : base()
        {
            FrameData = frameData;
        }
    }
}
