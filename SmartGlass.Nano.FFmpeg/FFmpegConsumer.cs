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

            _audioRenderer = new SdlAudio();
            _videoRenderer = new SdlVideo();

            _audioHandler.ProcessDecodedFrame += OnDecodedAudioFrame;
            _videoHandler.ProcessDecodedFrame += OnDecodedVideoFrame;
        }

        public void Start()
        {
            _videoRenderer.Initialize(
                width: (int)_videoFormat.Width,
                height: (int)_videoFormat.Height,
                fullscreen: false);

            _audioRenderer.Initialize(
                samplerate: (int)_audioFormat.SampleRate,
                channels: (int)_audioFormat.Channels,
                samples: 1024);

            // Start decoding threads
            _audioHandler.DecodingThread().Start();
            _videoHandler.DecodingThread().Start();
        }

        /* Called by Audio/Video handlers' decoding threads */
        public void OnDecodedVideoFrame(object sender, VideoFrameDecodedArgs args)
        {
            // Enqueue decoded video frame in renderer
            _videoRenderer.Update(args.FrameData, args.LineSizes);
        }

        public void OnDecodedAudioFrame(object sender, AudioFrameDecodedArgs args)
        {
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
    }
}
