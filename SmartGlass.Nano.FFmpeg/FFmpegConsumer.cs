using System;
using SDL2;
using SmartGlass.Common;
using SmartGlass.Nano.Consumer;
using SmartGlass.Nano.Packets;

using SmartGlass.Nano.FFmpeg.Renderer;
using SmartGlass.Nano.FFmpeg.Producer;
using SmartGlass.Nano.FFmpeg.Decoder;

namespace SmartGlass.Nano.FFmpeg
{
    public class FFmpegConsumer : IConsumer
    {
        NanoClient _client;
        AudioFormat _audioFormat;
        VideoFormat _videoFormat;
        VideoAssembler _videoAssembler;
        FFmpegAudio _audioHandler;
        FFmpegVideo _videoHandler;
        SdlAudio _audioRenderer;
        SdlVideo _videoRenderer;
        bool _audioContextInitialized;
        bool _videoContextInitialized;

        DateTime _audioRefTimestamp;
        DateTime _videoRefTimestamp;

        uint _audioFrameId;
        uint _videoFrameId;

        public FFmpegConsumer(AudioFormat audioFormat, VideoFormat videoFormat, NanoClient client)
        {
            _client = client;

            _videoAssembler = new VideoAssembler();

            _audioFormat = audioFormat;
            _videoFormat = videoFormat;

            _videoRefTimestamp = new DateTime().FromEpochMillisecondsUtc(_client.Video.ReferenceTimestamp);
            _audioRefTimestamp = new DateTime().FromEpochMillisecondsUtc(_client.Audio.ReferenceTimestamp);

            _audioFrameId = _client.Audio.FrameId;
            _videoFrameId = _client.Video.FrameId;

            _audioHandler = new FFmpegAudio();
            _videoHandler = new FFmpegVideo();

            _audioHandler.Initialize(_audioFormat);
            _videoHandler.Initialize(_videoFormat);
            _audioHandler.CreateDecoderContext();
            _videoHandler.CreateDecoderContext();

            _audioRenderer = new SdlAudio((int)_audioFormat.SampleRate, (int)_audioFormat.Channels);
            _videoRenderer = new SdlVideo((int)videoFormat.Width, (int)videoFormat.Height);

            _audioHandler.ProcessDecodedFrame += OnDecodedAudioFrame;
            _videoHandler.ProcessDecodedFrame += OnDecodedVideoFrame;
        }

        /// <summary>
        /// Start decoding threads
        /// </summary>
        public void Start()
        {
            _audioHandler.DecodingThread().Start();
            _videoHandler.DecodingThread().Start();
        }

        /// <summary>
        /// Callback for Video decoder.
        /// Gets called when a decoded frame is ready
        /// </summary>
        /// <param name="sender">Sending context</param>
        /// <param name="args">Decoded video frame event arguments</param>
        public void OnDecodedVideoFrame(object sender, VideoFrameDecodedArgs args)
        {
            if (!_videoRenderer.Initialized)
            {
                _videoRenderer.Initialize();
            }
            // Enqueue decoded video frame in renderer
            _videoRenderer.Update(args.FrameData, args.LineSizes);
        }

        /// <summary>
        /// Callback for Audio decoder.
        /// Gets called when a decoded sample is ready
        /// </summary>
        /// <param name="sender">Sending context</param>
        /// <param name="args">Decoded audio frame event arguments</param>
        public void OnDecodedAudioFrame(object sender, AudioFrameDecodedArgs args)
        {
            if (!_audioRenderer.Initialized)
            {
                _audioRenderer.Initialize(1024);
            }
            // Enqueue decoded audio sample in renderer
            _audioRenderer.Update(args.FrameData);
        }


        /* Called by NanoClient on freshly received data */
        public void ConsumeAudioData(object sender, AudioDataEventArgs args)
        {
            // TODO: Sorting
            AACFrame frame = AudioAssembler.AssembleAudioFrame(
                data: args.AudioData,
                profile: AACProfile.LC,
                samplingFreq: (int)_audioFormat.SampleRate,
                channels: (byte)_audioFormat.Channels);

            if (!_audioContextInitialized)
            {
                _audioHandler.UpdateCodecParameters(frame.GetCodecSpecificData());
                _audioContextInitialized = true;
            }

            if (frame == null)
                return;

            // Enqueue encoded audio data in decoder
            _audioHandler.PushData(frame);
        }

        public void ConsumeVideoData(object sender, VideoDataEventArgs args)
        {
            // TODO: Sorting
            H264Frame frame = _videoAssembler.AssembleVideoFrame(args.VideoData);

            // Enqueue encoded video data in decoder
            if (frame != null && _videoContextInitialized)
                _videoHandler.PushData(frame);

            else if (args.VideoData.Header.Marker)
            {
                H264Frame codecParamFrame = new H264Frame(args.VideoData.Data, 0, 0);
                if (!codecParamFrame.ContainsPPS || !codecParamFrame.ContainsSPS)
                {
                    throw new InvalidOperationException("Marked frame does not have desired params");
                }

                _videoHandler.UpdateCodecParameters(codecParamFrame.GetCodecSpecificDataAvcc());
                _videoContextInitialized = true;
            }
        }

        public void ConsumeInputFeedbackFrame(object sender, InputFrameEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            _audioRenderer.Close();
            _videoRenderer.Close();
            _audioHandler.Dispose();
            _videoHandler.Dispose();
        }
    }
}
