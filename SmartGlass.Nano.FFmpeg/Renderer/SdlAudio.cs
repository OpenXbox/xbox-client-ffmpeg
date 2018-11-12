using System;
using SDL2;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SmartGlass.Nano.Packets;
using System.Diagnostics;

namespace SmartGlass.Nano.FFmpeg
{
    public unsafe class SdlAudio
    {
        public bool Initialized { get; private set; }

        private uint _dev;
        private AudioFormat _audioFormat;
        private int _audioSamplesCount;
        private Queue<byte[]> _audioData;

        public SdlAudio()
        {
            Initialized = false;
            _dev = 0;
            _audioData = new Queue<byte[]>();
        }

        public void SetFormat(AudioFormat format, int samples=4096)
        {
            _audioFormat = format;
            _audioSamplesCount = samples;
        }

        public int Initialize(int samplerate, int channels, int samples)
        {
            SDL.SDL_AudioSpec want = new SDL.SDL_AudioSpec();
            SDL.SDL_AudioSpec have = new SDL.SDL_AudioSpec();

            int ret = SDL.SDL_Init(SDL.SDL_INIT_AUDIO);
            if (ret < 0)
            {
                Debug.WriteLine("SDL_Init AUDIO failed: {0}", SDL.SDL_GetError());
                return 1;
            }

            want.freq = samplerate;
            want.format = SDL.AUDIO_F32;
            want.channels = (byte)channels;
            want.samples = (ushort)samples;

            _dev = SDL.SDL_OpenAudioDevice(
                device: null,
                iscapture: 0,
                desired: ref want,
                obtained: out have,
                allowed_changes: (int)SDL.SDL_AUDIO_ALLOW_FORMAT_CHANGE);

            if (_dev == 0)
            {
                Debug.WriteLine("Failed to open audio: {0}", SDL.SDL_GetError());
                return 1;
            }

            if (have.format != want.format)
            { /* we let this one thing change. */
                Debug.WriteLine("We didn't get Float32 audio format.");
            }
            SDL.SDL_PauseAudioDevice(_dev, 0); /* start audio playing. */

            Initialized = true;
            return 0;
        }

        public void PushDecodedData(byte[] audioData)
        {
            _audioData.Enqueue(audioData);
        }

        public int Update(byte[] audioData)
        {
            fixed (byte* p = audioData)
            {
                return UpdateAudio((IntPtr)p, (uint)audioData.Length);
            }
        }

		private int UpdateAudio(IntPtr data, uint length)
		{
            if (!Initialized)
            {
                Debug.WriteLine("SDL Audio not initialized yet...");
                return -1;
            }

            return SDL.SDL_QueueAudio(_dev, data, length);
		}

        public int Close()
        {
            SDL.SDL_CloseAudioDevice(_dev);
            return 0;
        }
    }
}