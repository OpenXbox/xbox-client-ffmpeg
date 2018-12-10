using System;
using SmartGlass.Nano.Consumer;
using SmartGlass.Nano.Packets;

using SDL2;

namespace SmartGlass.Nano.FFmpeg
{
    public class FFmpegConsumer : IConsumer
    {
        AudioFormat _audioFormat;
        VideoFormat _videoFormat;
        VideoAssembler _videoAssembler;
        FFmpegAudio _audioHandler;
        FFmpegVideo _videoHandler;
        SdlAudio _audioRenderer;
        SdlVideo _videoRenderer;
        bool _initalSeenPPS;

        public FFmpegConsumer(AudioFormat audioFormat, VideoFormat videoFormat)
        {
            _audioFormat = audioFormat;
            _videoFormat = videoFormat;

            _videoAssembler = new VideoAssembler();
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

            if (frame == null)
                return;

            // Enqueue encoded audio data in decoder
            _audioHandler.PushData(frame);
        }

        public void ConsumeVideoData(object sender, VideoDataEventArgs args)
        {
            if (!_initalSeenPPS && args.VideoData.Header.Marker)
                _initalSeenPPS = true;

            // TODO: Sorting
            H264Frame frame = _videoAssembler.AssembleVideoFrame(args.VideoData);

            // Enqueue encoded video data in decoder
            if (frame != null && _initalSeenPPS)
                _videoHandler.PushData(frame);
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
