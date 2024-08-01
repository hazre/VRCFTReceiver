using System;
using System.Net;
using System.Threading;
using Elements.Core;
using FrooxEngine;
using Rug.Osc;

namespace VRCFTReceiver;

// heavily based on FrooxEngine's Steam Link OSC implementation
public class VRCFT_Driver : IInputDriver, IDisposable
{
  private bool disposed;

  private InputInterface input;

  private Eyes eyes;

  private Mouth mouth;

  private Thread thread;

  private readonly object _lock = new();
  private OscReceiver oscReceiver;
  private OscSender oscSender;

  private bool EnableEyeTracking;
  private bool EnableFaceTracking;

  private DateTime? lastEyeTracking;

  private DateTime? lastFaceTracking;

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

  private float _EyeOpenLeft;

  private float _EyeOpenRight;

  private float _EyeWideLeft;

  private float _EyeWideRight;

  private float _BrowLowererLeft;

  private float _BrowPinchLeft;

  private float _BrowLowererRight;

  private float _BrowPinchRight;

  private float _BrowInnerUpLeft;

  private float _BrowInnerUpRight;

  private float _BrowOuterUpLeft;

  private float _BrowOuterUpRight;

  private float _EyeSquintLeft;

  private float _EyeSquintRight;

  private float _MouthSmileLeft;

  private float _MouthSmileRight;

  private float _MouthFrownLeft;

  private float _MouthFrownRight;

  private float _MouthDimpleLeft;

  private float _MouthDimpleRight;

  private float _MouthLowerDownLeft;

  private float _MouthLowerDownRight;

  private float _MouthUpperUpLeft;

  private float _MouthUpperUpRight;

  private float _LipPuckerLowerLeft;

  private float _LipPuckerUpperLeft;

  private float _LipPuckerLowerRight;

  private float _LipPuckerUpperRight;

  private float _CheekPuffSuckLeft;

  private float _CheekPuffSuckRight;

  private float _CheekSquintLeft;

  private float _CheekSquintRight;

  private float _LipFunnelLowerLeft;

  private float _LipFunnelLowerRight;

  private float _LipFunnelUpperLeft;

  private float _LipFunnelUpperRight;

  private float _JawForward;

  private float _JawOpen;

  private float _JawLeft;

  private float _JawRight;

  private float _MouthClosed;

  private float _TongueOut;

  private float _TongueRoll;

  private float _TongueX;

  private float _TongueY;

  private float _MouthUpperX;

  private float _MouthLowerX;

  private float _NoseSneerLeft;

  private float _NoseSneerRight;

  private float _LipSuckLowerLeft;

  private float _LipSuckLowerRight;

  private float _LipSuckUpperLeft;

  private float _LipSuckUpperRight;

  private float _MouthTightenerLeft;

  private float _MouthTightenerRight;

  private float _MouthStretchLeft;

  private float _MouthStretchRight;

  private float _MouthPressLeft;

  private float _MouthPressRight;

  private float _MouthRaiserLower;

  private float _MouthRaiserUpper;

  public int UpdateOrder => 100;

  public void CollectDeviceInfos(DataTreeList list)
  {
    DataTreeDictionary dataTreeDictionary = new DataTreeDictionary();
    dataTreeDictionary.Add("Name", "VRCFaceTracking OSC");
    dataTreeDictionary.Add("Type", "Eye Tracking");
    dataTreeDictionary.Add("Model", "VRCFaceTracking OSC");
    list.Add(dataTreeDictionary);
    dataTreeDictionary = new DataTreeDictionary();
    dataTreeDictionary.Add("Name", "VRCFaceTracking OSC");
    dataTreeDictionary.Add("Type", "Lip Tracking");
    dataTreeDictionary.Add("Model", "VRCFaceTracking OSC");
    list.Add(dataTreeDictionary);
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
    Loader.Msg("Starting OSC processing thread");
    thread = new Thread(ReceiveTrackingData);
    thread.Start();
    OnSettingsChanged();
    Loader.config.OnThisConfigurationChanged += (_) => OnSettingsChanged();
    input.Engine.OnShutdown += Dispose;
  }

  private void OnSettingsChanged()
  {
    EnableEyeTracking = Loader.config.GetValue(Loader.ENABLE_EYE_TRACKING);
    Loader.Msg("Enable Eye Tracking: " + EnableEyeTracking);
    EnableFaceTracking = Loader.config.GetValue(Loader.ENABLE_FACE_TRACKING);
    Loader.Msg("Enable Face Tracking: " + EnableFaceTracking);
    int receiverPort = Loader.config.GetValue(Loader.KEY_RECEIVER_PORT);
    Loader.Msg("Receiver Port: " + receiverPort);
    int senderPort = Loader.config.GetValue(Loader.KEY_SENDER_PORT);
    Loader.Msg("Sender Port: " + senderPort);
    IPAddress ip = IPAddress.Parse(Loader.config.GetValue(Loader.KEY_IP));
    Loader.Msg("IP Address: " + ip);
    EyesReversedY = Loader.config.GetValue(Loader.REVERSE_EYES_Y);
    Loader.Msg("Eyes Reversed Y: " + EyesReversedY);
    EyesReversedX = Loader.config.GetValue(Loader.REVERSE_EYES_X);
    Loader.Msg("Eyes Reversed X: " + EyesReversedX);
    OscReceiver currentOscReceiver = this.oscReceiver;
    OscSender currentOscSender = this.oscSender;
    if ((currentOscReceiver == null || currentOscReceiver.Port != receiverPort || currentOscReceiver.LocalAddress != ip) && receiverPort != 0 && ip != null)
    {
      try
      {
        Loader.Msg($"Starting VRCFaceTracking OSC listener on on port {receiverPort}");
        OscReceiver oscReceiver = new OscReceiver(ip, receiverPort);
        oscReceiver.Connect();
        OscReceiver previousOscReceiver = this.oscReceiver;
        this.oscReceiver = oscReceiver;
        previousOscReceiver?.Close();
      }
      catch (Exception ex)
      {
        Loader.Error("Exception when starting VRCFaceTracking OSC receiver:\n" + ex);
      }
    }

    if ((currentOscSender == null || currentOscSender.Port != senderPort || currentOscSender.RemoteAddress != ip) && senderPort != 0 && ip != null)
    {
      try
      {
        Loader.Msg($"Starting VRCFaceTracking OSC sender on on port {senderPort}");
        OscSender oscSender = new OscSender(ip, senderPort);
        oscSender.Connect();
        OscSender previousOscSender = this.oscSender;
        this.oscSender = oscSender;
        previousOscSender?.Close();
      }
      catch (Exception ex)
      {
        Loader.Error("Exception when starting VRCFaceTracking OSC sender:\n" + ex);
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
      Loader.Error($"Failed to UpdateEyes! Exception: {ex}");
    }

    try
    {
      UpdateMouth(deltaTime);
    }
    catch (Exception ex)
    {
      Loader.Error($"Failed to UpdateMouth! Exception: {ex}");
    }
  }

  private void UpdateEyes(float deltaTime)
  {
    if (!EnableEyeTracking)
    {
      eyes.IsEyeTrackingActive = false;
      eyes.SetTracking(false);
      return;
    }

    lock (_lock)
    {
      // eyes.IsEyeTrackingActive = input.VR_Active;
      eyes.IsEyeTrackingActive = true;
      eyes.SetTracking(true);

      UpdateEye(EyeLeft, eyes.LeftEye);
      UpdateEye(EyeRight, eyes.RightEye);
      UpdateEye(EyeCombined, eyes.CombinedEye);

      eyes.LeftEye.Openness = _EyeOpenLeft;
      eyes.RightEye.Openness = _EyeOpenRight;
      eyes.LeftEye.Widen = _EyeWideLeft;
      eyes.RightEye.Widen = _EyeWideRight;
      eyes.LeftEye.Squeeze = _EyeSquintLeft;
      eyes.RightEye.Squeeze = _EyeSquintRight;
      float _leftBrowLowerer = _BrowPinchLeft - _BrowLowererLeft;
      eyes.LeftEye.InnerBrowVertical = _BrowInnerUpLeft - _leftBrowLowerer;
      eyes.LeftEye.OuterBrowVertical = _BrowOuterUpLeft - _leftBrowLowerer;
      float _rightBrowLowerer = _BrowPinchRight - _BrowLowererRight;
      eyes.RightEye.InnerBrowVertical = _BrowInnerUpRight - _rightBrowLowerer;
      eyes.RightEye.OuterBrowVertical = _BrowOuterUpRight - _rightBrowLowerer;
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
    if (!EnableFaceTracking)
    {
      mouth.IsTracking = false;
      return;
    }

    lock (_lock)
    {
      mouth.IsTracking = true;
      mouth.MouthLeftSmileFrown = _MouthSmileLeft - _MouthFrownLeft;
      mouth.MouthRightSmileFrown = _MouthSmileRight - _MouthFrownRight;
      mouth.MouthLeftDimple = _MouthDimpleLeft;
      mouth.MouthRightDimple = _MouthDimpleRight;
      mouth.CheekLeftPuffSuck = _CheekPuffSuckLeft;
      mouth.CheekRightPuffSuck = _CheekPuffSuckRight;
      mouth.CheekLeftRaise = _CheekSquintLeft;
      mouth.CheekRightRaise = _CheekSquintRight;
      mouth.LipUpperLeftRaise = _MouthUpperUpLeft;
      mouth.LipUpperRightRaise = _MouthUpperUpRight;
      mouth.LipLowerLeftRaise = _MouthLowerDownLeft;
      mouth.LipLowerRightRaise = _MouthLowerDownRight;
      mouth.MouthPoutLeft = _LipPuckerLowerLeft - _LipPuckerUpperLeft;
      mouth.MouthPoutRight = _LipPuckerLowerRight - _LipPuckerUpperRight;
      mouth.LipUpperHorizontal = _MouthUpperX;
      mouth.LipLowerHorizontal = _MouthLowerX;
      mouth.LipTopLeftOverturn = _LipFunnelUpperLeft;
      mouth.LipTopRightOverturn = _LipFunnelUpperRight;
      mouth.LipBottomLeftOverturn = _LipFunnelLowerLeft;
      mouth.LipBottomRightOverturn = _LipFunnelLowerRight;
      mouth.LipTopLeftOverUnder = 0f - _LipSuckUpperLeft;
      mouth.LipTopRightOverUnder = 0f - _LipSuckUpperRight;
      mouth.LipBottomLeftOverUnder = 0f - _LipSuckLowerLeft;
      mouth.LipBottomRightOverUnder = 0f - _LipSuckLowerRight;
      mouth.LipLeftStretchTighten = _MouthStretchLeft - _MouthTightenerLeft;
      mouth.LipRightStretchTighten = _MouthStretchRight - _MouthTightenerRight;
      mouth.LipsLeftPress = _MouthPressLeft;
      mouth.LipsRightPress = _MouthPressRight;
      // I don't know what's happening here, let's just trust froox.
      mouth.Jaw = new float3(_JawRight - _JawLeft, 0f - _MouthClosed, _JawForward);
      mouth.JawOpen = MathX.Clamp01(_JawOpen - _MouthClosed);
      // removed _tongueRetreat, it's not part of UE
      mouth.Tongue = new float3(_TongueX, _TongueY, _TongueOut);
      mouth.TongueRoll = _TongueRoll;
      mouth.NoseWrinkleLeft = _NoseSneerLeft;
      mouth.NoseWrinkleRight = _NoseSneerRight;
      mouth.ChinRaiseBottom = _MouthRaiserLower;
      mouth.ChinRaiseTop = _MouthRaiserUpper;
      // Loader.Msg($"Updated Mouth parameters");
    }
  }

  private void UpdateData(OscMessage message)
  {
    string address = message.Address;
    if (address == null)
    {
      return;
    }

    if (message[0] is not float)
    {
      // Loader.Msg("OscMessage is not a float, skipping " + message);
      return;
    }
    switch (address[22])
    {
      case 'B':
        switch (address)
        {
          case "/avatar/parameters/v2/BrowInnerUpLeft":
            _BrowInnerUpLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/BrowInnerUpRight":
            _BrowInnerUpRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/BrowLowererLeft":
            _BrowLowererLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/BrowLowererRight":
            _BrowLowererRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/BrowOuterUpLeft":
            _BrowOuterUpLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/BrowOuterUpRight":
            _BrowOuterUpRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/BrowPinchLeft":
            _BrowPinchLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/BrowPinchRight":
            _BrowPinchRight = ReadFloat(message);
            break;
        }
        break;
      case 'C':
        switch (address)
        {
          case "/avatar/parameters/v2/CheekPuffSuckLeft":
            _CheekPuffSuckLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/CheekPuffSuckRight":
            _CheekPuffSuckRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/CheekSquintLeft":
            _CheekSquintLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/CheekSquintRight":
            _CheekSquintRight = ReadFloat(message);
            break;
        }
        break;
      case 'D':
        switch (address)
        {
          case "/avatar/parameters/v2/MouthDimpleLeft":
            _MouthDimpleLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthDimpleRight":
            _MouthDimpleRight = ReadFloat(message);
            break;
        }
        break;
      case 'E':
        lastEyeTracking = DateTime.Now;
        switch (address)
        {
          case "/avatar/parameters/v2/EyeLeftX":
            {
              float value = ReadFloat(message);
              EyeLeft.SetDirectionFromXY(X: EyesReversedX ? -value : value);
            }
            break;
          case "/avatar/parameters/v2/EyeLeftY":
            {
              float value = ReadFloat(message);
              EyeLeft.SetDirectionFromXY(Y: EyesReversedY ? value : -value);
            }
            break;
          case "/avatar/parameters/v2/EyeRightX":
            {
              float value = ReadFloat(message);
              EyeRight.SetDirectionFromXY(X: EyesReversedX ? -value : value);
            }
            break;
          case "/avatar/parameters/v2/EyeRightY":
            {
              float value = ReadFloat(message);
              EyeRight.SetDirectionFromXY(Y: EyesReversedY ? value : -value);
            }
            break;
          case "/avatar/parameters/v2/EyeOpenLeft":
            _EyeOpenLeft = ReadFloat(message);
            EyeLeft.Eyelid = _EyeOpenLeft;
            break;
          case "/avatar/parameters/v2/EyeOpenRight":
            _EyeOpenRight = ReadFloat(message);
            EyeRight.Eyelid = _EyeOpenRight;
            break;
          case "/avatar/parameters/v2/EyeSquintLeft":
            _EyeSquintLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/EyeSquintRight":
            _EyeSquintRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/EyeWideLeft":
            _EyeWideLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/EyeWideRight":
            _EyeWideRight = ReadFloat(message);
            break;
        }
        break;
      case 'J':
        switch (address)
        {
          case "/avatar/parameters/v2/JawForward":
            _JawForward = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/JawLeft":
            _JawLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/JawOpen":
            _JawOpen = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/JawRight":
            _JawRight = ReadFloat(message);
            break;
        }
        break;
      case 'L':
        switch (address)
        {
          case "/avatar/parameters/v2/LipFunnelLowerLeft":
            _LipFunnelLowerLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/LipFunnelLowerRight":
            _LipFunnelLowerRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/LipFunnelUpperLeft":
            _LipFunnelUpperLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/LipFunnelUpperRight":
            _LipFunnelUpperRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/LipPuckerLowerLeft":
            _LipPuckerLowerLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/LipPuckerLowerRight":
            _LipPuckerLowerRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/LipPuckerUpperLeft":
            _LipPuckerUpperLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/LipPuckerUpperRight":
            _LipPuckerUpperRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/LipSuckLowerLeft":
            _LipSuckLowerLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/LipSuckLowerRight":
            _LipSuckLowerRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/LipSuckUpperLeft":
            _LipSuckUpperLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/LipSuckUpperRight":
            _LipSuckUpperRight = ReadFloat(message);
            break;
        }
        break;
      case 'M':
        lastFaceTracking = DateTime.Now;
        switch (address)
        {
          case "/avatar/parameters/v2/MouthClosed":
            _MouthClosed = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthFrownLeft":
            _MouthFrownLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthFrownRight":
            _MouthFrownRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthLowerDownLeft":
            _MouthLowerDownLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthLowerDownRight":
            _MouthLowerDownRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthLowerX":
            _MouthLowerX = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthPressLeft":
            _MouthPressLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthPressRight":
            _MouthPressRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthRaiserLower":
            _MouthRaiserLower = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthRaiserUpper":
            _MouthRaiserUpper = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthSmileLeft":
            _MouthSmileLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthSmileRight":
            _MouthSmileRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthStretchLeft":
            _MouthStretchLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthStretchRight":
            _MouthStretchRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthTightenerLeft":
            _MouthTightenerLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthTightenerRight":
            _MouthTightenerRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthUpperUpLeft":
            _MouthUpperUpLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthUpperUpRight":
            _MouthUpperUpRight = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/MouthUpperX":
            _MouthUpperX = ReadFloat(message);
            break;
        }
        break;
      case 'N':
        switch (address)
        {
          case "/avatar/parameters/v2/NoseSneerLeft":
            _NoseSneerLeft = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/NoseSneerRight":
            _NoseSneerRight = ReadFloat(message);
            break;
        }
        break;
      case 'T':
        switch (address)
        {
          case "/avatar/parameters/v2/TongueOut":
            _TongueOut = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/TongueRoll":
            _TongueRoll = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/TongueX":
            _TongueX = ReadFloat(message);
            break;
          case "/avatar/parameters/v2/TongueY":
            _TongueY = ReadFloat(message);
            break;
        }
        break;
    }
  }

  private void ReceiveTrackingData()
  {
    do
    {
      Thread.Sleep(100);
      OscReceiver oscReceiver = this.oscReceiver;
      if (oscReceiver == null)
      {
        continue;
      }
      try
      {
        Loader.Msg($"Processing VRCFT OSC on on port {oscReceiver.Port}, state {oscReceiver.State}");
        while (oscReceiver.State == OscSocketState.Connected)
        {
          OscPacket oscPacket = oscReceiver.Receive();
          if (oscPacket is OscBundle oscBundle)
          {
            foreach (OscPacket item in oscBundle)
            {
              if (item is OscMessage message)
              {
                UpdateData(message);
              }
              else
              {
                Loader.Warn("Unexpected Osc packet type within bundle: " + item);
              }
            }
          }
          else if (oscPacket is OscMessage message)
          {
            UpdateData(message);
          }
          else
          {
            Loader.Warn("Unexpected root Osc packet: " + oscPacket);
          }
        }
      }
      catch (Exception ex)
      {
        if (ex.Message != "The receiver socket has been disconnected")
        {
          Loader.Error("Exception in OSC listener thread:\n" + ex);
        }
      }
      try
      {
        Loader.Msg($"Disposing of VRCFT OSC on on port {oscReceiver.Port}");
        oscReceiver.Dispose();
      }
      catch (Exception ex2)
      {
        Loader.Error("Exception disposing of OSC receiver:\n" + ex2);
      }
    }
    while (!disposed);
    Loader.Msg("OSC processing thread completed");
  }

  public void RequestTrackingData()
  {
    const int maxAttempts = 50;
    const int sleepDurationMs = 100;

    int attempts = 0;
    while (oscSender == null && attempts < maxAttempts)
    {
      Thread.Sleep(sleepDurationMs);
      attempts++;
    }

    if (oscSender == null)
    {
      Loader.Error("Failed to initialize oscSender after maximum attempts.");
    }

    try
    {
      var message = new OscMessage("/vrcft/settings/forceRelevant", disposed ? false : true);

      oscSender.Send(message);
      Loader.Msg("Sent message to request tracking data for VRCFT OSC");
    }
    catch (Exception ex)
    {
      Loader.Error($"Failed to send tracking data request: {ex.Message}");
    }
  }

  public void Dispose()
  {
    disposed = true;
    RequestTrackingData();
    oscReceiver?.Close();
    oscSender?.Close();
  }

  private static bool IsTracking(DateTime? timestamp)
  {
    if (!timestamp.HasValue)
    {
      return false;
    }
    if ((DateTime.UtcNow - timestamp.Value).TotalSeconds > 10.0)
    {
      return false;
    }
    return true;
  }

  private static float ReadFloat(OscMessage message)
  {
    Loader.Msg($"Processing Address {message.Address} {message[0]}");
    return (float)message[0];
  }
}

// based on BlueCyro's Impressive code https://github.com/BlueCyro/Impressive
public struct VRCFTEye
{
  public readonly bool IsTracking => IsValid && Eyelid > 0.1f;

  public readonly bool IsValid => EyeDirection.Magnitude > 0f && MathX.IsValid(EyeDirection);

  public float3 EyeDirection
  {
    readonly get => EyeRotation * float3.Forward;
    set => EyeRotation = floatQ.LookRotation(EyeDirection);
  }

  public floatQ EyeRotation;

  private float DirX;
  private float DirY;

  public float Eyelid;

  public void SetDirectionFromXY(float? X = null, float? Y = null)
  {
    DirX = X ?? DirX;
    DirY = Y ?? DirY;

    // Get the angles out of the eye look
    float xAng = MathX.Asin(DirX);
    float yAng = MathX.Asin(DirY);

    // Convert to cartesian coordinates
    EyeRotation = floatQ.Euler(yAng * MathX.Rad2Deg, xAng * MathX.Rad2Deg, 0f);
  }
}