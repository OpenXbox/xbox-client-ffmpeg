using System;
using SDL2;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SmartGlass.Nano.Packets;
using System.Diagnostics;

namespace SmartGlass.Nano.FFmpeg
{
    public unsafe class SdlInput
    {
        public bool Initialized { get; private set; }
        public string ControllerMappingFilepath { get; private set; }

        private IntPtr _controller;
        public SdlInput(string controllerMappingFilepath)
        {
            ControllerMappingFilepath = controllerMappingFilepath;
        }

        public int Initialize()
        {
            int ret = SDL.SDL_Init(SDL.SDL_INIT_GAMECONTROLLER);
            if (ret < 0)
            {
                Debug.WriteLine("SDL_Init GAMECONTROLLER failed: {0}", SDL.SDL_GetError());
                return 0;
            }
            if (ControllerMappingFilepath != null)
            {
                ret = SDL.SDL_GameControllerAddMappingsFromFile(ControllerMappingFilepath);
                if (ret < 0)
                {
                    Debug.WriteLine("Failed to load GameControllerDB, {0}", ControllerMappingFilepath);
                }
            }

            int numJoysticks = SDL.SDL_NumJoysticks();
            Debug.WriteLine("Found {0} joysticks", numJoysticks);
            for (int i = 0; i < numJoysticks; i++)
            {
                if (SDL.SDL_IsGameController(i) == SDL.SDL_bool.SDL_TRUE)
                {
                    Debug.WriteLine("Found a gamecontroller (Index: {0})", i);
                    Open(i);
                }
            }

            return 1;
        }

        public int Open(int joystickIndex)
        {
            if (!Initialized)
            {
                Debug.WriteLine("SDL Input not initialized yet...");
                return -1;
            }

            if (SDL.SDL_IsGameController(joystickIndex) == SDL.SDL_bool.SDL_FALSE)
            {
                Debug.WriteLine("Joystick device does not support controllermode");
                return -1;
            }
            if (_controller != IntPtr.Zero)
            {
                Debug.WriteLine("There is an active controller already.");
                Debug.WriteLine("Closing the old one...");
                Close();
            }

            _controller = SDL.SDL_GameControllerOpen(joystickIndex);
            if (_controller == IntPtr.Zero)
            {
                Debug.WriteLine("Failed to open controller: {0}", joystickIndex);
                return -1;
            }
            string name = SDL.SDL_GameControllerNameForIndex(joystickIndex);
            Debug.WriteLine("Opened Controller {0} {1}", joystickIndex, name);
            return 0;
        }

        public void Close()
        {
            if (_controller == IntPtr.Zero)
            {
                Debug.WriteLine("Controller is not initialized, cannot remove");
                return;
            }
            Debug.WriteLine("Removing Controller...");
            SDL.SDL_GameControllerClose(_controller);
        }
    }
}