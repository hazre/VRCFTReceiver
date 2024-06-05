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

  private float _leftEyeX;

  private float _leftEyeY;

  private float _rightEyeX;

  private float _rightEyeY;

  private float _leftOpenness;

  private float _rightOpenness;

  private float _leftLidRaise;

  private float _rightLidRaise;

  private float _leftBrowLower;

  private float _leftBrowPinch;

  private float _rightBrowLower;

  private float _rightBrowPinch;

  private float _leftInnerBrowRaise;

  private float _rightInnerBrowRaise;

  private float _leftOuterBrowRaise;

  private float _rightOuterBrowRaise;

  private float _leftLidTighter;

  private float _rightLidTighter;

  private float _leftLipPull;

  private float _leftLipSlant;

  private float _rightLipPull;

  private float _rightLipSlant;

  private float _leftLipDepress;

  private float _rightLipDepress;

  private float _leftDimple;

  private float _rightDimple;

  private float _lowerLipDepressLeft;

  private float _lowerLipDepressRight;

  private float _upperLipRaiserLeft;

  private float _upperLipRaiserRight;

  private float _puckerLeftLower;

  private float _puckerLeftUpper;

  private float _puckerRightLower;

  private float _puckerRightUpper;

  private float _cheekPuffLeft;

  private float _cheekSuckLeft;

  private float _cheekPuffRight;

  private float _cheekSuckRight;

  private float _cheekLeftRaise;

  private float _cheekRightRaise;

  private float _lipFunnelLB;

  private float _lipFunnelRB;

  private float _lipFunnelLT;

  private float _lipFunnelRT;

  private float _jawThrust;

  private float _jawDrop;

  private float _jawLeft;

  private float _jawRight;

  private float _lipsToward;

  private float _tongueOut;

  private float _mouthUpperX;

  private float _mouthLowerX;

  private float _noseWrinkleLeft;

  private float _noseWrinkleRight;

  private float _lipSuckLB;

  private float _lipSuckRB;

  private float _lipSuckLT;

  private float _lipSuckRT;

  private float _lipTightenL;

  private float _lipTightenR;

  private float _lipStretchL;

  private float _lipStretchR;

  private float _lipPressL;

  private float _lipPressR;

  private float _chinRaiseBottom;

  private float _chinRaiseTop;

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
      radius * MathX.Sin(_leftEyeY) * MathX.Cos(_leftEyeX),
      radius * MathX.Sin(_leftEyeY) * MathX.Sin(_leftEyeX),
      radius * MathX.Cos(_leftEyeY)
    );

    float3 rightGazeVector = new float3(
      radius * MathX.Sin(_rightEyeY) * MathX.Cos(_leftEyeX),
      radius * MathX.Sin(_rightEyeY) * MathX.Sin(_leftEyeX),
      radius * MathX.Cos(_rightEyeY)
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
    eyes.LeftEye.Openness = _leftOpenness;
    eyes.RightEye.Openness = _rightOpenness;
    eyes.LeftEye.Widen = _leftLidRaise;
    eyes.RightEye.Widen = _rightLidRaise;
    eyes.LeftEye.Squeeze = _leftLidTighter;
    eyes.RightEye.Squeeze = _rightLidTighter;
    float _leftBrowLowerer = _leftBrowPinch - _leftBrowLower;
    eyes.LeftEye.InnerBrowVertical = _leftInnerBrowRaise - _leftBrowLowerer;
    eyes.LeftEye.OuterBrowVertical = _leftOuterBrowRaise - _leftBrowLowerer;
    float _rightBrowLowerer = _rightBrowPinch - _rightBrowLower;
    eyes.RightEye.InnerBrowVertical = _rightInnerBrowRaise - _rightBrowLowerer;
    eyes.RightEye.OuterBrowVertical = _rightOuterBrowRaise - _rightBrowLowerer;
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
    float _leftLipCornerPuller = _leftLipPull - _leftLipSlant;
    mouth.MouthLeftSmileFrown = _leftLipCornerPuller - _leftLipDepress;
    float _rightLipCornerPuller = _rightLipPull - _rightLipSlant;
    mouth.MouthRightSmileFrown = _rightLipCornerPuller - _rightLipDepress;
    mouth.MouthLeftDimple = _leftDimple;
    mouth.MouthRightDimple = _rightDimple;
    mouth.CheekLeftPuffSuck = _cheekPuffLeft - _cheekSuckLeft;
    mouth.CheekRightPuffSuck = _cheekPuffRight - _cheekSuckRight;
    mouth.CheekLeftRaise = _cheekLeftRaise;
    mouth.CheekRightRaise = _cheekRightRaise;
    mouth.LipUpperLeftRaise = _upperLipRaiserLeft;
    mouth.LipUpperRightRaise = _upperLipRaiserRight;
    mouth.LipLowerLeftRaise = _lowerLipDepressLeft;
    mouth.LipLowerRightRaise = _lowerLipDepressRight;
    mouth.MouthPoutLeft = _puckerLeftLower - _puckerLeftUpper;
    mouth.MouthPoutRight = _puckerRightLower - _puckerRightUpper;
    mouth.LipUpperHorizontal = _mouthUpperX;
    mouth.LipLowerHorizontal = _mouthLowerX;
    mouth.LipTopLeftOverturn = _lipFunnelLT;
    mouth.LipTopRightOverturn = _lipFunnelRT;
    mouth.LipBottomLeftOverturn = _lipFunnelLB;
    mouth.LipBottomRightOverturn = _lipFunnelRB;
    mouth.LipTopLeftOverUnder = 0f - _lipSuckLT;
    mouth.LipTopRightOverUnder = 0f - _lipSuckRT;
    mouth.LipBottomLeftOverUnder = 0f - _lipSuckLB;
    mouth.LipBottomRightOverUnder = 0f - _lipSuckRB;
    mouth.LipLeftStretchTighten = _lipStretchL - _lipTightenL;
    mouth.LipRightStretchTighten = _lipStretchR - _lipTightenR;
    mouth.LipsLeftPress = _lipPressL;
    mouth.LipsRightPress = _lipPressR;
    mouth.Jaw = new float3(_jawRight - _jawLeft, 0f - _lipsToward, _jawThrust);
    mouth.JawOpen = MathX.Clamp01(_jawDrop - _lipsToward);
    // removed _tongueRetreat, it's not part of UE
    // TODO: add rest of tongue parameters
    mouth.Tongue = new float3(0f, 0f, _tongueOut);
    mouth.NoseWrinkleLeft = _noseWrinkleLeft;
    mouth.NoseWrinkleRight = _noseWrinkleRight;
    mouth.ChinRaiseBottom = _chinRaiseBottom;
    mouth.ChinRaiseTop = _chinRaiseTop;
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
                _lipSuckLB = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipSuckLowerRight":
                _lipSuckRB = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipSuckUpperLeft":
                _lipSuckLT = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipSuckUpperRight":
                _lipSuckRT = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipPuckerLowerLeft":
                _puckerLeftLower = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipPuckerUpperLeft":
                _puckerLeftUpper = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipPuckerLowerRight":
                _puckerRightLower = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipPuckerUpperRight":
                _puckerRightUpper = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipFunnelLowerLeft":
                _lipFunnelLB = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipFunnelUpperLeft":
                _lipFunnelLT = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipFunnelLowerRight":
                _lipFunnelRB = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/LipFunnelUpperRight":
                _lipFunnelRT = ReadFloat(message);
                break;
            }
            break;
          case 'J':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/JawForward":
                _jawThrust = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/JawLeft":
                _jawLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/JawRight":
                _jawRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/JawOpen":
                _jawDrop = ReadFloat(message);
                break;
            }
            break;
          // TODO: add rest of tongue parameters
          case 'T':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/TongueOut":
                _tongueOut = ReadFloat(message);
                break;
            }
            break;
          case 'M':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/MouthUpperX":
                _mouthUpperX = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthLowerX":
                _mouthLowerX = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthPressLeft":
                _lipPressL = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthPressRight":
                _lipPressR = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthRaiserLower":
                _chinRaiseBottom = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthRaiserUpper":
                _chinRaiseTop = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthClosed":
                _lipsToward = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthUpperUpLeft":
                _upperLipRaiserLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthUpperUpRight":
                _upperLipRaiserRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthCornerPullLeft":
                _leftLipPull = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthCornerSlantLeft":
                _leftLipSlant = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthCornerPullRight":
                _rightLipPull = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthCornerSlantRight":
                _rightLipSlant = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthStretchLeft":
                _lipStretchL = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthStretchRight":
                _lipStretchR = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthTightenerLeft":
                _lipTightenL = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthTightenerRight":
                _lipTightenR = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthFrownLeft":
                _leftLipDepress = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthFrownRight":
                _rightLipDepress = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthLowerDownLeft":
                _lowerLipDepressLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthLowerDownRight":
                _lowerLipDepressRight = ReadFloat(message);
                break;
            }
            break;
          case 'E':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/EyeLeftX":
                _leftEyeX = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeLeftY":
                _leftEyeY = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeRightX":
                _rightEyeX = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeRightY":
                _rightEyeY = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeOpenLeft":
                _leftOpenness = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeOpenRight":
                _leftOpenness = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeWideLeft":
                _leftLidRaise = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeWideRight":
                _rightLidRaise = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeSquintLeft":
                _leftLidTighter = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/EyeSquintRight":
                _rightLidTighter = ReadFloat(message);
                break;
            }
            break;
          case 'D':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/MouthDimpleLeft":
                _leftDimple = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/MouthDimpleRight":
                _rightDimple = ReadFloat(message);
                break;
            }
            break;
          case 'C':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/CheekPuffLeft":
                _cheekPuffRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/CheekPuffRight":
                _cheekPuffLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/CheekSuckLeft":
                _cheekSuckRight = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/CheekSuckRight":
                _cheekSuckLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/CheekSquintLeft":
                _cheekLeftRaise = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/CheekSquintRight":
                _cheekRightRaise = ReadFloat(message);
                break;
            }
            break;
          case 'B':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/BrowPinchLeft":
                _leftBrowPinch = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowLowererLeft":
                _leftBrowLower = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowPinchRight":
                _rightBrowPinch = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowLowererRight":
                _rightBrowLower = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowInnerUpLeft":
                _leftInnerBrowRaise = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowInnerUpRight":
                _rightInnerBrowRaise = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowOuterUpLeft":
                _leftOuterBrowRaise = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/BrowOuterUpRight":
                _rightOuterBrowRaise = ReadFloat(message);
                break;
            }
            break;
          case 'N':
            switch (address)
            {
              case "/avatar/parameters/FT/v2/NoseSneerLeft":
                _noseWrinkleLeft = ReadFloat(message);
                break;
              case "/avatar/parameters/FT/v2/NoseSneerRight":
                _noseWrinkleRight = ReadFloat(message);
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