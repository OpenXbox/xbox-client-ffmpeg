using System;
using System.Collections.Generic;

using static SDL2.SDL;

namespace SmartGlass.Nano.FFmpeg
{
    public static class SdlInputMapping
    {
        static Dictionary<SDL_GameControllerButton, NanoGamepadButton> ButtonMap =
            new Dictionary<SDL_GameControllerButton, NanoGamepadButton>()
            {
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A, NanoGamepadButton.A},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B, NanoGamepadButton.B},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X, NanoGamepadButton.X},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y, NanoGamepadButton.Y},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT, NanoGamepadButton.DPadLeft},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT, NanoGamepadButton.DPadRight},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP, NanoGamepadButton.DPadUp},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN, NanoGamepadButton.DPadDown},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START, NanoGamepadButton.Start},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK, NanoGamepadButton.Back},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER, NanoGamepadButton.LeftShoulder},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER, NanoGamepadButton.RightShoulder},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK, NanoGamepadButton.LeftThumbstick},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK, NanoGamepadButton.RightThumbstick},
                {SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE, NanoGamepadButton.Guide}
            };

        static Dictionary<SDL_GameControllerAxis, NanoGamepadAxis> AxisMap =
            new Dictionary<SDL_GameControllerAxis, NanoGamepadAxis>()
            {
                {SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX, NanoGamepadAxis.LeftX},
                {SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY, NanoGamepadAxis.LeftY},
                {SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX, NanoGamepadAxis.RightX},
                {SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY, NanoGamepadAxis.RightY},
                {SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT, NanoGamepadAxis.TriggerLeft},
                {SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT, NanoGamepadAxis.TriggerRight}
            };

        public static NanoGamepadButton GetButton(SDL_GameControllerButton button)
        {
            return ButtonMap[button];
        }

        public static NanoGamepadAxis GetAxis(SDL_GameControllerAxis axis)
        {
            return AxisMap[axis];
        }
    }
}