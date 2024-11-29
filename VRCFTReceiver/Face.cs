using Elements.Core;

namespace VRCFTReceiver;

public class VRCFTFace
{
    public float MouthLeftSmileFrown => MouthSmileLeft - MouthFrownLeft;
    public float MouthRightSmileFrown => MouthSmileRight - MouthFrownRight;
    public float MouthPoutLeft => LipPuckerUpperLeft - LipPuckerLowerLeft;
    public float MouthPoutRight => LipPuckerUpperRight - LipPuckerLowerRight;
    public float LipLeftStretchTighten => MouthStretchLeft - MouthTightenerLeft;
    public float LipRightStretchTighten => MouthStretchRight - MouthTightenerRight;

    public float3 Jaw => new float3(
        JawRight - JawLeft,
        -MouthClosed,
        JawForward
    );

    public float JawOpenOut => MathX.Clamp01(JawOpen - MouthClosed);

    public float3 Tongue => new float3(
        TongueX,
        TongueY,
        TongueOut
    );

    [OSCMap("/avatar/parameters/v2/CheekPuffSuckLeft")]
    public float CheekPuffSuckLeft;

    [OSCMap("/avatar/parameters/v2/CheekPuffSuckRight")]
    public float CheekPuffSuckRight;

    [OSCMap("/avatar/parameters/v2/CheekSquintLeft")]
    public float CheekSquintLeft;

    [OSCMap("/avatar/parameters/v2/CheekSquintRight")]
    public float CheekSquintRight;

    [OSCMap("/avatar/parameters/v2/MouthDimpleLeft")]
    public float MouthDimpleLeft;

    [OSCMap("/avatar/parameters/v2/MouthDimpleRight")]
    public float MouthDimpleRight;

    [OSCMap("/avatar/parameters/v2/JawForward")]
    public float JawForward;

    [OSCMap("/avatar/parameters/v2/JawLeft")]
    public float JawLeft;

    [OSCMap("/avatar/parameters/v2/JawOpen")]
    public float JawOpen;

    [OSCMap("/avatar/parameters/v2/JawRight")]
    public float JawRight;

    [OSCMap("/avatar/parameters/v2/LipFunnelLowerLeft")]
    public float LipFunnelLowerLeft;

    [OSCMap("/avatar/parameters/v2/LipFunnelLowerRight")]
    public float LipFunnelLowerRight;

    [OSCMap("/avatar/parameters/v2/LipFunnelUpperLeft")]
    public float LipFunnelUpperLeft;

    [OSCMap("/avatar/parameters/v2/LipFunnelUpperRight")]
    public float LipFunnelUpperRight;

    [OSCMap("/avatar/parameters/v2/LipPuckerLowerLeft")]
    public float LipPuckerLowerLeft;

    [OSCMap("/avatar/parameters/v2/LipPuckerLowerRight")]
    public float LipPuckerLowerRight;

    [OSCMap("/avatar/parameters/v2/LipPuckerUpperLeft")]
    public float LipPuckerUpperLeft;

    [OSCMap("/avatar/parameters/v2/LipPuckerUpperRight")]
    public float LipPuckerUpperRight;

    [OSCMap("/avatar/parameters/v2/LipSuckLowerLeft")]
    public float LipSuckLowerLeft;

    [OSCMap("/avatar/parameters/v2/LipSuckLowerRight")]
    public float LipSuckLowerRight;

    [OSCMap("/avatar/parameters/v2/LipSuckUpperLeft")]
    public float LipSuckUpperLeft;

    [OSCMap("/avatar/parameters/v2/LipSuckUpperRight")]
    public float LipSuckUpperRight;

    [OSCMap("/avatar/parameters/v2/MouthClosed")]
    public float MouthClosed;

    [OSCMap("/avatar/parameters/v2/MouthFrownLeft")]
    public float MouthFrownLeft;

    [OSCMap("/avatar/parameters/v2/MouthFrownRight")]
    public float MouthFrownRight;

    [OSCMap("/avatar/parameters/v2/MouthLowerDownLeft")]
    public float MouthLowerDownLeft;

    [OSCMap("/avatar/parameters/v2/MouthLowerDownRight")]
    public float MouthLowerDownRight;

    [OSCMap("/avatar/parameters/v2/MouthLowerX")]
    public float MouthLowerX;

    [OSCMap("/avatar/parameters/v2/MouthPressLeft")]
    public float MouthPressLeft;

    [OSCMap("/avatar/parameters/v2/MouthPressRight")]
    public float MouthPressRight;

    [OSCMap("/avatar/parameters/v2/MouthRaiserLower")]
    public float MouthRaiserLower;

    [OSCMap("/avatar/parameters/v2/MouthRaiserUpper")]
    public float MouthRaiserUpper;

    [OSCMap("/avatar/parameters/v2/MouthSmileLeft")]
    public float MouthSmileLeft;

    [OSCMap("/avatar/parameters/v2/MouthSmileRight")]
    public float MouthSmileRight;

    [OSCMap("/avatar/parameters/v2/MouthStretchLeft")]
    public float MouthStretchLeft;

    [OSCMap("/avatar/parameters/v2/MouthStretchRight")]
    public float MouthStretchRight;

    [OSCMap("/avatar/parameters/v2/MouthTightenerLeft")]
    public float MouthTightenerLeft;

    [OSCMap("/avatar/parameters/v2/MouthTightenerRight")]
    public float MouthTightenerRight;

    [OSCMap("/avatar/parameters/v2/MouthUpperUpLeft")]
    public float MouthUpperUpLeft;

    [OSCMap("/avatar/parameters/v2/MouthUpperUpRight")]
    public float MouthUpperUpRight;

    [OSCMap("/avatar/parameters/v2/MouthUpperX")]
    public float MouthUpperX;

    [OSCMap("/avatar/parameters/v2/NoseSneerLeft")]
    public float NoseSneerLeft;

    [OSCMap("/avatar/parameters/v2/NoseSneerRight")]
    public float NoseSneerRight;

    [OSCMap("/avatar/parameters/v2/TongueOut")]
    public float TongueOut;

    [OSCMap("/avatar/parameters/v2/TongueRoll")]
    public float TongueRoll;

    [OSCMap("/avatar/parameters/v2/TongueX")]
    public float TongueX;

    [OSCMap("/avatar/parameters/v2/TongueY")]
    public float TongueY;
}