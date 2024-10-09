using System;
using System.Net;
using Elements.Core;
using FrooxEngine;
using Rug.Osc;

namespace VRCFTReceiver;

public class Driver : IInputDriver, IDisposable
{
  private InputInterface input;
  private Eyes eyes;
  private Mouth mouth;
  private OSCClient _OSCClient;
  private OSCQuery _OSCQuery;
  private IPAddress IP;
  private int ReceiverPort;
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
    DataTreeDictionary eyeDict = new();
    eyeDict.Add("Name", "VRCFaceTracking OSC");
    eyeDict.Add("Type", "Eye Tracking");
    eyeDict.Add("Model", "VRCFaceTracking OSC");
    list.Add(eyeDict);
    DataTreeDictionary mouthDict = new();
    mouthDict.Add("Name", "VRCFaceTracking OSC");
    mouthDict.Add("Type", "Lip Tracking");
    mouthDict.Add("Model", "VRCFaceTracking OSC");
    list.Add(mouthDict);
  }
  public void RegisterInputs(InputInterface inputInterface)
  {
    input = inputInterface;
    eyes = new Eyes(inputInterface, "VRCFaceTracking OSC", supportsPupilTracking: false);
    mouth = new Mouth(inputInterface, "VRCFaceTracking OSC", new MouthParameterGroup[16]
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
    OnSettingsChanged();
    VRCFTReceiver.config.OnThisConfigurationChanged += (_) => OnSettingsChanged();
    input.Engine.OnShutdown += Dispose;
    UniLog.Log("[VRCFTReceiver] Finished Initializing VRCFT driver");
  }
  private void OnSettingsChanged()
  {
    EnableEyeTracking = VRCFTReceiver.config.GetValue(VRCFTReceiver.ENABLE_EYE_TRACKING);
    EnableFaceTracking = VRCFTReceiver.config.GetValue(VRCFTReceiver.ENABLE_FACE_TRACKING);
    ReceiverPort = VRCFTReceiver.config.GetValue(VRCFTReceiver.KEY_RECEIVER_PORT);
    IP = IPAddress.Parse(VRCFTReceiver.config.GetValue(VRCFTReceiver.KEY_IP));
    EyesReversedY = VRCFTReceiver.config.GetValue(VRCFTReceiver.REVERSE_EYES_Y);
    EyesReversedX = VRCFTReceiver.config.GetValue(VRCFTReceiver.REVERSE_EYES_X);
    TrackingTimeout = VRCFTReceiver.config.GetValue(VRCFTReceiver.TRACKING_TIMEOUT_SECONDS);
    UniLog.Log($"[VRCFTReceiver] Starting VRCFTReceiver with these settings: EnableEyeTracking: {EnableEyeTracking}, EnableFaceTracking: {EnableFaceTracking},  ReceiverPort:{ReceiverPort}, IP: {IP}, EyesReversedY: {EyesReversedY}, EyesReversedX: {EyesReversedX}, TrackingTimeout: {TrackingTimeout}");
    InitializeOSCConnection();
  }
  private void InitializeOSCConnection()
  {
    UniLog.Log("[VRCFTReceiver] Initializing OSCConnection...");
    if (ReceiverPort != 0 && IP != null)
    {
      try
      {
        if (_OSCClient != null) _OSCClient.Teardown();
        _OSCClient = new OSCClient(IP, ReceiverPort);
        if (_OSCQuery != null) _OSCQuery.Teardown();
        _OSCQuery = new OSCQuery(ReceiverPort);
      }
      catch (Exception ex)
      {
        UniLog.Error("[VRCFTReceiver] Exception when starting OSCConnection:\n" + ex);
      }
    }
    else
    {
      UniLog.Warning("[VRCFTReceiver] OSCConnection not started because port or IP is not valid");
    }
  }
  private bool IsOSCConnectionActive()
  {
    if (_OSCClient != null || _OSCClient.receiver != null)
    {
      return false;
    }
    if (_OSCQuery != null || _OSCQuery.service != null)
    {
      return false;
    }

    return true;
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
      UniLog.Error($"[VRCFTReceiver] UpdateInputs Failed! Exception: {ex}");
    }
  }
  private void UpdateEyes(float deltaTime)
  {
    if (!IsTracking(OSCClient.LastEyeTracking) || !EnableEyeTracking)
    {
      eyes.IsEyeTrackingActive = false;
      eyes.SetTracking(state: false);
      return;
    }
    eyes.IsEyeTrackingActive = true;
    eyes.SetTracking(state: true);

    EyeLeft.SetDirectionFromXY(
      X: EyesReversedX ? -OSCClient.FTData[Expressions.EyeLeftX] : OSCClient.FTData[Expressions.EyeLeftX],
      Y: EyesReversedY ? -OSCClient.FTData[Expressions.EyeLeftY] : OSCClient.FTData[Expressions.EyeLeftY]
    );
    EyeRight.SetDirectionFromXY(
      X: EyesReversedX ? -OSCClient.FTData[Expressions.EyeRightX] : OSCClient.FTData[Expressions.EyeRightX],
      Y: EyesReversedY ? -OSCClient.FTData[Expressions.EyeRightY] : OSCClient.FTData[Expressions.EyeRightY]
    );

    UpdateEye(EyeLeft, eyes.LeftEye);
    UpdateEye(EyeRight, eyes.RightEye);
    UpdateEye(EyeCombined, eyes.CombinedEye);

    eyes.LeftEye.Openness = OSCClient.FTData[Expressions.EyeOpenLeft];
    eyes.RightEye.Openness = OSCClient.FTData[Expressions.EyeOpenRight];
    eyes.LeftEye.Widen = OSCClient.FTData[Expressions.EyeWideLeft];
    eyes.RightEye.Widen = OSCClient.FTData[Expressions.EyeWideRight];
    eyes.LeftEye.Squeeze = OSCClient.FTData[Expressions.EyeSquintLeft];
    eyes.RightEye.Squeeze = OSCClient.FTData[Expressions.EyeSquintRight];

    float leftBrowLowerer = OSCClient.FTData[Expressions.BrowPinchLeft] - OSCClient.FTData[Expressions.BrowLowererLeft];
    eyes.LeftEye.InnerBrowVertical = OSCClient.FTData[Expressions.BrowInnerUpLeft] - leftBrowLowerer;
    eyes.LeftEye.OuterBrowVertical = OSCClient.FTData[Expressions.BrowOuterUpLeft] - leftBrowLowerer;

    float rightBrowLowerer = OSCClient.FTData[Expressions.BrowPinchRight] - OSCClient.FTData[Expressions.BrowLowererRight];
    eyes.RightEye.InnerBrowVertical = OSCClient.FTData[Expressions.BrowInnerUpRight] - rightBrowLowerer;
    eyes.RightEye.OuterBrowVertical = OSCClient.FTData[Expressions.BrowOuterUpRight] - rightBrowLowerer;

    eyes.ComputeCombinedEyeParameters();
    eyes.FinishUpdate();
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

    mouth.IsTracking = true;
    mouth.MouthLeftSmileFrown = OSCClient.FTData[Expressions.MouthSmileLeft] - OSCClient.FTData[Expressions.MouthFrownLeft];
    mouth.MouthRightSmileFrown = OSCClient.FTData[Expressions.MouthSmileRight] - OSCClient.FTData[Expressions.MouthFrownRight];
    mouth.MouthLeftDimple = OSCClient.FTData[Expressions.MouthDimpleLeft];
    mouth.MouthRightDimple = OSCClient.FTData[Expressions.MouthDimpleRight];
    mouth.CheekLeftPuffSuck = OSCClient.FTData[Expressions.CheekPuffSuckLeft];
    mouth.CheekRightPuffSuck = OSCClient.FTData[Expressions.CheekPuffSuckRight];
    mouth.CheekLeftRaise = OSCClient.FTData[Expressions.CheekSquintLeft];
    mouth.CheekRightRaise = OSCClient.FTData[Expressions.CheekSquintRight];
    mouth.LipUpperLeftRaise = OSCClient.FTData[Expressions.MouthUpperUpLeft];
    mouth.LipUpperRightRaise = OSCClient.FTData[Expressions.MouthUpperUpRight];
    mouth.LipLowerLeftRaise = OSCClient.FTData[Expressions.MouthLowerDownLeft];
    mouth.LipLowerRightRaise = OSCClient.FTData[Expressions.MouthLowerDownRight];
    mouth.MouthPoutLeft = OSCClient.FTData[Expressions.LipPuckerLowerLeft] - OSCClient.FTData[Expressions.LipPuckerUpperLeft];
    mouth.MouthPoutRight = OSCClient.FTData[Expressions.LipPuckerLowerRight] - OSCClient.FTData[Expressions.LipPuckerUpperRight];
    mouth.LipUpperHorizontal = OSCClient.FTData[Expressions.MouthUpperX];
    mouth.LipLowerHorizontal = OSCClient.FTData[Expressions.MouthLowerX];
    mouth.LipTopLeftOverturn = OSCClient.FTData[Expressions.LipFunnelUpperLeft];
    mouth.LipTopRightOverturn = OSCClient.FTData[Expressions.LipFunnelUpperRight];
    mouth.LipBottomLeftOverturn = OSCClient.FTData[Expressions.LipFunnelLowerLeft];
    mouth.LipBottomRightOverturn = OSCClient.FTData[Expressions.LipFunnelLowerRight];
    mouth.LipTopLeftOverUnder = -OSCClient.FTData[Expressions.LipSuckUpperLeft];
    mouth.LipTopRightOverUnder = -OSCClient.FTData[Expressions.LipSuckUpperRight];
    mouth.LipBottomLeftOverUnder = -OSCClient.FTData[Expressions.LipSuckLowerLeft];
    mouth.LipBottomRightOverUnder = -OSCClient.FTData[Expressions.LipSuckLowerRight];
    mouth.LipLeftStretchTighten = OSCClient.FTData[Expressions.MouthStretchLeft] - OSCClient.FTData[Expressions.MouthTightenerLeft];
    mouth.LipRightStretchTighten = OSCClient.FTData[Expressions.MouthStretchRight] - OSCClient.FTData[Expressions.MouthTightenerRight];
    mouth.LipsLeftPress = OSCClient.FTData[Expressions.MouthPressLeft];
    mouth.LipsRightPress = OSCClient.FTData[Expressions.MouthPressRight];
    mouth.Jaw = new float3(
      OSCClient.FTData[Expressions.JawRight] - OSCClient.FTData[Expressions.JawLeft],
      -OSCClient.FTData[Expressions.MouthClosed],
      OSCClient.FTData[Expressions.JawForward]
    );
    mouth.JawOpen = MathX.Clamp01(OSCClient.FTData[Expressions.JawOpen] - OSCClient.FTData[Expressions.MouthClosed]);
    mouth.Tongue = new float3(
      OSCClient.FTData[Expressions.TongueX],
      OSCClient.FTData[Expressions.TongueY],
      OSCClient.FTData[Expressions.TongueOut]
    );
    mouth.TongueRoll = OSCClient.FTData[Expressions.TongueRoll];
    mouth.NoseWrinkleLeft = OSCClient.FTData[Expressions.NoseSneerLeft];
    mouth.NoseWrinkleRight = OSCClient.FTData[Expressions.NoseSneerRight];
    mouth.ChinRaiseBottom = OSCClient.FTData[Expressions.MouthRaiserLower];
    mouth.ChinRaiseTop = OSCClient.FTData[Expressions.MouthRaiserUpper];
  }
  public void Dispose()
  {
    UniLog.Log("[VRCFTReceiver] Driver disposal called");
    _OSCClient?.Teardown();
    _OSCQuery?.Teardown();
    UniLog.Log("[VRCFTReceiver] Driver disposed");
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