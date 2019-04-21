using System;
using SDL2;
using SmartGlass.Common;
using SmartGlass.Nano.Consumer;
using SmartGlass.Nano.Packets;

using SmartGlass.Nano.FFmpeg.Renderer;
using SmartGlass.Nano.FFmpeg.Producer;
using SmartGlass.Nano.FFmpeg.Decoder;
using System.Collections.Generic;

namespace SmartGlass.Nano.FFmpeg.Decoder
{
    public class FFmpegDecoder : IDisposable
    {
        private bool _disposed = false;

        NanoClient _client;
        AudioFormat _audioFormat;
        VideoFormat _videoFormat;
        VideoAssembler _videoAssembler;
        FFmpegAudio _audioHandler;
        FFmpegVideo _videoHandler;

        bool _audioContextInitialized;
        bool _videoContextInitialized;

        DateTime _audioRefTimestamp;
        DateTime _videoRefTimestamp;

        uint _audioFrameId;
        uint _videoFrameId;

        public Queue<YUVFrame> DecodedVideoQueue { get; private set; }
        public Queue<PCMSample> DecodedAudioQueue  { get; private set; }

        public FFmpegDecoder(NanoClient client, AudioFormat audioFormat, VideoFormat videoFormat)
        {
            _client = client;

            _videoAssembler = new VideoAssembler();

            _audioFormat = audioFormat;
            _videoFormat = videoFormat;

            _videoRefTimestamp = _client.Video.ReferenceTimestamp;
            _audioRefTimestamp = _client.Audio.ReferenceTimestamp;

            _audioFrameId = _client.Audio.FrameId;
            _videoFrameId = _client.Video.FrameId;

            _audioHandler = new FFmpegAudio();
            _videoHandler = new FFmpegVideo();

            _audioHandler.Initialize(_audioFormat);
            _videoHandler.Initialize(_videoFormat);
            _audioHandler.CreateDecoderContext();
            _videoHandler.CreateDecoderContext();

            DecodedAudioQueue = new Queue<PCMSample>();
            DecodedVideoQueue = new Queue<YUVFrame>();

            // Register queues for decoded video frames / audio samples
            _audioHandler.SampleDecoded += DecodedAudioQueue.Enqueue;
            _videoHandler.FrameDecoded += DecodedVideoQueue.Enqueue;
        }

        /// <summary>
        /// Start decoding threads
        /// </summary>
        public void Start()
        {
            _audioHandler.DecodingThread().Start();
            _videoHandler.DecodingThread().Start();
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
            var frame = _videoAssembler.AssembleVideoFrame(args.VideoData);

            if (frame == null)
                return;

            // Enqueue encoded video data in decoder
            if (_videoContextInitialized)
                _videoHandler.PushData(frame);
            else if (frame.PrimaryType == NalUnitType.SEQUENCE_PARAMETER_SET)
            {
                _videoHandler.UpdateCodecParameters(frame.GetCodecSpecificDataAvcc());
                _videoContextInitialized = true;
            }
        }

        public void ConsumeInputFeedbackFrame(object sender, InputFrameEventArgs args)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _audioHandler.Dispose();
                    _videoHandler.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
