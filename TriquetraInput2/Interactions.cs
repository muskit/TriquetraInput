using System;
using UnityEngine;

namespace Triquetra.Input
{
    public static class Interactions
    {
        const float knobTwistSpeed = 0.1f;
        public static void MoveTwistKnob(VRTwistKnob twistKnob, bool antiClockwise, float delta = knobTwistSpeed)
        {
            twistKnob.SetKnobValue(twistKnob.currentValue + (antiClockwise ? -knobTwistSpeed : knobTwistSpeed));
        }
        
        public static void SetTwistKnob(VRTwistKnob twistKnob, float value)
        {
            twistKnob.SetKnobValue(value);
        }

        public static void MoveLever(VRLever lever, int delta = 1, bool clamp = false)
        {
            int nextState;
            if (clamp)
                nextState = Math.Max(Math.Min(lever.currentState + delta, lever.states - 1), 0);
            else
                nextState = (lever.currentState + delta) % lever.states;
            lever.RemoteSetState(nextState);
        }
        
        public static void SetLever(VRLever lever, int state)
        {
            state = Mathf.Clamp(state, 0, lever.states - 1);
            lever.RemoteSetState(state);
        }

        public static void MoveTwistKnobInt(VRTwistKnobInt twistKnobInt, int delta = 1, bool clamp = false)
        {
            int nextState;
            if (clamp)
                nextState = Math.Max(Math.Min(twistKnobInt.currentState + delta, twistKnobInt.states - 1), 0);
            else
                nextState = (twistKnobInt.currentState + delta) % twistKnobInt.states;
            twistKnobInt.RemoteSetState(nextState);
        }

        public static void SetTwistKnobInt(VRTwistKnobInt twistKnobInt, int state)
        {
            state = Mathf.Clamp(state, 0, twistKnobInt.states - 1);
            twistKnobInt.RemoteSetState(state);
        }

        public static void MoveThrottle(VRThrottle throttle, float delta)
        {
            throttle.RemoteSetThrottleForceEvents(Math.Min(Math.Max(throttle.currentThrottle + delta, 0f), 1f));
        }
        public static void SetThrottle(VRThrottle throttle, float value)
        {
            throttle.RemoteSetThrottleForceEvents(Math.Min(Math.Max(value, 0f), 1f));
        }

        public static void Interact(VRInteractable interactable, float value = float.MinValue, bool antiClockwise = true)
        {
            VRLever lever = interactable.GetComponent<VRLever>();
            VRTwistKnob twistKnob = interactable.GetComponent<VRTwistKnob>();
            VRTwistKnobInt twistKnobInt = interactable.GetComponent<VRTwistKnobInt>();
            VRButton button = interactable.GetComponent<VRButton>();
            EjectHandle eject = interactable.GetComponent<EjectHandle>();
            VRDoor door = interactable.GetComponent<VRDoor>();
            VRKeyboard.VRKey vrKey = interactable.GetComponent<VRKeyboard.VRKey>();

            if (lever != null)
            {
                if (value != float.MinValue)
                    SetLever(lever, (int)value);
                else
                    MoveLever(lever, 1);
            }
            else if (twistKnob != null)
            {
                if (value != float.MinValue)
                    SetTwistKnob(twistKnob, value);
                else
                    MoveTwistKnob(twistKnob, antiClockwise);
            }
            else if (twistKnobInt != null)
            {
                if (value != float.MinValue)
                    SetTwistKnobInt(twistKnobInt, (int)value);
                else
                    MoveTwistKnobInt(twistKnobInt, 1);
            }
            else if (vrKey != null)
            {
                vrKey.OnPress();
            }
            else if (button != null)
            {
                interactable.StartInteraction();
            }
            else if (eject != null)
            {
                eject.OnHandlePull?.Invoke();
            }
            else if (door != null)
            {
                if (value != float.MinValue)
                    door.RemoteSetState(value);
                else
                    door.RemoteSetState(door.isLatched ? 1f : 0f); // 1f = to open, 0f = to close
            }
            else
            {
                try
                {
                    interactable.StartInteraction();
                }
                catch (NullReferenceException)
                {
                    interactable.OnInteract?.Invoke();
                }
            }
        }

        public static void AntiInteract(VRInteractable interactable)
        {
            VRButton button = interactable.GetComponent<VRButton>();
            if (button != null)
            {
                interactable.StopInteraction();
                button.Vrint_OnStopInteraction(null);
            }
            else
            {
                interactable.StopInteraction();
            }
        }
    }
}