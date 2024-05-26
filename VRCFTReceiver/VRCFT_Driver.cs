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

  private float3 _gazePoint;

  private float _leftOpenness;

  private float _rightOpenness;

  private float _leftLidRaise;

  private float _rightLidRaise;

  private float _leftBrowLower;

  private float _rightBrowLower;

  private float _leftInnerBrowRaise;

  private float _rightInnerBrowRaise;

  private float _leftOuterBrowRaise;

  private float _rightOuterBrowRaise;

  private float _leftLidTighter;

  private float _rightLidTighter;

  private float _leftLipPull;

  private float _rightLipPull;

  private float _leftLipDepress;

  private float _rightLipDepress;

  private float _leftDimple;

  private float _rightDimple;

  private float _lowerLipDepressLeft;

  private float _lowerLipDepressRight;

  private float _upperLipRaiserLeft;

  private float _upperLipRaiserRight;

  private float _puckerLeft;

  private float _puckerRight;

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

  private float _tongueRetreat;

  private float _mouthLeft;

  private float _mouthRight;

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
    float3 v = float3.Left;
    float3 v2 = v * 0.065f;
    float3 b = v2 * 0.5f;
    v = float3.Right;
    v2 = v * 0.065f;
    float3 b2 = v2 * 0.5f;
    float3 direction = (_gazePoint - b).Normalized;
    float3 direction2 = (_gazePoint - b2).Normalized;
    eyes.LeftEye.UpdateWithDirection(in direction);
    eyes.RightEye.UpdateWithDirection(in direction2);
    eyes.LeftEye.Openness = _leftOpenness;
    eyes.RightEye.Openness = _rightOpenness;
    eyes.LeftEye.Widen = _leftLidRaise;
    eyes.RightEye.Widen = _rightLidRaise;
    eyes.LeftEye.Squeeze = _leftLidTighter;
    eyes.RightEye.Squeeze = _rightLidTighter;
    eyes.LeftEye.InnerBrowVertical = _leftInnerBrowRaise - _leftBrowLower;
    eyes.LeftEye.OuterBrowVertical = _leftOuterBrowRaise - _leftBrowLower;
    eyes.RightEye.InnerBrowVertical = _rightInnerBrowRaise - _rightBrowLower;
    eyes.RightEye.OuterBrowVertical = _rightOuterBrowRaise - _rightBrowLower;
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
    mouth.MouthLeftSmileFrown = _leftLipPull - _leftLipDepress;
    mouth.MouthRightSmileFrown = _rightLipPull - _rightLipDepress;
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
    mouth.MouthPoutLeft = _puckerLeft;
    mouth.MouthPoutRight = _puckerRight;
    mouth.LipUpperHorizontal = _mouthRight - _mouthLeft;
    mouth.LipLowerHorizontal = mouth.LipUpperHorizontal;
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
    mouth.Tongue = new float3(0f, 0f, _tongueOut - _tongueRetreat);
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
        switch (address[15])
        {
          case 'L':
            switch (address)
            {
              // case "/sl/xrfb/facec/LowerFace":
              //   if (ReadFloat(message) > 0.5f)
              //   {
              //     lastFaceTracking = DateTime.UtcNow;
              //   }
              //   else
              //   {
              //     lastFaceTracking = null;
              //   }
              //   break;
              case "/sl/xrfb/facew/LipSuckLB":
                _lipSuckLB = ReadFloat(message);
                break;
              case "/sl/xrfb/facew/LipSuckRB":
                _lipSuckRB = ReadFloat(message);
                break;
              case "/sl/xrfb/facew/LipSuckLT":
                _lipSuckLT = ReadFloat(message);
                break;
              case "/sl/xrfb/facew/LipSuckRT":
                _lipSuckRT = ReadFloat(message);
                break;
            }
            break;
          case 'U':
            if (address == "/sl/xrfb/facec/UpperFace")
            {
              if (ReadFloat(message) > 0.5f)
              {
                lastEyeTracking = DateTime.UtcNow;
              }
              else
              {
                lastEyeTracking = null;
              }
            }
            break;
          case 'J':
            if (address == "/sl/xrfb/facew/JawThrust")
            {
              _jawThrust = ReadFloat(message);
            }
            break;
          case 'T':
            if (address == "/sl/xrfb/facew/ToungeOut")
            {
              _tongueOut = ReadFloat(message);
            }
            break;
          case 'M':
            if (address == "/sl/xrfb/facew/MouthLeft")
            {
              _mouthLeft = ReadFloat(message);
            }
            break;
        }
        break;
      case 23:
        switch (address[22])
        {
          case 't':
            if (address == "/sl/eyeTrackedGazePoint")
            {
              float3 a = ReadFloat3(message);
              float3 b = new float3(1f, 1f, -1f);
              _gazePoint = a * b;
            }
            break;
          case 'L':
            if (address == "/sl/xrfb/facew/DimplerL")
            {
              _leftDimple = ReadFloat(message);
            }
            break;
          case 'R':
            if (address == "/sl/xrfb/facew/DimplerR")
            {
              _rightDimple = ReadFloat(message);
            }
            break;
        }
        break;
      case 26:
        switch (address[25])
        {
          case 'L':
            if (!(address == "/sl/xrfb/facew/EyesClosedL"))
            {
              if (address == "/sl/xrfb/facew/LipPressorL")
              {
                _lipPressL = ReadFloat(message);
              }
            }
            else
            {
              _leftOpenness = 1f - MathX.Sqrt(ReadFloat(message));
            }
            break;
          case 'R':
            if (!(address == "/sl/xrfb/facew/EyesClosedR"))
            {
              if (address == "/sl/xrfb/facew/LipPressorR")
              {
                _lipPressR = ReadFloat(message);
              }
            }
            else
            {
              _rightOpenness = 1f - MathX.Sqrt(ReadFloat(message));
            }
            break;
          case 'B':
            if (address == "/sl/xrfb/facew/ChinRaiserB")
            {
              _chinRaiseBottom = ReadFloat(message);
            }
            break;
          case 'T':
            if (address == "/sl/xrfb/facew/ChinRaiserT")
            {
              _chinRaiseTop = ReadFloat(message);
            }
            break;
        }
        break;
      case 25:
        switch (address[20])
        {
          case 'P':
            if (!(address == "/sl/xrfb/facew/CheekPuffL"))
            {
              if (address == "/sl/xrfb/facew/CheekPuffR")
              {
                _cheekPuffRight = ReadFloat(message);
              }
            }
            else
            {
              _cheekPuffLeft = ReadFloat(message);
            }
            break;
          case 'S':
            if (!(address == "/sl/xrfb/facew/CheekSuckL"))
            {
              if (address == "/sl/xrfb/facew/CheekSuckR")
              {
                _cheekSuckRight = ReadFloat(message);
              }
            }
            else
            {
              _cheekSuckLeft = ReadFloat(message);
            }
            break;
          case 'o':
            if (address == "/sl/xrfb/facew/LipsToward")
            {
              _lipsToward = ReadFloat(message);
            }
            break;
          case 'c':
            if (!(address == "/sl/xrfb/facew/LipPuckerL"))
            {
              if (address == "/sl/xrfb/facew/LipPuckerR")
              {
                _puckerRight = ReadFloat(message);
              }
            }
            else
            {
              _puckerLeft = ReadFloat(message);
            }
            break;
          case 'R':
            if (address == "/sl/xrfb/facew/MouthRight")
            {
              _mouthRight = ReadFloat(message);
            }
            break;
        }
        break;
      case 27:
        switch (address[15])
        {
          case 'C':
            if (!(address == "/sl/xrfb/facew/CheekRaiserL"))
            {
              if (address == "/sl/xrfb/facew/CheekRaiserR")
              {
                _cheekRightRaise = ReadFloat(message);
              }
            }
            else
            {
              _cheekLeftRaise = ReadFloat(message);
            }
            break;
          case 'B':
            if (!(address == "/sl/xrfb/facew/BrowLowererL"))
            {
              if (address == "/sl/xrfb/facew/BrowLowererR")
              {
                _rightBrowLower = ReadFloat(message);
              }
            }
            else
            {
              _leftBrowLower = ReadFloat(message);
            }
            break;
        }
        break;
      case 30:
        switch (address[22])
        {
          case 'w':
            if (address == "/sl/xrfb/facew/JawSidewaysLeft")
            {
              _jawLeft = ReadFloat(message);
            }
            break;
          case 'd':
            if (!(address == "/sl/xrfb/facew/UpperLidRaiserL"))
            {
              if (address == "/sl/xrfb/facew/UpperLidRaiserR")
              {
                _rightLidRaise = ReadFloat(message);
              }
            }
            else
            {
              _leftLidRaise = ReadFloat(message);
            }
            break;
          case 'p':
            if (!(address == "/sl/xrfb/facew/UpperLipRaiserL"))
            {
              if (address == "/sl/xrfb/facew/UpperLipRaiserR")
              {
                _upperLipRaiserRight = ReadFloat(message);
              }
            }
            else
            {
              _upperLipRaiserLeft = ReadFloat(message);
            }
            break;
        }
        break;
      case 31:
        switch (address[15])
        {
          case 'J':
            if (address == "/sl/xrfb/facew/JawSidewaysRight")
            {
              _jawRight = ReadFloat(message);
            }
            break;
          case 'L':
            if (!(address == "/sl/xrfb/facew/LipCornerPullerL"))
            {
              if (address == "/sl/xrfb/facew/LipCornerPullerR")
              {
                _rightLipPull = ReadFloat(message);
              }
            }
            else
            {
              _leftLipPull = ReadFloat(message);
            }
            break;
          case 'I':
            if (!(address == "/sl/xrfb/facew/InnerBrowRaiserL"))
            {
              if (address == "/sl/xrfb/facew/InnerBrowRaiserR")
              {
                _rightInnerBrowRaise = ReadFloat(message);
              }
            }
            else
            {
              _leftInnerBrowRaise = ReadFloat(message);
            }
            break;
          case 'O':
            if (!(address == "/sl/xrfb/facew/OuterBrowRaiserL"))
            {
              if (address == "/sl/xrfb/facew/OuterBrowRaiserR")
              {
                _rightOuterBrowRaise = ReadFloat(message);
              }
            }
            else
            {
              _leftOuterBrowRaise = ReadFloat(message);
            }
            break;
          case 'K':
          case 'M':
          case 'N':
            break;
        }
        break;
      case 28:
        switch (address[18])
        {
          case 'g':
            if (address == "/sl/xrfb/facew/TongueRetreat")
            {
              _tongueRetreat = ReadFloat(message);
            }
            break;
          case 'e':
            if (!(address == "/sl/xrfb/facew/NoseWrinklerL"))
            {
              if (address == "/sl/xrfb/facew/NoseWrinklerR")
              {
                _noseWrinkleRight = ReadFloat(message);
              }
            }
            else
            {
              _noseWrinkleLeft = ReadFloat(message);
            }
            break;
          case 'F':
            switch (address)
            {
              case "/sl/xrfb/facew/LipFunnelerLB":
                _lipFunnelLB = ReadFloat(message);
                break;
              case "/sl/xrfb/facew/LipFunnelerRB":
                _lipFunnelRB = ReadFloat(message);
                break;
              case "/sl/xrfb/facew/LipFunnelerLT":
                _lipFunnelLT = ReadFloat(message);
                break;
              case "/sl/xrfb/facew/LipFunnelerRT":
                _lipFunnelRT = ReadFloat(message);
                break;
            }
            break;
          case 'S':
            if (!(address == "/sl/xrfb/facew/LipStretcherL"))
            {
              if (address == "/sl/xrfb/facew/LipStretcherR")
              {
                _lipStretchR = ReadFloat(message);
              }
            }
            else
            {
              _lipStretchL = ReadFloat(message);
            }
            break;
          case 'T':
            switch (address)
            {
              case "/sl/xrfb/facew/LipTightenerL":
                _lipTightenL = ReadFloat(message);
                break;
              case "/sl/xrfb/facew/LipTightenerR":
                _lipTightenR = ReadFloat(message);
                break;
              case "/sl/xrfb/facew/LidTightenerL":
                _leftLidTighter = ReadFloat(message);
                break;
              case "/sl/xrfb/facew/LidTightenerR":
                _rightLidTighter = ReadFloat(message);
                break;
            }
            break;
        }
        break;
      case 34:
        switch (address[33])
        {
          case 'L':
            if (address == "/sl/xrfb/facew/LipCornerDepressorL")
            {
              _leftLipDepress = ReadFloat(message);
            }
            break;
          case 'R':
            if (address == "/sl/xrfb/facew/LipCornerDepressorR")
            {
              _rightLipDepress = ReadFloat(message);
            }
            break;
        }
        break;
      case 33:
        switch (address[32])
        {
          case 'L':
            if (address == "/sl/xrfb/facew/LowerLipDepressorL")
            {
              _lowerLipDepressLeft = ReadFloat(message);
            }
            break;
          case 'R':
            if (address == "/sl/xrfb/facew/LowerLipDepressorR")
            {
              _lowerLipDepressRight = ReadFloat(message);
            }
            break;
        }
        break;
      case 22:
        if (address == "/sl/xrfb/facew/JawDrop")
        {
          _jawDrop = ReadFloat(message);
        }
        break;
      case 29:
      case 32:
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