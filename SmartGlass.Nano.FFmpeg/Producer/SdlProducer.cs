using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SDL2;
using SmartGlass.Nano.FFmpeg.Decoder;
using SmartGlass.Nano.FFmpeg.Renderer;
using SmartGlass.Nano.Packets;

namespace SmartGlass.Nano.FFmpeg.Producer
{
    public class SdlProducer : IDisposable
    {
        private bool _disposed = false;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly NanoClient _client;
        SdlAudio _audioRenderer;
        SdlVideo _videoRenderer;
        SdlInput Input { get; set; }
        event EventHandler<InputEventArgs> HandleInputEvent;

        public FFmpegDecoder Decoder;
        public bool useController { get; private set; }

        public SdlProducer(NanoClient client, AudioFormat audioFormat, VideoFormat videoFormat, bool fullscreen = false, bool useController = true)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _client = client;

            _audioRenderer = new SdlAudio((int)audioFormat.SampleRate, (int)audioFormat.Channels);
            _videoRenderer = new SdlVideo((int)videoFormat.Width, (int)videoFormat.Height, fullscreen);

            Decoder = new FFmpegDecoder(client, audioFormat, videoFormat);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            this.useController = useController;

            if (useController) {
                Input = new SdlInput($"{baseDir}/gamecontrollerdb.txt");
                HandleInputEvent += Input.HandleInput;
            }
        }

        Task StartInputFrameSendingTask()
        {
            return Task.Run(async () =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await _client.Input.SendInputFrame(
                            DateTime.UtcNow, Input.Buttons, Input.Analog, Input.Extension);
                    }
                    catch
                    {
                        Thread.Sleep(millisecondsTimeout: 5);
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public void MainLoop()
        {
            if (useController && !Input.Initialize())
                throw new InvalidOperationException("Failed to init SDL Input");

            _audioRenderer.Initialize(1024);
            _videoRenderer.Initialize();

            Decoder.Start();

            if (useController) {
                StartInputFrameSendingTask();
            }

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (Decoder.DecodedAudioQueue.Count > 0)
                {
                    var sample = Decoder.DecodedAudioQueue.Dequeue();
                    _audioRenderer.Update(sample);
                }

                if (Decoder.DecodedVideoQueue.Count > 0)
                {
                    var frame = Decoder.DecodedVideoQueue.Dequeue();
                    _videoRenderer.Update(frame);
                }

                if (SDL.SDL_PollEvent(out SDL.SDL_Event sdlEvent) <= 0)
                {
                    continue;
                }

                switch (sdlEvent.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        Console.WriteLine("SDL Quit, bye!");
                        _cancellationTokenSource.Cancel();
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

            // closes input controller
            if (useController) {
                Input.CloseController();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Decoder.Dispose();
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
