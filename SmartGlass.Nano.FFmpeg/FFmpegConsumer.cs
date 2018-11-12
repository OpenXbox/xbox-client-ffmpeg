using System;
using SmartGlass.Nano.Consumer;
using SmartGlass.Nano.Packets;

using SDL2;

namespace SmartGlass.Nano.FFmpeg
{
    public class FFmpegConsumer : IConsumer
    {
        private AudioFormat _audioFormat;
        private VideoFormat _videoFormat;

        private VideoAssembler _videoAssembler;
        private FFmpegAudio _audioHandler;
        private FFmpegVideo _videoHandler;
        private SdlRenderer _renderer;

        public void MainLoop() => _renderer.MainLoop();

        public FFmpegConsumer()
        {
            _audioFormat = new AudioFormat(
                channels: 2,
                sampleRate: 48000,
                codec: AudioCodec.AAC);

            _videoFormat = new VideoFormat(
                fps: 30,
                width: 1280,
                height: 720,
                codec: VideoCodec.H264
            );

            _videoAssembler = new VideoAssembler();
            _audioHandler = new FFmpegAudio();
            _videoHandler = new FFmpegVideo();

            _audioHandler.Initialize(_audioFormat);
            _videoHandler.Initialize(_videoFormat);
            _audioHandler.CreateDecoderContext();
            _videoHandler.CreateDecoderContext();

            _renderer = new SdlRenderer();

            _audioHandler.ProcessDecodedFrame += OnDecodedAudioFrame;
            _videoHandler.ProcessDecodedFrame += OnDecodedVideoFrame;
        }

        public void Start()
        {
            _renderer.Video.Initialize(
                width: (int)_videoFormat.Width,
                height: (int)_videoFormat.Height,
                fullscreen: false);

            _renderer.Audio.Initialize(
                samplerate: (int)_audioFormat.SampleRate,
                channels: (int)_audioFormat.Channels,
                samples: 1024);

            // Start decoding threads
            _audioHandler.DecodingThread().Start();
            _videoHandler.DecodingThread().Start();
        }

        public void OnDecodedVideoFrame(object sender, VideoFrameDecodedArgs args)
        {
            // Enqueue decoded video frame in renderer
            _renderer.Video.Update(args.FrameData, args.LineSizes);
        }

        public void OnDecodedAudioFrame(object sender, AudioFrameDecodedArgs args)
        {
            // Enqueue decoded audio sample in renderer
            _renderer.Audio.Update(args.FrameData);
        }

        public void ConsumeAudioData(AudioData data)
        {
            AACFrame frame = AudioAssembler.AssembleAudioFrame(
                data: data,
                profile: AACProfile.LC,
                samplingFreq: (int)_audioFormat.SampleRate,
                channels: (byte)_audioFormat.Channels);

            if (frame == null)
                return;

            // Enqueue encoded audio data in decoder
            _audioHandler.PushData(frame);
        }

        public void ConsumeAudioFormat(AudioFormat format)
        {
            _renderer.Audio.SetFormat(format);
        }

        public void ConsumeVideoData(VideoData data)
        {
            H264Frame frame = _videoAssembler.AssembleVideoFrame(data);

            if (frame == null)
                return;

            // Enqueue encoded video data in decoder
            _videoHandler.PushData(frame);
        }

        public void ConsumeVideoFormat(VideoFormat format)
        {
            _renderer.Video.SetFormat(format, fullscreen: false);
        }
    }
}
