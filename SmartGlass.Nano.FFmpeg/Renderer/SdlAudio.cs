using System;
using SDL2;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SmartGlass.Nano.Packets;
using System.Diagnostics;
using SmartGlass.Nano.FFmpeg.Decoder;

namespace SmartGlass.Nano.FFmpeg.Renderer
{
    public unsafe class SdlAudio
    {
        public bool Initialized => _dev > 0;

        uint _dev;
        int _sampleRate;
        int _channels;

        public SdlAudio(int sampleRate, int channels)
        {
            _sampleRate = sampleRate;
            _channels = channels;
            _dev = 0;
        }

        public int Initialize(int samples)
        {
            var desired = new SDL.SDL_AudioSpec();
            var obtained = new SDL.SDL_AudioSpec();

            int ret = SDL.SDL_Init(SDL.SDL_INIT_AUDIO);
            if (ret < 0)
            {
                Debug.WriteLine($"SDL_Init AUDIO failed: {SDL.SDL_GetError()}");
                return 1;
            }

            desired.freq = _sampleRate;
            desired.format = SDL.AUDIO_F32;
            desired.channels = (byte)_channels;
            desired.samples = (ushort)samples;

            _dev = SDL.SDL_OpenAudioDevice(
                device: null,
                iscapture: 0,
                desired: ref desired,
                obtained: out obtained,
                allowed_changes: (int)SDL.SDL_AUDIO_ALLOW_FORMAT_CHANGE);

            if (_dev == 0)
            {
                Debug.WriteLine($"Failed to open audio: {SDL.SDL_GetError()}");
                return 1;
            }

            if (obtained.format != desired.format)
            { /* we let this one thing change. */
                Debug.WriteLine($"We didn't get Float32 audio format.");
            }
            SDL.SDL_PauseAudioDevice(_dev, 0); /* start audio playing. */

            return 0;
        }

        public int Update(PCMSample sample)
        {
            fixed (byte* p = sample.SampleData)
            {
                return UpdateAudio((IntPtr)p, (uint)sample.SampleData.Length);
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
