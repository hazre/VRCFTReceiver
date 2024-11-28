using Elements.Core;
using FrooxEngine;
using Rug.Osc;
using ReSounding;
using System.Data.Common;

namespace VRCFTReceiver;

public class VRCFTDriver : IInputDriver
{
    private InputInterface? input;
    private Eyes? eyes;
    private Mouth? mouth;

    private readonly OSCBridge bridge = new();
    private readonly VRCFTEyes eyeData = new();
    private readonly VRCFTFace faceData = new();

    private readonly object _lock = new();
    public int UpdateOrder => 150;

    public DateTime DEBUG_TIME = DateTime.Now;


    public void CollectDeviceInfos(DataTreeList list)
    {
        VRCFTReceiver.Msg("Collecting VRCFT device info");

        // Eye tracking
        DataTreeDictionary eyeDict = new();

        eyeDict.Add("Name", "VRCFT Eye Datastream");
        eyeDict.Add("Type", "Eye Tracking");
        eyeDict.Add("Model", "VRCFT");
        list.Add(eyeDict);


        // Mouth tracking
        DataTreeDictionary mouthDict = new();

        mouthDict.Add("Name", "VRCFT Face Datastream");
        mouthDict.Add("Type", "Lip Tracking");
        mouthDict.Add("Model", "VRCFT");
        list.Add(mouthDict);
    }


    public void RegisterInputs(InputInterface i)
    {
        input = i;
        VRCFTReceiver.Msg("Attempting to start OSC listener");
        try
        {
            if (bridge.TryStartListen())
            {
                OSCMapper.RegisterConverters(typeof(OSCTypeConverters)); // Register custom type converter(s)
                VRCFTReceiver.Msg("Starting VRCFT datastream!");

                // Register eye and mouth tracking devices
                eyes = new(input, "VRCFTReceiver Datastream", false);
                mouth = new(input, "VRCFTReceiver Datastream", [
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

                // Subscribe events for receiving packets, changing config options, and shutting down
                bridge.ReceivedPacket += OnNewPacket;
                VRCFTReceiver.Port_Config.OnChanged += OnSettingChanged;
                i.Engine.OnShutdown += Shutdown;
            }
        }
        catch (Exception ex)
        {
            VRCFTReceiver.Msg($"Failed to initialize OSC server! Exception: {ex}");
        }
    }


    void OnNewPacket(object sender, OscPacket packet)
    {
        DEBUG_TRY_LOG(packet);
        if (packet is OscMessage msg)
        {
            Map(msg.Address, msg.ToArray());
        }
        else if (packet is OscBundle bundle)
        {
            foreach (var pkt in bundle)
            {
                if (pkt is OscMessage m)
                {
                    Map(m.Address, m.ToArray());
                }
            }
        }
    }


    void DEBUG_TRY_LOG(OscPacket pckt)
    {
        if (DateTime.Now - DEBUG_TIME < TimeSpan.FromSeconds(2f))
            return;

        DEBUG_TIME = DateTime.Now;
        if (pckt is OscMessage msg)
        {
            VRCFTReceiver.Msg("---- DEBUG MESSAGE ----");
            VRCFTReceiver.Msg(msg);
            VRCFTReceiver.Msg("---- END MSG ----");
            VRCFTReceiver.Msg("");
        }
        else if (pckt is OscBundle bnd)
        {
            VRCFTReceiver.Msg("---- DEBUG BUNDLE ----");
            foreach (var pkt in bnd)
            {
                VRCFTReceiver.Msg(pkt);
            }
            VRCFTReceiver.Msg("---- END BUNDLE ----");
            VRCFTReceiver.Msg("");
        }
    }


    void Map(string addr, object[] data)
    {
        lock (_lock)
        {
            try
            {
                OSCMapper.TryMapOSC(eyeData, addr, data);
                OSCMapper.TryMapOSC(faceData, addr, data);
            }
            catch (Exception ex)
            {
                VRCFTReceiver.Msg($"Invalid mapping to address \"{addr}\"! Exception: {ex}");
            }
        }
    }


    public void UpdateInputs(float dt)
    {
        if (eyes != null && mouth != null && input != null)
        {
            bool enabled = VRCFTReceiver.Enabled;
            eyes.IsDeviceActive = enabled;
            eyes.IsEyeTrackingActive = enabled;
            mouth.IsDeviceActive = enabled;
            mouth.IsTracking = enabled;

            if (enabled)
            {
                lock (_lock)
                {
                    UpdateEye(eyeData.EyeLeft, eyes.LeftEye);
                    UpdateEye(eyeData.EyeRight, eyes.RightEye);
                    UpdateEye(eyeData.EyeCombined, eyes.CombinedEye);

                    eyes.ComputeCombinedEyeParameters();
                    eyes.FinishUpdate();

                    UpdateFace(mouth);
                }
            }
            else
            {

            }
        }
    }


    public void UpdateEye(VRCFTEye source, Eye dest)
    {
        if (source.IsValid)
        {
            dest.UpdateWithRotation(source.EyeRotation);

            dest.Widen = source.Widen;
            dest.Openness = source.Openness;
            dest.PupilDiameter = 0.004f;
            dest.Squeeze = 0f;
            dest.IsTracking = true;
            dest.InnerBrowVertical = source.InnerBrowVertical;
            dest.OuterBrowVertical = source.OuterBrowVertical;
        }
        else
        {
            dest.Openness = 1f;
            dest.Widen = 0f;
            dest.Squeeze = 0f;
            dest.Direction = float3.Forward;
            dest.IsTracking = false;
            dest.InnerBrowVertical = 0f;
            dest.OuterBrowVertical = 0f;
        }
    }


    public void UpdateFace(Mouth mouth)
    {
        mouth.IsDeviceActive = true;
        mouth.IsTracking = true;
        mouth.MouthLeftSmileFrown = faceData.MouthLeftSmileFrown;
        mouth.MouthRightSmileFrown = faceData.MouthRightSmileFrown;
        mouth.MouthLeftDimple = faceData.MouthDimpleLeft;
        mouth.MouthRightDimple = faceData.MouthDimpleRight;
        mouth.CheekLeftPuffSuck = faceData.CheekPuffSuckLeft;
        mouth.CheekRightPuffSuck = faceData.CheekPuffSuckRight;
        mouth.CheekLeftRaise = faceData.CheekSquintLeft;
        mouth.CheekRightRaise = faceData.CheekSquintRight;
        mouth.LipUpperLeftRaise = faceData.MouthUpperUpLeft;
        mouth.LipUpperRightRaise = faceData.MouthUpperUpRight;
        mouth.LipLowerLeftRaise = faceData.MouthLowerDownLeft;
        mouth.LipLowerRightRaise = faceData.MouthLowerDownRight;
        mouth.MouthPoutLeft = faceData.MouthPoutLeft;
        mouth.MouthPoutRight = faceData.MouthPoutRight;
        mouth.LipUpperHorizontal = faceData.MouthUpperX;
        mouth.LipLowerHorizontal = faceData.MouthLowerX;
        mouth.LipTopLeftOverturn = faceData.LipFunnelUpperLeft;
        mouth.LipTopRightOverturn = faceData.LipFunnelUpperRight;
        mouth.LipBottomLeftOverturn = faceData.LipFunnelLowerLeft;
        mouth.LipBottomRightOverturn = faceData.LipFunnelLowerRight;
        mouth.LipTopLeftOverUnder = -faceData.LipSuckUpperLeft;
        mouth.LipTopRightOverUnder = -faceData.LipSuckUpperRight;
        mouth.LipBottomLeftOverUnder = -faceData.LipSuckLowerLeft;
        mouth.LipBottomRightOverUnder = -faceData.LipSuckLowerRight;
        mouth.LipLeftStretchTighten = faceData.LipLeftStretchTighten;
        mouth.LipRightStretchTighten = faceData.LipRightStretchTighten;
        mouth.LipsLeftPress = faceData.MouthPressLeft;
        mouth.LipsRightPress = faceData.MouthPressRight;
        mouth.Jaw = faceData.Jaw;
        mouth.JawOpen = faceData.JawOpen;
        mouth.Tongue = faceData.Tongue;
        mouth.TongueRoll = faceData.TongueRoll;
        mouth.NoseWrinkleLeft = faceData.NoseSneerLeft;
        mouth.NoseWrinkleRight = faceData.NoseSneerRight;
        mouth.ChinRaiseBottom = faceData.MouthRaiserLower;
        mouth.ChinRaiseTop = faceData.MouthRaiserUpper;
    }


    private void OnSettingChanged(object? o)
    {
        bridge.StopListen();
        bridge.TryStartListen();
    }


    private void Shutdown()
    {
        bridge.ReceivedPacket -= OnNewPacket;
        bridge.StopListen();
    }
}
