﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VTOLVR.Multiplayer;

namespace Triquetra.Input
{
    public enum ControllerAction
    {
        None,
        Throttle,
        HeloPower,
        Pitch,
        Yaw,
        Roll,
        JoystickTrigger,
        JoystickThumbStick,
        ThrottleThumbStick,
        Countermeasures,
        Brakes, // ThrottleTrigger
        FlapsIncrease,
        FlapsDecrease,
        FlapsCycle,
        SwitchWeapon,
        // LandingGear,
        Visor,
        NightVisionGoggles,
        PTT,
        VRInteract,
        Print
    }

    public static class ControllerActions
    {
        public static class Radio
        {
            internal static CockpitTeamRadioManager radioManager;

            static bool wasTalking = false;
            public static void PTT(Binding binding, int joystickValue)
            {
                if (radioManager == null)
                    radioManager = FindRadioManager();
                if (radioManager == null)
                    return;

                if (binding.GetButtonPressed(joystickValue))
                {
                    wasTalking = true;
                    radioManager.ptt?.StartVoice();
                }
                else if (wasTalking)
                {
                    radioManager.ptt?.StopVoice();
                    wasTalking = false;
                }
            }

            internal static CockpitTeamRadioManager FindRadioManager()
            {
                return GameObject.FindObjectOfType<CockpitTeamRadioManager>();
            }
        }
        public static class Helmet
        {
            private static HelmetController helmetController;

            public static void ToggleVisor(Binding binding, int joystickValue, int delta = 1)
            {
                if (helmetController == null)
                    helmetController = FindHelmetController();
                if (helmetController == null)
                    return;

                if (binding.GetButtonPressed(joystickValue))
                {
                    helmetController.ToggleVisor();
                }
            }

            public static void ToggleNVG(Binding binding, int joystickValue, int delta = 1)
            {
                if (helmetController == null)
                    helmetController = FindHelmetController();
                if (helmetController == null)
                    return;

                if (binding.GetButtonPressed(joystickValue))
                {
                    helmetController.ToggleNVG();
                }
            }

            private static HelmetController FindHelmetController()
            {
                return GameObject.FindObjectsOfType<HelmetController>(false).First();
            }
        }

        public static class Flaps
        {
            internal static VRLever flaps;
            public static void IncreaseFlaps(Binding binding, int joystickValue, int delta = 1)
            {
                if (flaps == null)
                    flaps = FindFlaps();
                if (flaps == null)
                    return;

                if (binding.GetButtonPressed(joystickValue))
                {
                    Interactions.MoveLever(flaps, delta, true);
                }
            }

            public static void DecreaseFlaps(Binding binding, int joystickValue)
            {
                IncreaseFlaps(binding, joystickValue, -1);
            }

            public static void CycleFlaps(Binding binding, int joystickValue)
            {
                if (flaps == null)
                    flaps = FindFlaps();
                if (flaps == null)
                    return;

                if (binding.GetButtonPressed(joystickValue))
                {
                    Interactions.MoveLever(flaps, 1, false);
                }
            }

            internal static VRLever FindFlaps()
            {
                return GameObject.FindObjectsOfType<VRLever>(false).Where(l => l?.GetComponent<VRInteractable>()?.interactableName == "Flaps").FirstOrDefault();
            }
        }

        // TODO: countermeasures, alt. controls (from holding trigger on throttle)
        public static class Helicopter
        {
            internal static VRThrottle power;
            public static void SetPower(Binding binding, int joystickValue)
            {
                if (power == null)
                    power = FindThrottle();
                if (power == null)
                    return;

                Interactions.SetThrottle(power, binding.GetAxisAsFloat(joystickValue));
            }

            public static void MoveThrottle(Binding binding, int joystickValue, float delta)
            {
                if (power == null)
                    return;

                Interactions.MoveThrottle(power, (float)Math.Round(binding.GetAxisAsFloat(joystickValue) - 0.5f, 1) * delta);
            }

            internal static VRThrottle FindThrottle()
            {
                return GameObject.FindObjectsOfType<VRThrottle>(false).Where(t => t.interactable?.interactableName == "Power").FirstOrDefault();
            }
        }

        public static class Throttle
        {
            internal static VRThrottle throttle;
            public static void SetThrottle(Binding binding, int joystickValue)
            {
                if (throttle == null)
                    return;

                Interactions.SetThrottle(throttle, binding.GetAxisAsFloat(joystickValue));
            }

            public static void MoveThrottle(Binding binding, int joystickValue, float delta)
            {
                if (throttle == null)
                    return;

                Interactions.MoveThrottle(throttle, (float)Math.Round(binding.GetAxisAsFloat(joystickValue) - 0.5f, 1) * delta);
            }

            private static bool triggerPressed = false;
            public static void TriggerBrakes(Binding binding, int joystickValue)
            {

                if (throttle == null)
                    return;

                if (triggerPressed == false && binding.GetButtonPressed(joystickValue))
                {
                    triggerPressed = true;
                    throttle.OnTriggerDown?.Invoke();
                }
                if (triggerPressed == true && !binding.GetButtonPressed(joystickValue))
                {
                    triggerPressed = false;
                    throttle.OnTriggerUp?.Invoke();
                }

                throttle.OnTriggerAxis?.Invoke(binding.GetAxisAsFloat(joystickValue));
            }

            #region Thumbstick
            private static bool thumbstickButtonPressed = false;
            public static void ThumbstickButton(Binding binding, int joystickValue)
            {
                if (throttle == null)
                    return;

                if (binding.GetButtonPressed(joystickValue))
                {
                    throttle.OnStickPressed?.Invoke();
                }
                if (thumbstickButtonPressed == false && binding.GetButtonPressed(joystickValue))
                {
                    thumbstickButtonPressed = true;
                    throttle.OnStickPressDown?.Invoke();
                }
                if (thumbstickButtonPressed == true && !binding.GetButtonPressed(joystickValue))
                {
                    thumbstickButtonPressed = false;
                    throttle.OnStickPressUp?.Invoke();
                }
            }

            public static bool ThumbstickUp = false;
            public static bool ThumbstickRight = false;
            public static bool ThumbstickDown = false;
            public static bool ThumbstickLeft = false;
            private static bool thumbstickWasZero = false;
            private static bool thumbstickWasMoving = false;
            public static void UpdateThumbstick()
            {
                if (throttle == null)
                    return;

                Vector3 vector = new Vector3();
                if (ThumbstickRight)
                    vector.x += 1;
                if (ThumbstickLeft)
                    vector.x -= 1;
                if (ThumbstickUp)
                    vector.y += 1;
                if (ThumbstickDown)
                    vector.y -= 1;

                if (vector != Vector3.zero)
                {
                    thumbstickWasZero = false;
                    throttle.OnSetThumbstick?.Invoke(vector);
                    thumbstickWasMoving = true;
                }
                else
                {
                    if (!thumbstickWasZero)
                    {
                        throttle.OnSetThumbstick?.Invoke(vector);
                        thumbstickWasZero = true;
                        thumbstickWasMoving = true;
                    }
                    else if (thumbstickWasMoving)
                    {
                        throttle.OnResetThumbstick?.Invoke();
                        thumbstickWasMoving = false;
                    }
                }
            }

            public static void Thumbstick(Binding binding, int joystickValue)
            {
                switch (binding.ThumbstickDirection)
                {
                    case ThumbstickDirection.Up:
                        ThumbstickUp = binding.GetButtonPressed(joystickValue);
                        break;
                    case ThumbstickDirection.Down:
                        ThumbstickDown = binding.GetButtonPressed(joystickValue);
                        break;
                    case ThumbstickDirection.Right:
                        ThumbstickRight = binding.GetButtonPressed(joystickValue);
                        break;
                    case ThumbstickDirection.Left:
                        ThumbstickLeft = binding.GetButtonPressed(joystickValue);
                        break;
                    case ThumbstickDirection.Press:
                        ThumbstickButton(binding, joystickValue);
                        break;
                }
            }
            #endregion

            private static bool cmPressed = false;
            public static void Countermeasures(Binding binding, int joystickValue)
            {
                if (throttle == null)
                    return;

                if (cmPressed == false && binding.GetButtonPressed(joystickValue))
                {
                    cmPressed = true;
                    throttle.OnMenuButtonDown?.Invoke();
                }
                if (cmPressed == true && !binding.GetButtonPressed(joystickValue))
                {
                    cmPressed = false;
                    throttle.OnMenuButtonUp?.Invoke();
                }
            }

            internal static VRThrottle FindThrottle()
            {
                return GameObject.FindObjectOfType<VRThrottle>(false);
            }
        }

        public static class Joystick
        {
            internal static VRJoystick joystick;
            private static Vector3 stickVector = new Vector3(0, 0, 0);
            public static void SetPitch(Binding binding, int joystickValue)
            {
                stickVector.x = binding.GetAxisAsFloat(joystickValue) - 0.5f;
            }
            public static void SetYaw(Binding binding, int joystickValue)
            {
                stickVector.y = binding.GetAxisAsFloat(joystickValue) - 0.5f;
            }
            public static void SetRoll(Binding binding, int joystickValue)
            {
                stickVector.z = binding.GetAxisAsFloat(joystickValue) - 0.5f;
            }

            public static void UpdateStick()
            {
                if (joystick == null)
                    return;

                joystick.OnSetStick.Invoke(stickVector * 2); // stickVector is usually -0.5 to 0.5
            }

            private static bool triggerPressed = false;
            public static void Trigger(Binding binding, int joystickValue)
            {
                if (joystick == null)
                    return;

                if (triggerPressed == false && binding.GetButtonPressed(joystickValue))
                {
                    triggerPressed = true;
                    joystick.OnTriggerDown?.Invoke();
                }
                if (triggerPressed == true && !binding.GetButtonPressed(joystickValue))
                {
                    triggerPressed = false;
                    joystick.OnTriggerUp?.Invoke();
                }
                joystick.OnTriggerAxis?.Invoke(binding.GetAxisAsFloat(joystickValue));
            }

            #region Thumbstick
            private static bool thumbstickButtonPressed = false;
            public static void ThumbstickButton(Binding binding, int joystickValue)
            {
                if (joystick == null)
                    return;

                if (binding.GetButtonPressed(joystickValue))
                {
                    joystick.OnThumbstickButton?.Invoke();
                }
                if (thumbstickButtonPressed == false && binding.GetButtonPressed(joystickValue))
                {
                    thumbstickButtonPressed = true;
                    joystick.OnThumbstickButtonDown?.Invoke();
                }
                if (thumbstickButtonPressed == true && !binding.GetButtonPressed(joystickValue))
                {
                    thumbstickButtonPressed = false;
                    joystick.OnThumbstickButtonUp?.Invoke();
                }
            }

            public static bool ThumbstickUp = false;
            public static bool ThumbstickRight = false;
            public static bool ThumbstickDown = false;
            public static bool ThumbstickLeft = false;
            private static bool thumbstickWasZero = false;
            private static bool thumbstickWasMoving = false;
            public static void UpdateThumbstick()
            {
                if (joystick == null)
                    return;

                Vector3 vector = new Vector3();
                if (ThumbstickRight)
                    vector.x += 1;
                if (ThumbstickLeft)
                    vector.x -= 1;
                if (ThumbstickUp)
                    vector.y += 1;
                if (ThumbstickDown)
                    vector.y -= 1;

                if (vector != Vector3.zero)
                {
                    thumbstickWasZero = false;
                    joystick.OnSetThumbstick?.Invoke(vector);
                    thumbstickWasMoving = true;
                }
                else
                {
                    if (!thumbstickWasZero)
                    {
                        joystick.OnSetThumbstick?.Invoke(vector);
                        thumbstickWasZero = true;
                        thumbstickWasMoving = true;
                    }
                    else if (thumbstickWasMoving)
                    {
                        joystick.OnResetThumbstick?.Invoke();
                        thumbstickWasMoving = false;
                    }
                }
            }

            public static void Thumbstick(Binding binding, int joystickValue)
            {
                switch (binding.ThumbstickDirection)
                {
                    case ThumbstickDirection.Up:
                        ThumbstickUp = binding.GetButtonPressed(joystickValue);
                        break;
                    case ThumbstickDirection.Down:
                        ThumbstickDown = binding.GetButtonPressed(joystickValue);
                        break;
                    case ThumbstickDirection.Right:
                        ThumbstickRight = binding.GetButtonPressed(joystickValue);
                        break;
                    case ThumbstickDirection.Left:
                        ThumbstickLeft = binding.GetButtonPressed(joystickValue);
                        break;
                    case ThumbstickDirection.Press:
                        ThumbstickButton(binding, joystickValue);
                        break;
                }
            }
            #endregion

            internal static VRJoystick FindJoystick()
            {
                var joysticks = GameObject.FindObjectsOfType<VRJoystick>(false);

                var joystick = joysticks.FirstOrDefault(stick => stick.name == "joyInteractable_sideFront") ??
                        joysticks.FirstOrDefault();
                    
                return joystick;
            }

            private static bool menuButtonPressed = false;
            public static void SwitchWeapon(Binding binding, int joystickValue)
            {
                if (joystick == null)
                    return;

                if (!menuButtonPressed && binding.GetButtonPressed(joystickValue))
                {
                    menuButtonPressed = true;
                    joystick.OnMenuButtonDown?.Invoke();
                }
                else if (menuButtonPressed && !binding.GetButtonPressed(joystickValue))
                {
                    menuButtonPressed = false;
                    joystick.OnMenuButtonUp?.Invoke();
                }
            }
        }

        public static void Print(Binding binding, int joystickValue)
        {
            Plugin.Write($"Triquetra.Input: Axis is {binding.Offset}. Value is {joystickValue}");
        }

        internal static void TryGetSticks()
        {
            if (Plugin.IsFlyingScene())
            {
                if (Joystick.joystick == null)
                    Joystick.joystick = Joystick.FindJoystick();
                if (Throttle.throttle == null)
                    Throttle.throttle = Throttle.FindThrottle();
            }
        }
    }
}
