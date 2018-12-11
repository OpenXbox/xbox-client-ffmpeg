using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SDL2;

namespace SmartGlass.Nano.FFmpeg.Producer
{
    public class SdlProducer
    {
        readonly NanoClient _client;
        SdlInput Input { get; set; }
        event EventHandler<InputEventArgs> HandleInputEvent;
        public bool IsRunning { get; private set; }

        public SdlProducer(NanoClient client)
        {
            _client = client;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            Input = new SdlInput($"{baseDir}/gamecontrollerdb.txt");
            HandleInputEvent += Input.HandleInput;
        }

        public void MainLoop()
        {
            if (!Input.Initialize())
                throw new InvalidOperationException("Failed to init SDL Input");

            IsRunning = true;

            new Thread(() =>
            {
                while (IsRunning)
                {
                    try
                    {
                        _client.Input.SendInputFrame(
                            Input.Timestamp, Input.Buttons, Input.Analog, Input.Extension)
                                .GetAwaiter().GetResult();
                    }
                    catch
                    {
                        Thread.Sleep(millisecondsTimeout: 5);
                    }
                }
            }).Start();

            while (IsRunning)
            {
                if (SDL.SDL_PollEvent(out SDL.SDL_Event sdlEvent) > 0)
                {
                    switch (sdlEvent.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            Console.WriteLine("SDL Quit, bye!");
                            IsRunning = false;
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
                }
                Task.Delay(10).GetAwaiter().GetResult();
            }

            // closes input controller
            Input.CloseController();
        }
    }
}
