using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using SmartGlass.Nano.Packets;
using System.Collections.Generic;
using SmartGlass.Nano.Consumer;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace SmartGlass.Nano.FFmpeg
{
    public unsafe class FFmpegAudio : FFmpegBase
    {
        uint sampleSize;
        uint sampleType;
        int sampleRate;
        int channels;
        long avChannelLayout;
        AVSampleFormat avSourceSampleFormat;
        AVSampleFormat avTargetSampleFormat;

        Queue<AACFrame> encodedDataQueue;

        public event EventHandler<AudioFrameDecodedArgs> ProcessDecodedFrame;

        public void PushData(AACFrame data) => encodedDataQueue.Enqueue(data);

        public FFmpegAudio() : base(audio: true)
        {
            encodedDataQueue = new Queue<AACFrame>();
        }

        public void Initialize(AudioFormat format)
        {
            Initialize(format.Codec, (int)format.SampleRate, (int)format.Channels,
                       format.SampleSize, format.SampleType);
        }

        /// <summary>
        /// Initialize the specified codecID, sampleRate, channels, sampleSize and sampleType.
        /// </summary>
        /// <returns>The initialize.</returns>
        /// <param name="codecID">Codec identifier.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="channels">Channels.</param>
        /// <param name="sampleSize">Sample size.</param>
        /// <param name="sampleType">Sample type.</param>
        public void Initialize(AudioCodec codecID, int sampleRate, int channels,
                               uint sampleSize = 0, uint sampleType = 0)
        {
            /* AudioCodec.PCM specific */
            this.sampleSize = sampleSize;
            this.sampleType = sampleType;
            /* specific end */

            this.sampleRate = sampleRate;
            this.channels = channels;
            avChannelLayout = ffmpeg.av_get_default_channel_layout(channels);

            switch (codecID)
            {
                case AudioCodec.AAC:
                    avCodecID = AVCodecID.AV_CODEC_ID_AAC;
                    avSourceSampleFormat = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                    avTargetSampleFormat = AVSampleFormat.AV_SAMPLE_FMT_FLT;
                    break;
                case AudioCodec.Opus:
                    avCodecID = AVCodecID.AV_CODEC_ID_OPUS;
                    avSourceSampleFormat = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                    avTargetSampleFormat = AVSampleFormat.AV_SAMPLE_FMT_FLT;
                    break;
                case AudioCodec.PCM:
                    throw new NotImplementedException("FFmpegAudio: AudioCodec.PCM");
                default:
                    throw new NotSupportedException($"Invalid AudioCodec: {codecID}");
            }

            if (avTargetSampleFormat != avSourceSampleFormat)
                doResample = true;
            Initialized = true;

            Debug.WriteLine($"Codec ID: {avCodecID}");
            Debug.WriteLine($"Source Sample Format: {avSourceSampleFormat}");
            Debug.WriteLine($"Target Sample Format: {avTargetSampleFormat}");
            Debug.WriteLine($"Channels: {channels}, SampleRate: {sampleRate}");
        }

        /// <summary>
        /// Overwrites the target Audio sampleformat.
        /// </summary>
        /// <param name="targetFormat">Target format.</param>
        void OverwriteTargetSampleformat(AVSampleFormat targetFormat)
        {
            if (ContextCreated)
            {
                throw new InvalidOperationException("Cannot overwrite target format if context already initialized");
            }
            avTargetSampleFormat = targetFormat;

            if (avTargetSampleFormat != avSourceSampleFormat)
                doResample = true;
        }

        internal override void SetCodecContextParams()
        {
            pCodecContext->sample_rate = sampleRate;
            pCodecContext->sample_fmt = avSourceSampleFormat;
            pCodecContext->channels = channels;
            pCodecContext->channel_layout = (ulong)avChannelLayout;
        }

        internal override void SetResamplerParams()
        {
            ffmpeg.av_opt_set_int(pResampler, "in_channel_count", pCodecContext->channels, 0);
            ffmpeg.av_opt_set_channel_layout(pResampler, "in_channel_layout", (long)pCodecContext->channel_layout, 0);
            ffmpeg.av_opt_set_int(pResampler, "in_sample_rate", pCodecContext->sample_rate, 0);
            ffmpeg.av_opt_set_sample_fmt(pResampler, "in_sample_fmt", pCodecContext->sample_fmt, 0);

            ffmpeg.av_opt_set_int(pResampler, "out_channel_count", pCodecContext->channels, 0);
            ffmpeg.av_opt_set_channel_layout(pResampler, "out_channel_layout", (long)pCodecContext->channel_layout, 0);
            ffmpeg.av_opt_set_int(pResampler, "out_sample_rate", pCodecContext->sample_rate, 0);
            ffmpeg.av_opt_set_sample_fmt(pResampler, "out_sample_fmt", avTargetSampleFormat, 0);
        }

        /// <summary>
        /// Gets the decoded and converted audio frame from ffmpeg queue (PACKED FORMAT / Float32)
        /// </summary>
        /// <returns>The decoded audio frame.</returns>
        /// <param name="decodedFrame">OUT: Decoded Audio frame.</param>
        int DequeueDecodedFrame(out byte[] frameData)
        {
            frameData = null;

            if (!IsDecoder)
            {
                Debug.WriteLine("GetDecodedAudioFrame: Context is not initialized for decoding");
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
                byte* convertedData = null;
                ret = ffmpeg.av_samples_alloc(
                                    &convertedData,
                                    null,
                                    pDecodedFrame->channels,
                                    pDecodedFrame->nb_samples,
                                    avTargetSampleFormat,
                                    1);
                if (ret < 0)
                {
                    Debug.WriteLine("Could not allocate audio buffer");
                    ffmpeg.av_frame_unref(pDecodedFrame);
                    return ret;
                }

                ffmpeg.swr_convert(pResampler, &convertedData, pDecodedFrame->nb_samples,
                                   pDecodedFrame->extended_data, pDecodedFrame->nb_samples);

                int bufSize = ffmpeg.av_samples_get_buffer_size(null, pDecodedFrame->channels,
                                                                      pDecodedFrame->nb_samples,
                                                                      avTargetSampleFormat, 1);

                frameData = new byte[bufSize];
                Marshal.Copy((IntPtr)convertedData, frameData, 0, frameData.Length);
            }
            else
            {
                // TODO
                throw new NotImplementedException("Can we even deal with non-converted audio data?");
            }

            ffmpeg.av_frame_unref(pDecodedFrame);
            return 0;
        }

        public override Thread DecodingThread()
        {
            return new Thread(() =>
            {
                while (true)
                {
                    // Dequeue decoded Frames
                    int ret = DequeueDecodedFrame(out byte[] audioSampleData);
                    if (ret == 0)
                    {
                        AudioFrameDecodedArgs args = new AudioFrameDecodedArgs(
                            audioSampleData);

                        ProcessDecodedFrame?.Invoke(this, args);
                    }

                    // Enqueue encoded packet
                    AACFrame frame = null;
                    try
                    {
                        if (encodedDataQueue.Count > 0)
                        {
                            frame = encodedDataQueue.Dequeue();
                            EnqueuePacketForDecoding(frame.RawData);
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        Debug.WriteLine($"FFmpegAudio Loop: {e}");
                    }
                }
            });
        }
    }
}
