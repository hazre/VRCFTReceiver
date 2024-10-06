using System;
using System.Net;
using System.Threading;
using Elements.Core;
using FrooxEngine;
using Rug.Osc;

namespace VRCFTReceiver;

// based on FrooxEngine's Steam Link OSC implementation
public class VRCFT_Driver : IInputDriver, IDisposable
{
  private InputInterface input;
  private Eyes eyes;
  private Mouth mouth;
  private readonly object _lock = new();
  private OSCClient _OSCClient;
  private OSCQuery _OSCQuery;
  private bool EnableEyeTracking;
  private bool EnableFaceTracking;
  private static int TrackingTimeout;
  public bool EyesReversedY = false;
  public bool EyesReversedX = false;
  public VRCFTEye EyeLeft = new();
  public VRCFTEye EyeRight = new();
  public VRCFTEye EyeCombined => new()
  {
    Eyelid = MathX.Max(EyeLeft.Eyelid, EyeRight.Eyelid),
    EyeRotation = CombinedEyesDir
  };
  public floatQ CombinedEyesDir
  {
    get
    {
      if (EyeLeft.IsValid && EyeRight.IsValid && EyeLeft.IsTracking && EyeRight.IsTracking)
        _lastValidCombined = MathX.Slerp(EyeLeft.EyeRotation, EyeRight.EyeRotation, 0.5f);
      else if (EyeLeft.IsValid && EyeLeft.IsTracking)
        _lastValidCombined = EyeLeft.EyeRotation;
      else if (EyeRight.IsValid && EyeRight.IsTracking)
        _lastValidCombined = EyeRight.EyeRotation;

      return _lastValidCombined;
    }
  }
  private floatQ _lastValidCombined = floatQ.Identity;
  public int UpdateOrder => 100;
  public void CollectDeviceInfos(DataTreeList list)
  {
    DataTreeDictionary dataTreeDictionary = new DataTreeDictionary();
    dataTreeDictionary.Add("Name", "VRCFaceTracking OSC");
    dataTreeDictionary.Add("Type", "Eye Tracking");
    dataTreeDictionary.Add("Model", "VRCFaceTracking OSC");
    list.Add(dataTreeDictionary);
    dataTreeDictionary.Add("Name", "VRCFaceTracking OSC");
    dataTreeDictionary.Add("Type", "Lip Tracking");
    dataTreeDictionary.Add("Model", "VRCFaceTracking OSC");
    list.Add(dataTreeDictionary);
  }
  public void RegisterInputs(InputInterface inputInterface)
  {
    input = inputInterface;
    eyes = new Eyes(inputInterface, "VRCFaceTracking OSC", supportsPupilTracking: false);
    mouth = new Mouth(inputInterface, "VRCFaceTracking OSC",
    [
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
    ]);
    OnSettingsChanged();
    VRCFTReceiver.config.OnThisConfigurationChanged += (_) => OnSettingsChanged();
    input.Engine.OnShutdown += Dispose;
  }
  private void OnSettingsChanged()
  {
    EnableEyeTracking = VRCFTReceiver.config.GetValue(VRCFTReceiver.ENABLE_EYE_TRACKING);
    EnableFaceTracking = VRCFTReceiver.config.GetValue(VRCFTReceiver.ENABLE_FACE_TRACKING);
    int receiverPort = VRCFTReceiver.config.GetValue(VRCFTReceiver.KEY_RECEIVER_PORT);
    IPAddress ip = IPAddress.Parse(VRCFTReceiver.config.GetValue(VRCFTReceiver.KEY_IP));
    EyesReversedY = VRCFTReceiver.config.GetValue(VRCFTReceiver.REVERSE_EYES_Y);
    EyesReversedX = VRCFTReceiver.config.GetValue(VRCFTReceiver.REVERSE_EYES_X);
    TrackingTimeout = VRCFTReceiver.config.GetValue(VRCFTReceiver.TRACKING_TIMEOUT_SECONDS);
    if (receiverPort != 0 && ip != null)
    {
      try
      {
        if (_OSCClient != null) _OSCClient.Teardown();
        _OSCClient = new OSCClient(ip, receiverPort);
        if (_OSCQuery != null) _OSCQuery.Teardown();
        _OSCQuery = new OSCQuery(receiverPort);
      }
      catch (Exception ex)
      {
        UniLog.Error("Exception when starting OSCClient:\n" + ex);
      }
    }
  }
  public void UpdateInputs(float deltaTime)
  {
    try
    {
      UpdateEyes(deltaTime);
    }
    catch (Exception ex)
    {
      UniLog.Error($"Failed to UpdateEyes! Exception: {ex}");
    }

    try
    {
      UpdateMouth(deltaTime);
    }
    catch (Exception ex)
    {
      UniLog.Error($"Failed to UpdateMouth! Exception: {ex}");
    }
  }
  private void UpdateEyes(float deltaTime)
  {
    if (!IsTracking(OSCClient.LastEyeTracking) || !EnableEyeTracking)
    {
      eyes.IsEyeTrackingActive = false;
      eyes.SetTracking(false);
      return;
    }

    lock (_lock)
    {
      eyes.IsEyeTrackingActive = true;
      eyes.SetTracking(true);

      EyeLeft.SetDirectionFromXY(
        X: EyesReversedX ? -OSCClient.FTDataWithAddress[Expressions.EyeLeftX] : OSCClient.FTDataWithAddress[Expressions.EyeLeftX],
        Y: EyesReversedY ? -OSCClient.FTDataWithAddress[Expressions.EyeLeftY] : OSCClient.FTDataWithAddress[Expressions.EyeLeftY]
      );
      EyeRight.SetDirectionFromXY(
        X: EyesReversedX ? -OSCClient.FTDataWithAddress[Expressions.EyeRightX] : OSCClient.FTDataWithAddress[Expressions.EyeRightX],
        Y: EyesReversedY ? -OSCClient.FTDataWithAddress[Expressions.EyeRightY] : OSCClient.FTDataWithAddress[Expressions.EyeRightY]
      );

      UpdateEye(EyeLeft, eyes.LeftEye);
      UpdateEye(EyeRight, eyes.RightEye);
      UpdateEye(EyeCombined, eyes.CombinedEye);

      eyes.LeftEye.Openness = OSCClient.FTDataWithAddress[Expressions.EyeOpenLeft];
      eyes.RightEye.Openness = OSCClient.FTDataWithAddress[Expressions.EyeOpenRight];
      eyes.LeftEye.Widen = OSCClient.FTDataWithAddress[Expressions.EyeWideLeft];
      eyes.RightEye.Widen = OSCClient.FTDataWithAddress[Expressions.EyeWideRight];
      eyes.LeftEye.Squeeze = OSCClient.FTDataWithAddress[Expressions.EyeSquintLeft];
      eyes.RightEye.Squeeze = OSCClient.FTDataWithAddress[Expressions.EyeSquintRight];

      float leftBrowLowerer = OSCClient.FTDataWithAddress[Expressions.BrowPinchLeft] - OSCClient.FTDataWithAddress[Expressions.BrowLowererLeft];
      eyes.LeftEye.InnerBrowVertical = OSCClient.FTDataWithAddress[Expressions.BrowInnerUpLeft] - leftBrowLowerer;
      eyes.LeftEye.OuterBrowVertical = OSCClient.FTDataWithAddress[Expressions.BrowOuterUpLeft] - leftBrowLowerer;

      float rightBrowLowerer = OSCClient.FTDataWithAddress[Expressions.BrowPinchRight] - OSCClient.FTDataWithAddress[Expressions.BrowLowererRight];
      eyes.RightEye.InnerBrowVertical = OSCClient.FTDataWithAddress[Expressions.BrowInnerUpRight] - rightBrowLowerer;
      eyes.RightEye.OuterBrowVertical = OSCClient.FTDataWithAddress[Expressions.BrowOuterUpRight] - rightBrowLowerer;

      eyes.ComputeCombinedEyeParameters();
      eyes.FinishUpdate();
    }
  }
  public void UpdateEye(VRCFTEye source, Eye dest)
  {
    if (source.IsValid)
    {
      dest.UpdateWithRotation(source.EyeRotation);
    }
  }
  private void UpdateMouth(float deltaTime)
  {
    if (!IsTracking(OSCClient.LastFaceTracking) || !EnableFaceTracking)
    {
      mouth.IsTracking = false;
      return;
    }

    lock (_lock)
    {
      mouth.IsTracking = true;
      mouth.MouthLeftSmileFrown = OSCClient.FTDataWithAddress[Expressions.MouthSmileLeft] - OSCClient.FTDataWithAddress[Expressions.MouthFrownLeft];
      mouth.MouthRightSmileFrown = OSCClient.FTDataWithAddress[Expressions.MouthSmileRight] - OSCClient.FTDataWithAddress[Expressions.MouthFrownRight];
      mouth.MouthLeftDimple = OSCClient.FTDataWithAddress[Expressions.MouthDimpleLeft];
      mouth.MouthRightDimple = OSCClient.FTDataWithAddress[Expressions.MouthDimpleRight];
      mouth.CheekLeftPuffSuck = OSCClient.FTDataWithAddress[Expressions.CheekPuffSuckLeft];
      mouth.CheekRightPuffSuck = OSCClient.FTDataWithAddress[Expressions.CheekPuffSuckRight];
      mouth.CheekLeftRaise = OSCClient.FTDataWithAddress[Expressions.CheekSquintLeft];
      mouth.CheekRightRaise = OSCClient.FTDataWithAddress[Expressions.CheekSquintRight];
      mouth.LipUpperLeftRaise = OSCClient.FTDataWithAddress[Expressions.MouthUpperUpLeft];
      mouth.LipUpperRightRaise = OSCClient.FTDataWithAddress[Expressions.MouthUpperUpRight];
      mouth.LipLowerLeftRaise = OSCClient.FTDataWithAddress[Expressions.MouthLowerDownLeft];
      mouth.LipLowerRightRaise = OSCClient.FTDataWithAddress[Expressions.MouthLowerDownRight];
      mouth.MouthPoutLeft = OSCClient.FTDataWithAddress[Expressions.LipPuckerLowerLeft] - OSCClient.FTDataWithAddress[Expressions.LipPuckerUpperLeft];
      mouth.MouthPoutRight = OSCClient.FTDataWithAddress[Expressions.LipPuckerLowerRight] - OSCClient.FTDataWithAddress[Expressions.LipPuckerUpperRight];
      mouth.LipUpperHorizontal = OSCClient.FTDataWithAddress[Expressions.MouthUpperX];
      mouth.LipLowerHorizontal = OSCClient.FTDataWithAddress[Expressions.MouthLowerX];
      mouth.LipTopLeftOverturn = OSCClient.FTDataWithAddress[Expressions.LipFunnelUpperLeft];
      mouth.LipTopRightOverturn = OSCClient.FTDataWithAddress[Expressions.LipFunnelUpperRight];
      mouth.LipBottomLeftOverturn = OSCClient.FTDataWithAddress[Expressions.LipFunnelLowerLeft];
      mouth.LipBottomRightOverturn = OSCClient.FTDataWithAddress[Expressions.LipFunnelLowerRight];
      mouth.LipTopLeftOverUnder = -OSCClient.FTDataWithAddress[Expressions.LipSuckUpperLeft];
      mouth.LipTopRightOverUnder = -OSCClient.FTDataWithAddress[Expressions.LipSuckUpperRight];
      mouth.LipBottomLeftOverUnder = -OSCClient.FTDataWithAddress[Expressions.LipSuckLowerLeft];
      mouth.LipBottomRightOverUnder = -OSCClient.FTDataWithAddress[Expressions.LipSuckLowerRight];
      mouth.LipLeftStretchTighten = OSCClient.FTDataWithAddress[Expressions.MouthStretchLeft] - OSCClient.FTDataWithAddress[Expressions.MouthTightenerLeft];
      mouth.LipRightStretchTighten = OSCClient.FTDataWithAddress[Expressions.MouthStretchRight] - OSCClient.FTDataWithAddress[Expressions.MouthTightenerRight];
      mouth.LipsLeftPress = OSCClient.FTDataWithAddress[Expressions.MouthPressLeft];
      mouth.LipsRightPress = OSCClient.FTDataWithAddress[Expressions.MouthPressRight];
      mouth.Jaw = new float3(
        OSCClient.FTDataWithAddress[Expressions.JawRight] - OSCClient.FTDataWithAddress[Expressions.JawLeft],
        -OSCClient.FTDataWithAddress[Expressions.MouthClosed],
        OSCClient.FTDataWithAddress[Expressions.JawForward]
      );
      mouth.JawOpen = MathX.Clamp01(OSCClient.FTDataWithAddress[Expressions.JawOpen] - OSCClient.FTDataWithAddress[Expressions.MouthClosed]);
      mouth.Tongue = new float3(
        OSCClient.FTDataWithAddress[Expressions.TongueX],
        OSCClient.FTDataWithAddress[Expressions.TongueY],
        OSCClient.FTDataWithAddress[Expressions.TongueOut]
      );
      mouth.TongueRoll = OSCClient.FTDataWithAddress[Expressions.TongueRoll];
      mouth.NoseWrinkleLeft = OSCClient.FTDataWithAddress[Expressions.NoseSneerLeft];
      mouth.NoseWrinkleRight = OSCClient.FTDataWithAddress[Expressions.NoseSneerRight];
      mouth.ChinRaiseBottom = OSCClient.FTDataWithAddress[Expressions.MouthRaiserLower];
      mouth.ChinRaiseTop = OSCClient.FTDataWithAddress[Expressions.MouthRaiserUpper];
    }
  }
  public void Dispose()
  {
    _OSCClient?.Teardown();
    _OSCQuery?.Teardown();
  }
  private static bool IsTracking(DateTime? timestamp)
  {
    if (!timestamp.HasValue)
    {
      return false;
    }
    if ((DateTime.UtcNow - timestamp.Value).TotalSeconds > TrackingTimeout)
    {
      return false;
    }
    return true;
  }
  public void AvatarChange()
  {
    foreach (var profile in _OSCQuery.profiles)
    {
      if (profile.name.StartsWith("VRCFT"))
      {
        OSCClient.SendMessage(profile.address, profile.port, "/avatar/change", "default");
      }
    }
  }
}