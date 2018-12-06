using System;

namespace SmartGlass.Nano
{
    public class InputButtons2 : SmartGlass.Nano.Packets.InputButtons
    {
        public InputButtons2()
            : base()
        {
        }

        // TODO: Add to core library
        public byte GetValue(NanoGamepadButton button)
        {
            switch (button)
            {
                case NanoGamepadButton.A:
                    return A;
                case NanoGamepadButton.B:
                    return B;
                case NanoGamepadButton.X:
                    return X;
                case NanoGamepadButton.Y:
                    return Y;
                case NanoGamepadButton.Start:
                    return Start;
                case NanoGamepadButton.Back:
                    return Back;
                case NanoGamepadButton.Guide:
                    return Guide;
                case NanoGamepadButton.DPadDown:
                    return DPadDown;
                case NanoGamepadButton.DPadLeft:
                    return DPadLeft;
                case NanoGamepadButton.DPadRight:
                    return DPadRight;
                case NanoGamepadButton.DPadUp:
                    return DPadUp;
                case NanoGamepadButton.LeftShoulder:
                    return LeftShoulder;
                case NanoGamepadButton.LeftThumbstick:
                    return LeftThumbstick;
                case NanoGamepadButton.RightShoulder:
                    return RightShoulder;
                case NanoGamepadButton.RightThumbstick:
                    return RightThumbstick;
                default:
                    throw new NotSupportedException();
            }
        }

        public void SetValue(NanoGamepadButton button, byte value)
        {
            switch (button)
            {
                case NanoGamepadButton.A:
                    A = value;
                    break;
                case NanoGamepadButton.B:
                    B = value;
                    break;
                case NanoGamepadButton.X:
                    X = value;
                    break;
                case NanoGamepadButton.Y:
                    Y = value;
                    break;
                case NanoGamepadButton.Start:
                    Start = value;
                    break;
                case NanoGamepadButton.Back:
                    Back = value;
                    break;
                case NanoGamepadButton.Guide:
                    Guide = value;
                    break;
                case NanoGamepadButton.DPadDown:
                    DPadDown = value;
                    break;
                case NanoGamepadButton.DPadLeft:
                    DPadLeft = value;
                    break;
                case NanoGamepadButton.DPadRight:
                    DPadRight = value;
                    break;
                case NanoGamepadButton.DPadUp:
                    DPadUp = value;
                    break;
                case NanoGamepadButton.LeftShoulder:
                    LeftShoulder = value;
                    break;
                case NanoGamepadButton.LeftThumbstick:
                    LeftThumbstick = value;
                    break;
                case NanoGamepadButton.RightShoulder:
                    RightShoulder = value;
                    break;
                case NanoGamepadButton.RightThumbstick:
                    RightThumbstick = value;
                    break;
                default:
                    throw new NotSupportedException();
            }

        }
    }
}