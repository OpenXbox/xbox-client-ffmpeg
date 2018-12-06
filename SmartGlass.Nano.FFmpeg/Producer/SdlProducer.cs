using System;
using System.Diagnostics;
using SDL2;

namespace SmartGlass.Nano.FFmpeg
{
    public class SdlProducer
    {
        private readonly NanoClient _client;
        private SdlInput Input { get; set; }

        private event EventHandler<InputEventArgs> HandleInputEvent;

        public SdlProducer(NanoClient client)
        {
            _client = client;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string controllerMappingPath = System.IO.Path
                                            .Combine(baseDir, "gamecontrollerdb.txt");

            Input = new SdlInput(controllerMappingPath);

            HandleInputEvent += Input.HandleInput;
        }

        public void MainLoop()
        {
            bool success = Input.Initialize();
            if (!success)
                throw new InvalidOperationException("Failed to init SDL Input");

            bool running = true;
            while (running)
            {
                if (SDL.SDL_PollEvent(out SDL.SDL_Event sdlEvent) > 0)
                {
                    switch (sdlEvent.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            Console.WriteLine("SDL Quit, bye!");
                            running = false;
                            break;

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
                                    Button = SdlInputMapping.GetButton((SDL.SDL_GameControllerButton)pressedButton.button)
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
                                    Button = SdlInputMapping.GetButton((SDL.SDL_GameControllerButton)releasedButton.button)
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
                                    Axis = SdlInputMapping.GetAxis((SDL.SDL_GameControllerAxis)axisEvent.axis),
                                    AxisValue = axisEvent.axisValue
                                });
                            break;
                    }

                    _client.Input.SendInputFrame(
                        Input.Timestamp, Input.Buttons, Input.Analog, Input.Extension)
                            .GetAwaiter().GetResult();

                    // TODO: Check if sleep is necessary
                    System.Threading.Thread.Sleep(millisecondsTimeout: 10);
                }
            }
        }
    }
}