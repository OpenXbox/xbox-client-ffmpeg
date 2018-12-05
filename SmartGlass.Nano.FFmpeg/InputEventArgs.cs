using System;
using SmartGlass.Nano;

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
        public float[] AxisValues { get; internal set; }
    }
}