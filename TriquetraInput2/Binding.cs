using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SharpDX.DirectInput;
using UnityEngine;
using UnityEngine.PlayerLoop;
using DeviceType = SharpDX.DirectInput.DeviceType;

namespace Triquetra.Input
{
    public class Binding
    {
        public const int AxisMin = 0;
        public const int AxisMiddle = 32768;
        public const int AxisMax = 65535;
        public const int ButtonMin = 0;
        public const int ButtonMax = 128;
        public const int Deadzone = 8192;
        public const int POVMin = 0;
        public const int POVMax = 36000;

        public static List<Binding> Bindings = new List<Binding>();
        public static DirectInput directInput = new DirectInput();

        public string Name = "New Binding";

        [XmlIgnore] public bool IsKeyboard { get; internal set; } = false;
        [XmlIgnore] public TriquetraJoystick Controller;
        public JoystickOffset Offset;
        [XmlIgnore] public int RawOffset => (int)Offset;
        public bool Invert;
        public AxisCentering AxisCentering = AxisCentering.Normal;
        public TwoAxis SelectedTwoAxis = TwoAxis.Positive;
        public POVFacing POVDirection = POVFacing.Up;
        public ControllerAction OutputAction = ControllerAction.None;
        public ThumbstickDirection ThumbstickDirection = ThumbstickDirection.None;
        public string VRInteractName = "";
        public VRInteractType VRInteractType = VRInteractType.Default;
        public int Value = -1;
        public MFDAction MFDAction = MFDAction.None;
        public KeyboardKey KeyboardKey;
        [XmlIgnore] public DeviceInstance JoystickDevice;
        private string productGuid;

        // For the Xml Serialize/Deserialize
        public string ProductGuid
        {
            get
            {
                if (IsKeyboard)
                    return "keyboard";
                return Controller?.Information.ProductGuid.ToString() ?? "";
            }
            set
            {
                if (value == "keyboard")
                {
                    IsKeyboard = true;
                    return;
                }
                IsKeyboard = false;
                productGuid = value;
            }
        }
        
        public void CreateController(List<DeviceInstance> joystickDevices)
        {
            if (IsKeyboard)
                return;
            JoystickDevice = joystickDevices.FirstOrDefault(x => x.ProductGuid.ToString() == productGuid);
            if (JoystickDevice == null)
                return;

            Controller = new TriquetraJoystick(directInput, JoystickDevice.InstanceGuid);
        }

        public static bool IsButton(int offset) => offset >= (int)JoystickOffset.Buttons0 && offset <= (int)JoystickOffset.Buttons127;
        public static bool IsPOV(int offset) => offset >= (int)JoystickOffset.PointOfViewControllers0 && offset <= (int)JoystickOffset.PointOfViewControllers3;
        public static bool IsAxis(int offset) => !IsButton(offset) && !IsPOV(offset);

        [XmlIgnore] public bool IsOffsetButton => (IsKeyboard && !KeyboardKey.IsAxis) || RawOffset >= (int)JoystickOffset.Buttons0 && RawOffset <= (int)JoystickOffset.Buttons127;
        [XmlIgnore] public bool IsOffsetPOV => !IsKeyboard && RawOffset >= (int)JoystickOffset.PointOfViewControllers0 && RawOffset <= (int)JoystickOffset.PointOfViewControllers3;
        [XmlIgnore] public bool IsOffsetAxis => !IsOffsetButton && !IsOffsetPOV;

        [XmlIgnore] public bool OffsetSelectOpen = false;
        [XmlIgnore] public bool OutputActionSelectOpen = false;
        [XmlIgnore] public bool POVDirectionSelectOpen = false;
        [XmlIgnore] public bool DetectingOffset = false;
        [XmlIgnore] public bool ThumbstickDirectionSelectOpen = false;
        [XmlIgnore] public bool VRInteractTypeSelectOpen = false;
        [XmlIgnore] public bool MFDActionSelectOpen = false;
        [XmlIgnore] public bool AxisCenteringSelectOpen = false;

        [XmlIgnore] public TriquetraJoystick.JoystickUpdated bindingDelegate;

        public Binding()
        {
        }
        
        public Binding(bool isKeyboard)
        {
            if (isKeyboard)
            {
                IsKeyboard = true;
                AxisCentering = AxisCentering.Middle;
                KeyboardKey = new KeyboardKey();
            }
            else
                NextJoystick();
        }

        private int currentJoystickIndex = -1;
        public void NextJoystick()
        {
            if (IsKeyboard)
                return;

            List<DeviceInstance> devices = directInput.GetDevices().Where(x => IsJoystick(x)).ToList();
            if (devices.Count == 0)
            {
                return;
            }
            currentJoystickIndex = (currentJoystickIndex + 1) % devices.Count;

            this.JoystickDevice = devices[currentJoystickIndex];
            Controller = new TriquetraJoystick(directInput, JoystickDevice.InstanceGuid);
        }
        public void PrevJoystick()
        {
            if (IsKeyboard)
                return;

            List<DeviceInstance> devices = directInput.GetDevices().Where(x => IsJoystick(x)).ToList();
            if (devices.Count == 0)
            {
                return;
            }
            currentJoystickIndex = (currentJoystickIndex - 1) % devices.Count;

            this.JoystickDevice = devices[currentJoystickIndex];
            Controller = new TriquetraJoystick(directInput, JoystickDevice.InstanceGuid);
        }

        public static bool IsJoystick(DeviceInstance deviceInstance)
        {
            return deviceInstance.Type == DeviceType.Joystick
                   || deviceInstance.Type == DeviceType.Gamepad
                   || deviceInstance.Type == DeviceType.FirstPerson
                   || deviceInstance.Type == DeviceType.Flight
                   || deviceInstance.Type == DeviceType.Driving
                   || deviceInstance.Type == DeviceType.Supplemental;
        }

        public void RunAction(int joystickValue)
        {
            if (OutputAction == ControllerAction.Print)
            {
                ControllerActions.Print(this, joystickValue);
            }
            if (Plugin.IsFlyingScene()) // Only try and get throttle in a flying scene
            {
                if (OutputAction == ControllerAction.ResetPosition)
                {
                    ControllerActions.Head.ResetPosition(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Throttle)
                {
                    if (IsKeyboard)
                        ControllerActions.Throttle.MoveThrottle(this, joystickValue, 0.025f);
                    else
                        ControllerActions.Throttle.SetThrottle(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.HeloPower)
                {
                    if (IsKeyboard)
                        ControllerActions.Throttle.MoveThrottle(this, joystickValue, 0.025f);
                    else
                        ControllerActions.Helicopter.SetPower(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Pitch)
                {
                    ControllerActions.Joystick.SetPitch(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Yaw)
                {
                    ControllerActions.Joystick.SetYaw(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Roll)
                {
                    ControllerActions.Joystick.SetRoll(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.JoystickTrigger)
                {
                    ControllerActions.Joystick.Trigger(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.SwitchWeapon)
                {
                    ControllerActions.Joystick.SwitchWeapon(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.JoystickThumbStick)
                {
                    ControllerActions.Joystick.Thumbstick(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.ThrottleThumbStick)
                {
                    ControllerActions.Throttle.Thumbstick(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Countermeasures)
                {
                    ControllerActions.Throttle.Countermeasures(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Brakes)
                {
                    ControllerActions.Throttle.TriggerBrakes(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.FlapsIncrease)
                {
                    ControllerActions.Flaps.IncreaseFlaps(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.FlapsDecrease)
                {
                    ControllerActions.Flaps.DecreaseFlaps(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.FlapsCycle)
                {
                    ControllerActions.Flaps.CycleFlaps(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Flaps0)
                {
                    ControllerActions.Flaps.SetFlaps(this, joystickValue, 0);
                }
                else if (OutputAction == ControllerAction.Flaps1)
                {
                    ControllerActions.Flaps.SetFlaps(this, joystickValue, 1);
                }
                else if (OutputAction == ControllerAction.Flaps2)
                {
                    ControllerActions.Flaps.SetFlaps(this, joystickValue, 2);
                }
                else if (OutputAction == ControllerAction.Visor)
                {
                    ControllerActions.Helmet.ToggleVisor(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.OpenVisor)
                {
                    ControllerActions.Helmet.OpenVisor(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.CloseVisor)
                {
                    ControllerActions.Helmet.CloseVisor(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.NightVisionGoggles)
                {
                    ControllerActions.Helmet.ToggleNVG(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.NightVisionGogglesOn)
                {
                    ControllerActions.Helmet.NVGOn(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.NightVisionGogglesOff)
                {
                    ControllerActions.Helmet.NVGOff(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.PTT)
                {
                    ControllerActions.Radio.PTT(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.VRInteract)
                {
                    var interactable = GameObject.FindObjectsOfType<VRInteractable>(false)
                                             .FirstOrDefault(i => string.Equals( i.controlReferenceName == "" ? i.interactableName : i.controlReferenceName, VRInteractName, StringComparison.CurrentCultureIgnoreCase));
                    if (interactable == null)
                        return;
                    
                    switch (VRInteractType)
                    {
                        case VRInteractType.FixedValue:
                            if (GetButtonPressed(joystickValue))
                                Interactions.Interact(interactable, Value);
                            else
                                Interactions.AntiInteract(interactable);
                            break;
                        case VRInteractType.Default:
                            if (GetButtonPressed(joystickValue))
                                Interactions.Interact(interactable);
                            else
                                Interactions.AntiInteract(interactable);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (OutputAction == ControllerAction.MFDInteract || OutputAction == ControllerAction.MMFDInteract)
                {

                    var throttleSoiSwitcher = GameObject.FindObjectOfType<ThrottleSOISwitcher>();
                    if (throttleSoiSwitcher == null)
                        return;

                    var mfdManager = throttleSoiSwitcher.mfdManager;
                    if (mfdManager == null)
                        return;

                    
                    if (OutputAction == ControllerAction.MMFDInteract)
                    {
                        mfdManager = GameObject.FindObjectsOfType<MFDManager>().FirstOrDefault(o => o != mfdManager);
                        if (mfdManager == null)
                            return;
                    }

                    if (Value >= mfdManager.mfds.Count)
                        return;
                    
                    var mfd = mfdManager.mfds[Value];

                    if (OutputAction == ControllerAction.MFDInteract)
                    {
                        switch (MFDAction)
                        {
                            case MFDAction.TogglePower:
                                if (GetButtonPressed(joystickValue))
                                {
                                    mfd.powerKnob.GetComponent<VRInteractable>().activeController?.ReleaseFromInteractable();
                                    mfd.powerKnob.RemoteSetState(mfd.powerOn ? 0 : 1);
                                }
                                else
                                    Interactions.AntiInteract(mfd.powerKnob.GetComponent<VRInteractable>());
                                break;
                            case MFDAction.TurnOn:
                                if (GetButtonPressed(joystickValue))
                                {
                                    mfd.powerKnob.GetComponent<VRInteractable>().activeController?.ReleaseFromInteractable();
                                    mfd.powerKnob.RemoteSetState(1);
                                }
                                else
                                    Interactions.AntiInteract(mfd.powerKnob.GetComponent<VRInteractable>());
                                break;
                            case MFDAction.TurnOff:
                                if (GetButtonPressed(joystickValue))
                                {
                                    mfd.powerKnob.GetComponent<VRInteractable>().activeController?.ReleaseFromInteractable();
                                    mfd.powerKnob.RemoteSetState(0);
                                }
                                else
                                    Interactions.AntiInteract(mfd.powerKnob.GetComponent<VRInteractable>());
                                break;
                            default:
                                var mfdButton = (MFD.MFDButtons)MFDAction;
                                var buttonCompsField = mfd.GetType()
                                                          .GetField("buttonComps",
                                                                    System.Reflection.BindingFlags.NonPublic
                                                                  | System.Reflection.BindingFlags.Instance);
                                var buttonComps = (Dictionary<MFD.MFDButtons, MFD.MFDButtonComp>)buttonCompsField.GetValue(mfd);
                                if (!buttonComps.TryGetValue(mfdButton, out var comp))
                                    return;
                                
                                if (GetButtonPressed(joystickValue))
                                    Interactions.Interact(comp.interactable);
                                else
                                    Interactions.AntiInteract(comp.interactable);
                                break;
                        }
                    }
                    else
                    {
                        var powerButton = mfd.transform.gameObject.GetComponentsInChildren<VRInteractable>().FirstOrDefault(o => o.interactableName.Contains("Power"));
                        if (powerButton == null)
                            return;
                        
                        switch (MFDAction)
                        {
                            case MFDAction.TogglePower:
                                if (GetButtonPressed(joystickValue))
                                    Interactions.Interact(powerButton);
                                else
                                    Interactions.AntiInteract(powerButton);
                                break;
                            case MFDAction.TurnOn:
                                if (GetButtonPressed(joystickValue) && !mfd.powerOn)
                                    Interactions.Interact(powerButton);
                                else
                                    Interactions.AntiInteract(powerButton);
                                break;
                            case MFDAction.TurnOff:
                                if (GetButtonPressed(joystickValue) && mfd.powerOn)
                                    Interactions.Interact(powerButton);
                                else
                                    Interactions.AntiInteract(powerButton);
                                break;
                        }
                    }
                }
                else if (OutputAction == ControllerAction.AutopilotHeadingLeft)
                {
                    ControllerActions.Autopilot.SetHeading(this, joystickValue, -1);
                }
                else if (OutputAction == ControllerAction.AutopilotHeadingRight)
                {
                     ControllerActions.Autopilot.SetHeading(this, joystickValue, 1);
                }
                else if (OutputAction == ControllerAction.AutopilotAltitudeUp)
                {
                    ControllerActions.Autopilot.SetAltitude(this, joystickValue, 1);
                }
                else if (OutputAction == ControllerAction.AutopilotAltitudeDown)
                {
                    ControllerActions.Autopilot.SetAltitude(this, joystickValue, -1);
                }
                else if (OutputAction == ControllerAction.AutopilotSpeedUp)
                {
                    ControllerActions.Autopilot.SetSpeed(this, joystickValue, 1);
                }
                else if (OutputAction == ControllerAction.AutopilotSpeedDown)
                {
                    ControllerActions.Autopilot.SetSpeed(this, joystickValue, -1);
                }
                else if (OutputAction == ControllerAction.AutopilotCourseLeft)
                {
                    ControllerActions.Autopilot.SetCourse(this, joystickValue, -1);
                }
                else if (OutputAction == ControllerAction.AutopilotCourseRight)
                { 
                    ControllerActions.Autopilot.SetCourse(this, joystickValue, 1);
                }
            }
        }

        public float GetAxisAsFloat(int value)
        {
            if (IsOffsetButton)
            {
                if (Invert)
                    return 1f - ((float)value / ButtonMax);
                return (float)value / ButtonMax;
            }
            if (IsOffsetPOV)
            {
                return (float)value / POVMax;
            }
            if (AxisCentering == AxisCentering.TwoAxis)
            {
                if (value > AxisMiddle && SelectedTwoAxis == TwoAxis.Positive)
                {
                    float val = 1f - Math.Abs((float)((float)(value - AxisMiddle) / AxisMiddle));
                    return Invert ? val : 1f - val;
                }
                else if (value < AxisMiddle && SelectedTwoAxis == TwoAxis.Negative)
                {
                    float val = Math.Abs((float)((float)value / AxisMiddle));
                    return Invert ? val : 1f - val;
                }
                else
                    return 0;

            }
            if (Invert)
                return 1f - ((float)value / AxisMax);
            return (float)value / AxisMax;
        }

        public bool GetButtonPressed(int value)
        {
            if (IsOffsetAxis)
            {
                if (AxisCentering == AxisCentering.Middle)
                    return value < AxisMiddle - Deadzone || value > AxisMiddle + Deadzone;
                else if (AxisCentering == AxisCentering.TwoAxis)
                {
                    return GetAxisAsFloat(value) >= 0.5f;
                }
                else // Normal Min-Max
                {
                    if (Invert) // Max-Min
                        return value < AxisMax - Deadzone;
                    else // Min-Max
                        return value > AxisMin + Deadzone;
                }
            }
            if (IsOffsetButton)
            {
                if (Invert)
                    return value < ButtonMax;
                else
                    return value >= ButtonMax;
            }
            if (IsOffsetPOV)
            {
                if (POVDirection == POVFacing.Up)
                    return value == (int)POVFacing.Up || value == (int)POVFacing.UpRight || value == (int)POVFacing.UpLeft;
                else if (POVDirection == POVFacing.Right)
                    return value == (int)POVFacing.Right || value == (int)POVFacing.DownRight || value == (int)POVFacing.UpRight;
                else if (POVDirection == POVFacing.Down)
                    return value == (int)POVFacing.Down || value == (int)POVFacing.DownLeft || value == (int)POVFacing.DownRight;
                else if (POVDirection == POVFacing.Left)
                    return value == (int)POVFacing.Left || value == (int)POVFacing.UpLeft || value == (int)POVFacing.DownLeft;
                else
                    return false;
            }
            return false;
        }

        public void HandleKeyboardKeys()
        {
            if (KeyboardKey.IsAxis)
            {
                int translatedValue = KeyboardKey.GetAxisTranslatedValue();

                RunAction(translatedValue);
            }
            else // Is Button
            {
                if (KeyboardKey.IsRepeatButton)
                {
                    bool pressed = UnityEngine.Input.GetKeyDown(KeyboardKey.PrimaryKey);
                    int translatedValue = pressed ? ButtonMax : ButtonMin;
                    RunAction(translatedValue);
                }
                else
                {
                    bool pressed = UnityEngine.Input.GetKeyDown(KeyboardKey.PrimaryKey);
                    bool released = UnityEngine.Input.GetKeyUp(KeyboardKey.PrimaryKey);
                    int translatedValue = pressed ? ButtonMax : ButtonMin;

                    if (pressed && !KeyboardKey.PrimaryKeyDown)
                    {
                        KeyboardKey.PrimaryKeyDown = true;
                        RunAction(translatedValue);
                    }
                    else if (released)
                    {
                        KeyboardKey.PrimaryKeyDown = false;
                        RunAction(translatedValue);
                    }
                }
            }
        }
    }

    public enum AxisCentering
    {
        Normal, // Minimum
        Middle,
        TwoAxis
    }

    public enum POVFacing : int
    {
        None = -1,
        Up = 0,
        UpRight = 4500,
        Right = 9000,
        DownRight = 13500,
        Down = 18000,
        DownLeft = 22500,
        Left = 27000,
        UpLeft = 31500,
    }

    public enum ThumbstickDirection
    {
        None,
        Up,
        Down,
        Left,
        Right,
        Press,
        XAxis,
        YAxis
    }

    public enum TwoAxis
    {
        Positive,
        Negative
    }
}