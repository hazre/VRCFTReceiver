using System;
using System.Collections.Generic;
using System.Net;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using HarmonyLib;
using ResoniteModLoader;
using VRC.OSCQuery;

namespace VRCFTReceiver;

public class Driver : IInputDriver, IDisposable
{
    private floatQ _lastValidCombined = floatQ.Identity;
    private OscClient _oscClient;
    private OscQuery _oscQuery;
    public VrcftEye EyeLeft;
    public VrcftEye EyeRight;
    private Eyes _eyes;
    private InputInterface _input;
    private Mouth _mouth;

    public VrcftEye EyeCombined => new VrcftEye
    {
        Eyelid = MathX.Max(EyeLeft.Eyelid, EyeRight.Eyelid),
        EyeRotation = CombinedEyesDir
    };

    public floatQ CombinedEyesDir
    {
        get
        {
            if (EyeLeft.IsValid && EyeRight.IsValid && EyeLeft.IsTracking && EyeRight.IsTracking)
            {
                _lastValidCombined = MathX.Slerp(EyeLeft.EyeRotation, EyeRight.EyeRotation, 0.5f);
            }
            else if (EyeLeft.IsValid && EyeLeft.IsTracking)
            {
                _lastValidCombined = EyeLeft.EyeRotation;
            }
            else if (EyeRight.IsValid && EyeRight.IsTracking)
            {
                _lastValidCombined = EyeRight.EyeRotation;
            }

            return _lastValidCombined;
        }
    }

    public void Dispose()
    {
        ResoniteMod.Debug("Driver disposal called");
        
        VrcftReceiver.KeyReceiverPort.OnChanged -= SettingsChanged;
        VrcftReceiver.KeyIp.OnChanged -= SettingsChanged;
        VrcftReceiver.AvatarName.OnChanged -= AvatarChange;
        
        _oscClient?.Dispose();
        _oscQuery?.Dispose();
        ResoniteMod.Debug("Driver disposed");
    }

    public int UpdateOrder => 100;

    public void CollectDeviceInfos(DataTreeList list)
    {
        DataTreeDictionary eyeDict = new DataTreeDictionary();
        eyeDict.Add("Name", "VRCFaceTracking OSC");
        eyeDict.Add("Type", "Eye Tracking");
        eyeDict.Add("Model", "VRCFaceTracking OSC");
        list.Add(eyeDict);
        DataTreeDictionary mouthDict = new DataTreeDictionary();
        mouthDict.Add("Name", "VRCFaceTracking OSC");
        mouthDict.Add("Type", "Lip Tracking");
        mouthDict.Add("Model", "VRCFaceTracking OSC");
        list.Add(mouthDict);
    }

    public void RegisterInputs(InputInterface inputInterface)
    {
        try
        {
            _input = inputInterface;
            _eyes = new Eyes(inputInterface, "VRCFaceTracking OSC", true);
            _mouth = new Mouth(inputInterface, "VRCFaceTracking OSC", new MouthParameterGroup[]
            {
                MouthParameterGroup.JawPose,
                MouthParameterGroup.JawOpen,
                MouthParameterGroup.TonguePose,
                MouthParameterGroup.LipRaise,
                MouthParameterGroup.LipHorizontal,
                MouthParameterGroup.SmileFrown,
                MouthParameterGroup.MouthDimple,
                MouthParameterGroup.MouthPout,
                MouthParameterGroup.LipOverturn,
                MouthParameterGroup.LipOverUnder,
                MouthParameterGroup.LipStretchTighten,
                MouthParameterGroup.LipsPress,
                MouthParameterGroup.CheekPuffSuck,
                MouthParameterGroup.CheekRaise,
                MouthParameterGroup.ChinRaise,
                MouthParameterGroup.NoseWrinkle
            });
            InitializeOscConnection();
            VrcftReceiver.KeyReceiverPort.OnChanged -= SettingsChanged;
            VrcftReceiver.KeyIp.OnChanged -= SettingsChanged;
            VrcftReceiver.AvatarName.OnChanged -= AvatarChange;

            VrcftReceiver.KeyReceiverPort.OnChanged += SettingsChanged;
            VrcftReceiver.KeyIp.OnChanged += SettingsChanged;
            VrcftReceiver.AvatarName.OnChanged += AvatarChange;

            ResoniteMod.Debug("Finished Initializing VRCFT driver");
        }
        catch (Exception ex)
        {
            ResoniteMod.Error($"Failed to register inputs: {ex}");
            throw;
        }
    }

    public void UpdateInputs(float deltaTime)
    {
        try
        {
            UpdateEyes(deltaTime);
            UpdateMouth(deltaTime);
        }
        catch (Exception ex)
        {
            ResoniteMod.Error($"UpdateInputs Failed! Exception: {ex}");
        }
    }

    private void SettingsChanged(object value) => InitializeOscConnection();

    private void InitializeOscConnection()
    {
        int port = VrcftReceiver.Config.GetValue(VrcftReceiver.KeyReceiverPort);
        IPAddress address = IPAddress.Parse(VrcftReceiver.Config.GetValue(VrcftReceiver.KeyIp));
        if (port != 0 && address != null)
        {
            try
            {
                _oscClient?.Dispose();
                _oscClient = new OscClient(address, port);
                _oscQuery?.Dispose();
                _oscQuery = new OscQuery(port);

                AvatarChange();
            }
            catch (Exception ex)
            {
                ResoniteMod.Error("Exception when starting OSCConnection:\n" + ex);
            }
        }
        else
        {
            ResoniteMod.Warn("OSCConnection not started because port or IP is not valid");
        }
    }

    private void UpdateEyes(float deltaTime)
    {
        if (!IsTracking(OscClient.LastEyeTracking) || !VrcftReceiver.Config.GetValue(VrcftReceiver.EnableEyeTracking))
        {
            _eyes.IsEyeTrackingActive = false;
            _eyes.SetTracking(false);
            return;
        }

        _eyes.IsEyeTrackingActive = true;
        _eyes.SetTracking(true);

        EyeLeft.SetDirectionFromXy(VrcftReceiver.Config.GetValue(VrcftReceiver.ReverseEyesX) ? -OscClient.FtData[Expressions.EyeLeftX] : OscClient.FtData[Expressions.EyeLeftX], VrcftReceiver.Config.GetValue(VrcftReceiver.ReverseEyesY) ? -OscClient.FtData[Expressions.EyeLeftY] : OscClient.FtData[Expressions.EyeLeftY]);
        EyeRight.SetDirectionFromXy(VrcftReceiver.Config.GetValue(VrcftReceiver.ReverseEyesX) ? -OscClient.FtData[Expressions.EyeRightX] : OscClient.FtData[Expressions.EyeRightX], VrcftReceiver.Config.GetValue(VrcftReceiver.ReverseEyesY) ? -OscClient.FtData[Expressions.EyeRightY] : OscClient.FtData[Expressions.EyeRightY]);

        UpdateEye(EyeLeft, _eyes.LeftEye);
        UpdateEye(EyeRight, _eyes.RightEye);
        UpdateEye(EyeCombined, _eyes.CombinedEye);

        _eyes.LeftEye.Openness = OscClient.FtData[Expressions.EyeOpenLeft];
        _eyes.RightEye.Openness = OscClient.FtData[Expressions.EyeOpenRight];
        _eyes.LeftEye.Widen = OscClient.FtData[Expressions.EyeWideLeft];
        _eyes.RightEye.Widen = OscClient.FtData[Expressions.EyeWideRight];
        _eyes.LeftEye.Squeeze = OscClient.FtData[Expressions.EyeSquintLeft];
        _eyes.RightEye.Squeeze = OscClient.FtData[Expressions.EyeSquintRight];

        float scale = VrcftReceiver.Config.GetValue(VrcftReceiver.PupilDilationScale);
        (float leftPupilDiameter, float rightPupilDiameter) = GetPupilDiameters();
        _eyes.LeftEye.PupilDiameter = leftPupilDiameter * scale;
        _eyes.RightEye.PupilDiameter = rightPupilDiameter * scale;

        float leftBrowLowerer = OscClient.FtData[Expressions.BrowPinchLeft] - OscClient.FtData[Expressions.BrowLowererLeft];
        _eyes.LeftEye.InnerBrowVertical = OscClient.FtData[Expressions.BrowInnerUpLeft] - leftBrowLowerer;
        _eyes.LeftEye.OuterBrowVertical = OscClient.FtData[Expressions.BrowOuterUpLeft] - leftBrowLowerer;

        float rightBrowLowerer = OscClient.FtData[Expressions.BrowPinchRight] - OscClient.FtData[Expressions.BrowLowererRight];
        _eyes.RightEye.InnerBrowVertical = OscClient.FtData[Expressions.BrowInnerUpRight] - rightBrowLowerer;
        _eyes.RightEye.OuterBrowVertical = OscClient.FtData[Expressions.BrowOuterUpRight] - rightBrowLowerer;

        _eyes.ComputeCombinedEyeParameters();
        _eyes.FinishUpdate();
    }

    private static (float left, float right) GetPupilDiameters()
    {
        float left = MathX.Max(0f, OscClient.FtData[Expressions.PupilDiameterLeft]);
        float right = MathX.Max(0f, OscClient.FtData[Expressions.PupilDiameterRight]);

        float combinedDiameter = MathX.Max(0f, OscClient.FtData[Expressions.PupilDiameter]);
        if (left <= 0f)
        {
            left = combinedDiameter;
        }

        if (right <= 0f)
        {
            right = combinedDiameter;
        }

        if (left > 0f && right > 0f)
        {
            return (left, right);
        }

        float dilation = MathX.Clamp01(OscClient.FtData[Expressions.PupilDilation]);
        if (dilation <= 0f)
        {
            return (left, right);
        }

        float estimatedDiameter = MathX.Lerp(2f, 8f, dilation);
        if (left <= 0f)
        {
            left = estimatedDiameter;
        }

        if (right <= 0f)
        {
            right = estimatedDiameter;
        }

        return (left, right);
    }

    public void UpdateEye(VrcftEye source, Eye dest)
    {
        if (source.IsValid)
        {
            dest.UpdateWithRotation(source.EyeRotation);
        }
    }

    private void UpdateMouth(float deltaTime)
    {
        if (!IsTracking(OscClient.LastFaceTracking) || !VrcftReceiver.Config.GetValue(VrcftReceiver.EnableFaceTracking))
        {
            _mouth.IsTracking = false;
            return;
        }

        _mouth.IsTracking = true;
        _mouth.MouthLeftSmileFrown = OscClient.FtData[Expressions.MouthSmileLeft] - OscClient.FtData[Expressions.MouthFrownLeft];
        _mouth.MouthRightSmileFrown = OscClient.FtData[Expressions.MouthSmileRight] - OscClient.FtData[Expressions.MouthFrownRight];
        _mouth.MouthLeftDimple = OscClient.FtData[Expressions.MouthDimpleLeft];
        _mouth.MouthRightDimple = OscClient.FtData[Expressions.MouthDimpleRight];
        _mouth.CheekLeftPuffSuck = OscClient.FtData[Expressions.CheekPuffSuckLeft];
        _mouth.CheekRightPuffSuck = OscClient.FtData[Expressions.CheekPuffSuckRight];
        _mouth.CheekLeftRaise = OscClient.FtData[Expressions.CheekSquintLeft];
        _mouth.CheekRightRaise = OscClient.FtData[Expressions.CheekSquintRight];
        _mouth.LipUpperLeftRaise = OscClient.FtData[Expressions.MouthUpperUpLeft];
        _mouth.LipUpperRightRaise = OscClient.FtData[Expressions.MouthUpperUpRight];
        _mouth.LipLowerLeftRaise = OscClient.FtData[Expressions.MouthLowerDownLeft];
        _mouth.LipLowerRightRaise = OscClient.FtData[Expressions.MouthLowerDownRight];
        _mouth.MouthPoutLeft = OscClient.FtData[Expressions.LipPuckerLowerLeft] - OscClient.FtData[Expressions.LipPuckerUpperLeft];
        _mouth.MouthPoutRight = OscClient.FtData[Expressions.LipPuckerLowerRight] - OscClient.FtData[Expressions.LipPuckerUpperRight];
        _mouth.LipUpperHorizontal = OscClient.FtData[Expressions.MouthUpperX];
        _mouth.LipLowerHorizontal = OscClient.FtData[Expressions.MouthLowerX];
        _mouth.LipTopLeftOverturn = OscClient.FtData[Expressions.LipFunnelUpperLeft];
        _mouth.LipTopRightOverturn = OscClient.FtData[Expressions.LipFunnelUpperRight];
        _mouth.LipBottomLeftOverturn = OscClient.FtData[Expressions.LipFunnelLowerLeft];
        _mouth.LipBottomRightOverturn = OscClient.FtData[Expressions.LipFunnelLowerRight];
        _mouth.LipTopLeftOverUnder = -OscClient.FtData[Expressions.LipSuckUpperLeft];
        _mouth.LipTopRightOverUnder = -OscClient.FtData[Expressions.LipSuckUpperRight];
        _mouth.LipBottomLeftOverUnder = -OscClient.FtData[Expressions.LipSuckLowerLeft];
        _mouth.LipBottomRightOverUnder = -OscClient.FtData[Expressions.LipSuckLowerRight];
        _mouth.LipLeftStretchTighten = OscClient.FtData[Expressions.MouthStretchLeft] - OscClient.FtData[Expressions.MouthTightenerLeft];
        _mouth.LipRightStretchTighten = OscClient.FtData[Expressions.MouthStretchRight] - OscClient.FtData[Expressions.MouthTightenerRight];
        _mouth.LipsLeftPress = OscClient.FtData[Expressions.MouthPressLeft];
        _mouth.LipsRightPress = OscClient.FtData[Expressions.MouthPressRight];
        _mouth.Jaw = new float3(OscClient.FtData[Expressions.JawRight] - OscClient.FtData[Expressions.JawLeft], -OscClient.FtData[Expressions.MouthClosed], OscClient.FtData[Expressions.JawForward]);
        _mouth.JawOpen = MathX.Clamp01(OscClient.FtData[Expressions.JawOpen] - OscClient.FtData[Expressions.MouthClosed]);
        _mouth.Tongue = new float3(OscClient.FtData[Expressions.TongueX], OscClient.FtData[Expressions.TongueY], OscClient.FtData[Expressions.TongueOut]);
        _mouth.TongueRoll = OscClient.FtData[Expressions.TongueRoll];
        _mouth.NoseWrinkleLeft = OscClient.FtData[Expressions.NoseSneerLeft];
        _mouth.NoseWrinkleRight = OscClient.FtData[Expressions.NoseSneerRight];
        _mouth.ChinRaiseBottom = OscClient.FtData[Expressions.MouthRaiserLower];
        _mouth.ChinRaiseTop = OscClient.FtData[Expressions.MouthRaiserUpper];
    }

    private static bool IsTracking(DateTime? timestamp)
    {
        if (VrcftReceiver.Config.GetValue(VrcftReceiver.TrackingTimeoutSeconds) == -1)
        {
            return true;
        }

        if (!timestamp.HasValue)
        {
            return false;
        }

        if ((DateTime.UtcNow - timestamp.Value).TotalSeconds > VrcftReceiver.Config.GetValue(VrcftReceiver.TrackingTimeoutSeconds))
        {
            return false;
        }

        return true;
    }

    public void AvatarChange(object value) => AvatarChange();

    public void AvatarChange()
    {
        foreach (OSCQueryServiceProfile profile in _oscQuery.Profiles)
        {
            if (profile.name.StartsWith("VRCFT"))
            {
                string avatar = VrcftReceiver.Config.GetValue(VrcftReceiver.AvatarName);
                OscClient.SendMessage(profile.address, profile.port, "/avatar/change", avatar);
                OscClient.SendMessage(profile.address, profile.port, "/avatar/name", avatar);
                OscClient.SendMessage(profile.address, profile.port, "/avatar/id", avatar);
            }
        }
    }
}