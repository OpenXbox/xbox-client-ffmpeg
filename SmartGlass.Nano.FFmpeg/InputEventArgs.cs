using System;


// TODO: Move into core library?
namespace SmartGlass.Nano.FFmpeg
{
    public enum InputEventType
    {
        ControllerAdded,
        ControllerRemoved,
        ButtonPressed,
        ButtonReleased,
        AxisMoved
    }

    public class InputEventArgs : EventArgs
    {
        public InputEventType EventType { get; internal set; }
        public int ControllerIndex { get; internal set; }
        public uint Timestamp { get; internal set; }
        public NanoGamepadButton Button { get; internal set; }
        public NanoGamepadAxis Axis { get; internal set; }
        public float AxisValue { get; internal set; }
    }
}