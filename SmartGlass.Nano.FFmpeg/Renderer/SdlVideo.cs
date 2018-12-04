using System;
using SDL2;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SmartGlass.Nano.Packets;
using System.Diagnostics;

namespace SmartGlass.Nano.FFmpeg
{
    public unsafe class SdlVideo
    {
        public bool Initialized { get; private set; }

        private bool _videoFullscreen;

        private SDL.SDL_Rect _rect;
        private IntPtr _window;
        private IntPtr _renderer;
        private IntPtr _texture;

        private Queue<Tuple<byte[][], int[]>> _videoData;

        public SdlVideo()
        {
            Initialized = false;
            _videoData = new Queue<Tuple<byte[][], int[]>>();
        }

        public int Initialize(int width, int height, bool fullscreen)
        {
            SDL.SDL_WindowFlags window_flags = 0;

            int ret = SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            if (ret < 0)
            {
                Debug.WriteLine("SDL_Init VIDEO failed: {0}", SDL.SDL_GetError());
                return 1;
            }

            if (fullscreen)
            {
                window_flags = SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
            }

            _window = SDL.SDL_CreateWindow("Nano",
                SDL.SDL_WINDOWPOS_UNDEFINED,
                SDL.SDL_WINDOWPOS_UNDEFINED,
                width, height,
                window_flags | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL);

            if (_window == IntPtr.Zero)
            {
                // In the case that the window could not be made...
                Debug.WriteLine("Could not create window: {0}", SDL.SDL_GetError());
                return 1;
            }

            _renderer = SDL.SDL_CreateRenderer(_window,
                index: -1,
                flags: (SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
                        SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC));

            if (_renderer == IntPtr.Zero)
            {
                Debug.WriteLine("Could not create renderer: {0}", SDL.SDL_GetError());
                return 1;
            }

            _texture = SDL.SDL_CreateTexture(_renderer,
                SDL.SDL_PIXELFORMAT_YV12,
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                width,
                height);

            if (_texture == IntPtr.Zero)
            {
                Debug.WriteLine("Could not create texture: {0}", SDL.SDL_GetError());
                return 1;
            }

            _rect = new SDL.SDL_Rect()
            {
                x = 0,
                y = 0,
                w = width,
                h = height
            };

            Initialized = true;
            return 0;
        }

        public void PushDecodedData(byte[][] yuvData, int[] lineSizes)
        {
            _videoData.Enqueue(new Tuple<byte[][], int[]>(yuvData, lineSizes));
        }

        public int Update(byte[][] yuvData, int[] lineSizes)
        {
            if (!Initialized)
            {
                Debug.WriteLine("SDL Video not initialized yet...");
                return -1;
            }

            int ret = -1;
            fixed (byte* y = yuvData[0], u = yuvData[1], v = yuvData[2])
            {
                ret = SDL.SDL_UpdateYUVTexture(
                    _texture,
                    ref _rect,
                    (IntPtr)y,
                    lineSizes[0],
                    (IntPtr)u,
                    lineSizes[1],
                    (IntPtr)v,
                    lineSizes[2]
                );
            }

            if (ret < 0)
            {
                Debug.WriteLine("Could not update texture: {0}", SDL.SDL_GetError());
                return ret;
            }
            SDL.SDL_RenderClear(_renderer);
            SDL.SDL_RenderCopy(_renderer, _texture, ref _rect, ref _rect);
            SDL.SDL_RenderPresent(_renderer);
            return ret;
        }

        public int Close()
        {
            SDL.SDL_DestroyTexture(_texture);
            SDL.SDL_DestroyRenderer(_renderer);
            SDL.SDL_DestroyWindow(_window);

            return 0;
        }
    }
}