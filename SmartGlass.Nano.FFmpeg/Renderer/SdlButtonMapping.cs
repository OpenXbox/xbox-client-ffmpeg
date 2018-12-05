using System;
using System.Collections.Generic;

using static SDL2.SDL;

namespace SmartGlass.Nano.FFmpeg
{
    public static class SdlButtonMapping
    {
        public static Dictionary<SDL_GameControllerButton, NanoGamepadButton> Map =
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

        public static NanoGamepadButton GetButton(SDL_GameControllerButton button)
        {
            return Map[button];
        }
    }
}