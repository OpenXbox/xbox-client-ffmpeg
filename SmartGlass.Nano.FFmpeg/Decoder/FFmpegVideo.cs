using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SmartGlass.Nano;
using SmartGlass.Nano.Consumer;
using SmartGlass.Nano.Packets;
using FFmpeg.AutoGen;

namespace SmartGlass.Nano.FFmpeg.Decoder
{
    public unsafe class FFmpegVideo : FFmpegBase
    {
        uint bpp;
        uint bytes;
        ulong redMask;
        ulong greenMask;
        ulong blueMask;
        int fps;
        int videoWidth;
        int videoHeight;
        AVPixelFormat avSourcePixelFormat;
        AVPixelFormat avTargetPixelFormat;
        AVRational avTimebase;

        Queue<H264Frame> encodedDataQueue;

        public event Action<YUVFrame> FrameDecoded;

        public void PushData(H264Frame data) => encodedDataQueue.Enqueue(data);

        public FFmpegVideo() : base()
        {
            encodedDataQueue = new Queue<H264Frame>();
        }

        public void Initialize(VideoFormat format)
        {
            Initialize(format.Codec, (int)format.Width, (int)format.Height, (int)format.FPS,
                       format.Bpp, format.Bytes, format.RedMask, format.GreenMask, format.BlueMask);
        }

        /// <summary>
        /// Initialize the specified codecID, videoWidth, videoHeight, fps, bpp, bytes, redMask, greenMask and blueMask.
        /// </summary>
        /// <returns>The initialize.</returns>
        /// <param name="codecID">Codec identifier.</param>
        /// <param name="videoWidth">Video width.</param>
        /// <param name="videoHeight">Video height.</param>
        /// <param name="fps">Fps.</param>
        /// <param name="bpp">Bpp.</param>
        /// <param name="bytes">Bytes.</param>
        /// <param name="redMask">Red mask.</param>
        /// <param name="greenMask">Green mask.</param>
        /// <param name="blueMask">Blue mask.</param>
        public void Initialize(VideoCodec codecID, int videoWidth, int videoHeight, int fps,
                               uint bpp = 0, uint bytes = 0, ulong redMask = 0x0, ulong greenMask = 0x0, ulong blueMask = 0x0)
        {
            // valid only for VideoCodec.RGB
            this.bpp = bpp;
            this.bytes = bytes;
            this.redMask = redMask;
            this.greenMask = greenMask;
            this.blueMask = blueMask;

            this.fps = fps;
            this.videoWidth = videoWidth;
            this.videoHeight = videoHeight;
            avTimebase = new AVRational { num = 1, den = fps };

            switch (codecID)
            {
                case VideoCodec.H264:
                    avCodecID = AVCodecID.AV_CODEC_ID_H264;
                    avSourcePixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
                    avTargetPixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
                    break;
                case VideoCodec.YUV:
                    avCodecID = AVCodecID.AV_CODEC_ID_YUV4;
                    avSourcePixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
                    avTargetPixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
                    break;
                case VideoCodec.RGB:
                    throw new NotImplementedException("FFmpegVideo: VideoCodec.RGB");
                default:
                    throw new NotSupportedException($"Invalid VideoCodec: {codecID}");
            }

            if (avSourcePixelFormat != avTargetPixelFormat)
                doResample = true;
            Initialized = true;

            Debug.WriteLine($"Codec ID: {avCodecID}");
            Debug.WriteLine($"Source Pixel Format: {avSourcePixelFormat}");
            Debug.WriteLine($"Target Pixel Format: {avTargetPixelFormat}");
            Debug.WriteLine($"Resolution: {videoWidth}x{videoHeight}, FPS: {fps}");
        }

        /// <summary>
        /// Overwrites the target Video pixelformat.
        /// </summary>
        /// <param name="targetFormat">Target format.</param>
        void OverwriteTargetPixelformat(AVPixelFormat targetFormat)
        {
            if (ContextCreated)
            {
                throw new InvalidOperationException("Cannot overwrite target format if context already initialized");
            }
            avTargetPixelFormat = targetFormat;

            if (avTargetPixelFormat != avSourcePixelFormat)
                doResample = true;
        }

        /// <summary>
        /// Update Codec context with extradata, needed for decoding
        /// </summary>
        /// <param name="codecData">Codec specific data (SPS/PPS) in AVCC format</param>
        internal override void UpdateCodecParameters(byte[] codecData)
        {
            pCodecContext->extradata = (byte*)ffmpeg.av_mallocz((ulong)(codecData.Length + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE));
            Marshal.Copy(codecData, 0, (IntPtr)pCodecContext->extradata, codecData.Length);
            pCodecContext->extradata_size = codecData.Length;
        }

        /// <summary>
        /// Sets the codec context parameters.
        /// </summary>
        internal override void SetCodecContextParams(AVCodecContext* codecContext)
        {
            codecContext->width = videoWidth;
            codecContext->height = videoHeight;
            codecContext->time_base = avTimebase;
            codecContext->pix_fmt = avSourcePixelFormat;
        }

        /// <summary>
        /// Sets the resampler parameters.
        /// </summary>
        internal override SwrContext* CreateResampler(AVCodecContext* codecContext)
        {
            SwrContext* resampler = ffmpeg.swr_alloc();

            ffmpeg.av_opt_set_pixel_fmt(resampler, "in_pixel_fmt", codecContext->pix_fmt, 0);
            ffmpeg.av_opt_set_video_rate(resampler, "in_video_rate", codecContext->time_base, 0);
            ffmpeg.av_opt_set_image_size(resampler, "in_image_size", codecContext->width, codecContext->height, 0);

            ffmpeg.av_opt_set_pixel_fmt(resampler, "out_pixel_fmt", avTargetPixelFormat, 0);
            ffmpeg.av_opt_set_video_rate(resampler, "out_video_rate", codecContext->time_base, 0);
            ffmpeg.av_opt_set_image_size(resampler, "out_image_size", codecContext->width, codecContext->height, 0);

            ffmpeg.swr_init(resampler);
            return resampler;
        }

        /// <summary>
        /// Gets the decoded video frame from ffmpeg queue (PLANAR FORMAT / H264)
        /// </summary>
        /// <returns>The return value of avcodec_receive_frame: 0 on success, smaller 0 on failure </returns>
        /// <param name="decodedFrame">OUT: decoded Frame</param>
        int DequeueDecodedFrame(out byte[][] frameData, out int[] lineSizes)
        {
            frameData = new byte[3][];
            lineSizes = new int[] { 0, 0, 0 };
            if (!IsDecoder)
            {
                Debug.WriteLine("GetDecodedVideoFrame: Context is not initialized for decoding");
                return -1;
            }
            int ret;
            ret = ffmpeg.avcodec_receive_frame(pCodecContext, pDecodedFrame);
            if (ret < 0)
            {
                ffmpeg.av_frame_unref(pDecodedFrame);
                return ret;
            }

            if (doResample)
            {
                // TODO
                throw new NotSupportedException("Should we resample video?");
            }
            else
            {
                // Copy each plane into managed bytearray
                for (int i = 0; i < 3; i++)
                {
                    var plane = ffmpeg.av_frame_get_plane_buffer(pDecodedFrame, i);
                    if (plane == null)
                    {
                        throw new Exception("Invalid frame data");
                    }

                    frameData[i] = new byte[plane->size];
                    Marshal.Copy((IntPtr)plane->data, frameData[i], 0, plane->size);
                    lineSizes[i] = pDecodedFrame->linesize[(uint)i];
                }
            }

            ffmpeg.av_frame_unref(pDecodedFrame);
            return 0;
        }

        public override Thread DecodingThread()
        {
            // Dequeue decoded frames
            return new Thread(() =>
            {
                while (true)
                {
                    // Dequeue decoded Frames
                    int ret = DequeueDecodedFrame(out byte[][] yuvData,
                                                  out int[] lineSizes);
                    if (ret == 0)
                    {
                        FrameDecoded?.Invoke(new YUVFrame(yuvData, lineSizes));
                    }

                    // Enqueue encoded packet
                    H264Frame frame = null;
                    try
                    {
                        if (encodedDataQueue.Count > 0)
                        {
                            frame = encodedDataQueue.Dequeue();
                            if (frame != null)
                                EnqueuePacketForDecoding(frame.RawData);
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        Debug.WriteLine($"FFmpegVideo Loop: {e}");
                    }
                }
            });
        }
    }
}
