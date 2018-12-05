using System;
using SDL2;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SmartGlass.Nano.Packets;
using System.Diagnostics;

namespace SmartGlass.Nano.FFmpeg
{
    public class SdlRenderer
    {
        public SdlAudio Audio { get; private set; }
        public SdlVideo Video { get; private set; }
        public SdlInput Input { get; private set; }
        public event EventHandler<InputEventArgs> HandleInputEvent;

        public SdlRenderer()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string controllerMappingPath = System.IO.Path
                                            .Combine(baseDir, "gamecontrollerdb.txt");
            Debug.WriteLine("Using Controller Mapping file: {0}", controllerMappingPath);

            Audio = new SdlAudio();
            Video = new SdlVideo();
            Input = new SdlInput(controllerMappingPath);
        }

        public void MainLoop()
        {
            while (true)
            {
                if (SDL.SDL_PollEvent(out SDL.SDL_Event sdlEvent) > 0)
                {
                    switch (sdlEvent.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            Console.WriteLine("SDL Quit, bye!");
                            return;

                        case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                            HandleInputEvent?.Invoke(this,
                                new InputEventArgs()
                                {
                                    EventType = InputEventType.ControllerAdded,
                                    Timestamp = sdlEvent.cdevice.timestamp,
                                    ControllerIndex = sdlEvent.cdevice.which
                                });
                            break;

                        case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                            HandleInputEvent?.Invoke(this,
                                new InputEventArgs()
                                {
                                    EventType = InputEventType.ControllerRemoved,
                                    Timestamp = sdlEvent.cdevice.timestamp,
                                    ControllerIndex = sdlEvent.cdevice.which
                                });
                            break;

                        case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                            SDL.SDL_ControllerButtonEvent pressedButton = sdlEvent.cbutton;
                            HandleInputEvent?.Invoke(this,
                                new InputEventArgs()
                                {
                                    EventType = InputEventType.ButtonPressed,
                                    ControllerIndex = sdlEvent.cdevice.which,
                                    Timestamp = pressedButton.timestamp,
                                    Button = SdlButtonMapping.GetButton((SDL.SDL_GameControllerButton)pressedButton.button)
                                });
                            break;

                        case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                            SDL.SDL_ControllerButtonEvent releasedButton = sdlEvent.cbutton;
                            HandleInputEvent?.Invoke(this,
                                new InputEventArgs()
                                {
                                    EventType = InputEventType.ButtonReleased,
                                    ControllerIndex = sdlEvent.cdevice.which,
                                    Timestamp = releasedButton.timestamp,
                                    Button = SdlButtonMapping.GetButton((SDL.SDL_GameControllerButton)releasedButton.button)
                                });
                            break;

                        case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
                            SDL.SDL_ControllerAxisEvent axisEvent = sdlEvent.caxis;
                            HandleInputEvent?.Invoke(this,
                                new InputEventArgs()
                                {
                                    EventType = InputEventType.AxisMoved,
                                    ControllerIndex = sdlEvent.cdevice.which,
                                    Timestamp = axisEvent.timestamp,
                                    // TODO: Mapping
                                    AxisValues = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f }
                                });
                            break;
                    }
                }

                System.Threading.Thread.Sleep(millisecondsTimeout: 10);
            }
        }
    }
}
