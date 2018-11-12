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
        //public event EventHandler<SinkEventArgs> HandlerSinkEvent;

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

        /*
        private void FireSinkEvent(SinkEventArgs e)
        {
            EventHandler<SinkEventArgs> handler = HandlerSinkEvent;
            if (handler != null)
            {
                Debug.WriteLine("Firing Event: {0}", e.Type);
                handler(this, e);
            }
        }
        */

        public void MainLoop()
        {
            while (true)
            {
                if (SDL.SDL_PollEvent(out SDL.SDL_Event sdlEvent) > 0)
                {
                    switch (sdlEvent.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            //ev = new SinkEvent(EventType.RENDER_QUIT, 0, 0);
                            break;
                        case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                            int ret = Input.Open(sdlEvent.cdevice.which);
                            //if (ret == 0)
                            // FireSinkEvent(new ControllerStatechangeArgs(0, 1));
                            break;
                        case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                            //FireSinkEvent(new ControllerStatechangeArgs(0, 0));
                            Input.Close();
                            break;
                        case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                            SDL.SDL_ControllerButtonEvent pressedButton = sdlEvent.cbutton;
                            //FireSinkEvent(new ControllerButtonpressArgs(pressedButton.Button, true));
                            break;
                        case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                            SDL.SDL_ControllerButtonEvent releasedButton = sdlEvent.cbutton;
                            //FireSinkEvent(new ControllerButtonpressArgs(releasedButton.Button, false));
                            break;
                        case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
                            SDL.SDL_ControllerAxisEvent axisEvent = sdlEvent.caxis;
                            //FireSinkEvent(new ControllerAxismoveArgs(axisEvent.Axis, axisEvent.Value));
                            break;
                    }
                }
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
