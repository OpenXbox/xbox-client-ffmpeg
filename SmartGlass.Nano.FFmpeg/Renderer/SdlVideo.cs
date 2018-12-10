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
        public bool Initialized => _texture != IntPtr.Zero;
        bool _fullscreenWindow;
        SDL.SDL_Rect _rectOrigin;
        IntPtr _window;
        IntPtr _renderer;
        IntPtr _texture;

        public SdlVideo(int width = 1280, int height = 720, bool fullscreen = false)
        {
            _fullscreenWindow = fullscreen;
            _rectOrigin = new SDL.SDL_Rect() { x = 0, y = 0, w = width, h = height };
        }

        public void Initialize()
        {
            SDL.SDL_WindowFlags window_flags = 0;
            if ((SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_EVENTS) | SDL_ttf.TTF_Init()) < 0)
            {
                Debug.WriteLine($"Could not init SDL video: {SDL.SDL_GetError()}");
                return;
            }

            if (_fullscreenWindow)
                window_flags = SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;

            _window = SDL.SDL_CreateWindow(
                "Nano",
                SDL.SDL_WINDOWPOS_UNDEFINED,
                SDL.SDL_WINDOWPOS_UNDEFINED,
                _rectOrigin.w, _rectOrigin.h,
                window_flags | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL
            );

            if (_window == IntPtr.Zero)
            {
                // In the case that the window could not be made...
                Debug.WriteLine($"Could not create window: {SDL.SDL_GetError()}");
                return;
            }

            _renderer = SDL.SDL_CreateRenderer(_window,
                index: -1,
                flags: (SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC));

            if (_renderer == IntPtr.Zero)
            {
                Debug.WriteLine($"Could not create renderer: {SDL.SDL_GetError()}");
                return;
            }

            _texture = SDL.SDL_CreateTexture(_renderer,
                SDL.SDL_PIXELFORMAT_YV12,
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, _rectOrigin.w, _rectOrigin.h
            );

            if (_texture == IntPtr.Zero)
            {
                Debug.WriteLine($"Could not create texture: {SDL.SDL_GetError()}");
                return;
            }
        }

        public void Update(byte[][] yuvData, int[] lineSizes)
        {
            if (!Initialized)
            {
                Debug.WriteLine("SDL Video not initialized yet...");
                return;
            }

            var tFrameRect = new SDL.SDL_Rect { x = 0, y = 0, w = _rectOrigin.w, h = _rectOrigin.h };
            fixed (byte* y = yuvData[0], u = yuvData[1], v = yuvData[2])
            {
                if (SDL.SDL_UpdateYUVTexture(
                    _texture,
                    ref _rectOrigin,
                    (IntPtr)y,
                    lineSizes[0],
                    (IntPtr)u,
                    lineSizes[1],
                    (IntPtr)v,
                    lineSizes[2]
                ) < 0)
                {
                    Debug.WriteLine($"Could not update texture: {SDL.SDL_GetError()}");
                    return;
                }
            }

            SDL.SDL_RenderClear(_renderer);
            SDL.SDL_RenderCopy(_renderer, _texture, IntPtr.Zero, IntPtr.Zero);
            SDL.SDL_RenderPresent(_renderer);
        }

        public void Close()
        {
            SDL.SDL_DestroyTexture(_texture);
            SDL.SDL_DestroyRenderer(_renderer);
            SDL.SDL_DestroyWindow(_window);
            SDL.SDL_Quit();
        }
    }
}
