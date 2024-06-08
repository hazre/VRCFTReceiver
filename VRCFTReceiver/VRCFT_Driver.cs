using System;
using System.Net;
using System.Threading;
using Elements.Core;
using FrooxEngine;
using ResoniteModLoader;
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

  private OscReceiver oscReceiver;
  private OscSender oscSender;

  private DateTime? lastEyeTracking;

  private DateTime? lastFaceTracking;

  private float _EyeLeftX;

  private float _EyeLeftY;

  private float _EyeRightX;

  private float _EyeRightY;

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

  private float _MouthCornerPullLeft;

  private float _MouthCornerSlantLeft;

  private float _MouthCornerPullRight;

  private float _MouthCornerSlantRight;

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

  private float _CheekPuffLeft;

  private float _CheekSuckLeft;

  private float _CheekPuffRight;

  private float _CheekSuckRight;

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
    UniLog.Log("Starting OSC processing thread");
    thread = new Thread(ReceiveTrackingData);
    thread.Start();
    Loader.config.OnThisConfigurationChanged += OnSettingsChanged;
  }

  private void OnSettingsChanged(ConfigurationChangedEvent configurationChangedEvent)
  {
    int receiverPort = Loader.config.GetValue(Loader.KEY_RECEIVER_PORT);
    int senderPort = Loader.config.GetValue(Loader.KEY_SENDER_PORT);
    IPAddress ip = IPAddress.Parse(Loader.config.GetValue(Loader.KEY_IP));
    OscReceiver currentOscReceiver = this.oscReceiver;
    if ((currentOscReceiver == null || currentOscReceiver.Port != receiverPort) && receiverPort != 0)
    {
      try
      {
        UniLog.Log($"Starting VRCFaceTracking OSC listener on on port {receiverPort}");
        OscReceiver oscReceiver = new OscReceiver(ip, receiverPort);
        oscReceiver.Connect();
        OscReceiver previousOscReceiver = this.oscReceiver;
        this.oscReceiver = oscReceiver;
        previousOscReceiver?.Close();
      }
      catch (Exception ex)
      {
        UniLog.Error("Exception when starting VRCFaceTracking OSC receiver:\n" + ex);
      }
    }
  }

  public void UpdateInputs(float deltaTime)
  {
    UpdateEyes(deltaTime);
    UpdateMouth(deltaTime);
  }

  private void UpdateEyes(float deltaTime)
  {
    if (!IsTracking(lastEyeTracking))
    {
      eyes.IsEyeTrackingActive = false;
      eyes.SetTracking(state: false);
      return;
    }
    eyes.IsEyeTrackingActive = input.VR_Active;
    eyes.SetTracking(state: true);

    float radius = 2.0f;

    float3 leftGazeVector = new float3(
      radius * MathX.Sin(_EyeLeftY) * MathX.Cos(_EyeLeftX),
      radius * MathX.Sin(_EyeLeftY) * MathX.Sin(_EyeLeftX),
      radius * MathX.Cos(_EyeLeftY)
    );

    float3 rightGazeVector = new float3(
      radius * MathX.Sin(_EyeRightY) * MathX.Cos(_EyeLeftX),
      radius * MathX.Sin(_EyeRightY) * MathX.Sin(_EyeLeftX),
      radius * MathX.Cos(_EyeRightY)
    );

    float3 v = float3.Left;
    float3 v2 = v * 0.065f;
    float3 b = v2 * 0.5f;
    v = float3.Right;
    v2 = v * 0.065f;
    float3 b2 = v2 * 0.5f;
    float3 direction = (leftGazeVector - b).Normalized;
    float3 direction2 = (rightGazeVector - b2).Normalized;
    eyes.LeftEye.UpdateWithDirection(in direction);
    eyes.RightEye.UpdateWithDirection(in direction2);
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

  private void UpdateMouth(float deltaTime)
  {
    // replace this with a settings option instead
    // if (!IsTracking(lastFaceTracking))
    // {
    //   mouth.IsTracking = false;
    //   return;
    // }
    mouth.IsTracking = true;
    float MouthSmileLeft = _MouthCornerPullLeft - _MouthCornerSlantLeft;
    mouth.MouthLeftSmileFrown = MouthSmileLeft - _MouthFrownLeft;
    float MouthSmileRight = _MouthCornerPullRight - _MouthCornerSlantRight;
    mouth.MouthRightSmileFrown = MouthSmileRight - _MouthFrownRight;
    mouth.MouthLeftDimple = _MouthDimpleLeft;
    mouth.MouthRightDimple = _MouthDimpleRight;
    mouth.CheekLeftPuffSuck = _CheekPuffLeft - _CheekSuckLeft;
    mouth.CheekRightPuffSuck = _CheekPuffRight - _CheekSuckRight;
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
    mouth.Jaw = new float3(_JawRight - _JawLeft, 0f - _MouthClosed, _JawForward);
    mouth.JawOpen = MathX.Clamp01(_JawOpen - _MouthClosed);
    // removed _tongueRetreat, it's not part of UE
    // TODO: add rest of tongue parameters
    mouth.Tongue = new float3(0f, 0f, _TongueOut);
    mouth.NoseWrinkleLeft = _NoseSneerLeft;
    mouth.NoseWrinkleRight = _NoseSneerRight;
    mouth.ChinRaiseBottom = _MouthRaiserLower;
    mouth.ChinRaiseTop = _MouthRaiserUpper;
  }

  private void UpdateData(OscMessage message)
  {
    string address = message.Address;
    if (address == null)
    {
      return;
    }
    switch (address.Length)
    {
      case 24:
        switch (address[25])
        {
          case 'L':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/LipSuckLowerLeft":
                _LipSuckLowerLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipSuckLowerRight":
                _LipSuckLowerRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipSuckUpperLeft":
                _LipSuckUpperLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipSuckUpperRight":
                _LipSuckUpperRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipPuckerLowerLeft":
                _LipPuckerLowerLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipPuckerUpperLeft":
                _LipPuckerUpperLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipPuckerLowerRight":
                _LipPuckerLowerRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipPuckerUpperRight":
                _LipPuckerUpperRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipFunnelLowerLeft":
                _LipFunnelLowerLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipFunnelUpperLeft":
                _LipFunnelUpperLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipFunnelLowerRight":
                _LipFunnelLowerRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipFunnelUpperRight":
                _LipFunnelUpperRight = ReadFloat(message);
                break;
            }
            break;
          case 'J':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/JawForward":
                _JawForward = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/JawLeft":
                _JawLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/JawRight":
                _JawRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/JawOpen":
                _JawOpen = ReadFloat(message);
                break;
            }
            break;
          // TODO: add rest of tongue parameters
          case 'T':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/TongueOut":
                _TongueOut = ReadFloat(message);
                break;
            }
            break;
          case 'M':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/MouthUpperX":
                _MouthUpperX = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthLowerX":
                _MouthLowerX = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthPressLeft":
                _MouthPressLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthPressRight":
                _MouthPressRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthRaiserLower":
                _MouthRaiserLower = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthRaiserUpper":
                _MouthRaiserUpper = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthClosed":
                _MouthClosed = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthUpperUpLeft":
                _MouthUpperUpLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthUpperUpRight":
                _MouthUpperUpRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthCornerPullLeft":
                _MouthCornerPullLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthCornerSlantLeft":
                _MouthCornerSlantLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthCornerPullRight":
                _MouthCornerPullRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthCornerSlantRight":
                _MouthCornerSlantRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthStretchLeft":
                _MouthStretchLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthStretchRight":
                _MouthStretchRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthTightenerLeft":
                _MouthTightenerLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthTightenerRight":
                _MouthTightenerRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthFrownLeft":
                _MouthFrownLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthFrownRight":
                _MouthFrownRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthLowerDownLeft":
                _MouthLowerDownLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthLowerDownRight":
                _MouthLowerDownRight = ReadFloat(message);
                break;
            }
            break;
          case 'E':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/EyeLeftX":
                _EyeLeftX = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeLeftY":
                _EyeLeftY = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeRightX":
                _EyeRightX = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeRightY":
                _EyeRightY = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeOpenLeft":
                _EyeOpenLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeOpenRight":
                _EyeOpenRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeWideLeft":
                _EyeWideLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeWideRight":
                _EyeWideRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeSquintLeft":
                _EyeSquintLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeSquintRight":
                _EyeSquintRight = ReadFloat(message);
                break;
            }
            break;
          case 'D':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/MouthDimpleLeft":
                _MouthDimpleLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthDimpleRight":
                _MouthDimpleRight = ReadFloat(message);
                break;
            }
            break;
          case 'C':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/CheekPuffLeft":
                _CheekPuffLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/CheekPuffRight":
                _CheekPuffRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/CheekSuckLeft":
                _CheekSuckLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/CheekSuckRight":
                _CheekSuckRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/CheekSquintLeft":
                _CheekSquintLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/CheekSquintRight":
                _CheekSquintRight = ReadFloat(message);
                break;
            }
            break;
          case 'B':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/BrowPinchLeft":
                _BrowPinchLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowLowererLeft":
                _BrowLowererLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowPinchRight":
                _BrowPinchRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowLowererRight":
                _BrowLowererRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowInnerUpLeft":
                _BrowInnerUpLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowInnerUpRight":
                _BrowInnerUpRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowOuterUpLeft":
                _BrowOuterUpLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowOuterUpRight":
                _BrowOuterUpRight = ReadFloat(message);
                break;
            }
            break;
          case 'N':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/NoseSneerLeft":
                _NoseSneerLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/NoseSneerRight":
                _NoseSneerRight = ReadFloat(message);
                break;
            }
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
        UniLog.Log($"Processing SteamLink OSC on on port {oscReceiver.Port}");
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
                UniLog.Warning("Unexpected Osc packet type within bundle: " + item);
              }
            }
          }
          else
          {
            UniLog.Warning("Unexpected root Osc packet: " + oscPacket);
          }
        }
      }
      catch (Exception ex)
      {
        if (ex.Message != "The receiver socket has been disconnected")
        {
          UniLog.Error("Exception in OSC listener thread:\n" + ex);
        }
      }
      try
      {
        UniLog.Log($"Disposing of SteamLink OSC on on port {oscReceiver.Port}");
        oscReceiver.Dispose();
      }
      catch (Exception ex2)
      {
        UniLog.Error("Exception disposing of OSC receiver:\n" + ex2);
      }
    }
    while (!disposed);
    UniLog.Log("OSC processing thread completed");
  }

  public void Dispose()
  {
    disposed = true;
    oscReceiver?.Close();
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
    return (float)message[0];
  }

  private static float3 ReadFloat3(OscMessage message)
  {
    return new float3((float)message[0], (float)message[1], (float)message[2]);
  }
}