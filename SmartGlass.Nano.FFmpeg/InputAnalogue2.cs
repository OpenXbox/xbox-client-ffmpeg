using System;

namespace SmartGlass.Nano
{
    public class InputAnalogue2 : SmartGlass.Nano.Packets.InputAnalogue
    {
        public InputAnalogue2()
            : base()
        {
        }

        // TODO: Add to core library
        // TODO: Verify data types - float vs ushort vs byte
        public ushort GetValue(NanoGamepadAxis axis)
        {
            switch (axis)
            {
                case NanoGamepadAxis.LeftX:
                    return LeftThumbX;
                case NanoGamepadAxis.LeftY:
                    return LeftThumbY;
                case NanoGamepadAxis.RightX:
                    return RightThumbX;
                case NanoGamepadAxis.RightY:
                    return RightThumbY;
                case NanoGamepadAxis.TriggerLeft:
                    return LeftTrigger;
                case NanoGamepadAxis.TriggerRight:
                    return RightTrigger;
            }
            throw new NotSupportedException();
        }

        public void SetValue(NanoGamepadAxis axis, float value)
        {
            switch (axis)
            {
                case NanoGamepadAxis.LeftX:
                    LeftThumbX = (ushort)value;
                    break;
                case NanoGamepadAxis.LeftY:
                    LeftThumbY = (ushort)value;
                    break;
                case NanoGamepadAxis.RightX:
                    RightThumbX = (ushort)value;
                    break;
                case NanoGamepadAxis.RightY:
                    RightThumbY = (ushort)value;
                    break;
                case NanoGamepadAxis.TriggerLeft:
                    LeftTrigger = (byte)value;
                    break;
                case NanoGamepadAxis.TriggerRight:
                    RightTrigger = (byte)value;
                    break;
            }
            throw new NotSupportedException();
        }
    }
}