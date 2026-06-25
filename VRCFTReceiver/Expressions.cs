namespace VRCFTReceiver;

public enum ExpressionIndex
{
    BrowInnerUpLeft,
    BrowInnerUpRight,
    BrowLowererLeft,
    BrowLowererRight,
    BrowOuterUpLeft,
    BrowOuterUpRight,
    BrowPinchLeft,
    BrowPinchRight,
    CheekPuffSuckLeft,
    CheekPuffSuckRight,
    CheekSquintLeft,
    CheekSquintRight,
    MouthDimpleLeft,
    MouthDimpleRight,
    EyeLeftX,
    EyeLeftY,
    EyeRightX,
    EyeRightY,
    EyeOpenLeft,
    EyeOpenRight,
    EyeSquintLeft,
    EyeSquintRight,
    EyeWideLeft,
    EyeWideRight,
    PupilDilation,
    PupilDiameterRight,
    PupilDiameterLeft,
    PupilDiameter,
    JawForward,
    JawLeft,
    JawOpen,
    JawRight,
    LipFunnelLowerLeft,
    LipFunnelLowerRight,
    LipFunnelUpperLeft,
    LipFunnelUpperRight,
    LipPuckerLowerLeft,
    LipPuckerLowerRight,
    LipPuckerUpperLeft,
    LipPuckerUpperRight,
    LipSuckLowerLeft,
    LipSuckLowerRight,
    LipSuckUpperLeft,
    LipSuckUpperRight,
    MouthClosed,
    MouthFrownLeft,
    MouthFrownRight,
    MouthLowerDownLeft,
    MouthLowerDownRight,
    MouthLowerX,
    MouthPressLeft,
    MouthPressRight,
    MouthRaiserLower,
    MouthRaiserUpper,
    MouthSmileLeft,
    MouthSmileRight,
    MouthStretchLeft,
    MouthStretchRight,
    MouthTightenerLeft,
    MouthTightenerRight,
    MouthUpperUpLeft,
    MouthUpperUpRight,
    MouthUpperX,
    NoseSneerLeft,
    NoseSneerRight,
    TongueOut,
    TongueRoll,
    TongueX,
    TongueY,
    EyesClosedAmount,
    CenterPitchYaw,
    Count // Used to determine array size
}

public static class Expressions
{
    public const string BrowInnerUpLeft = "/avatar/parameters/FT/v2/BrowInnerUpLeft";
    public const string BrowInnerUpRight = "/avatar/parameters/FT/v2/BrowInnerUpRight";
    public const string BrowLowererLeft = "/avatar/parameters/FT/v2/BrowLowererLeft";
    public const string BrowLowererRight = "/avatar/parameters/FT/v2/BrowLowererRight";
    public const string BrowOuterUpLeft = "/avatar/parameters/FT/v2/BrowOuterUpLeft";
    public const string BrowOuterUpRight = "/avatar/parameters/FT/v2/BrowOuterUpRight";
    public const string BrowPinchLeft = "/avatar/parameters/FT/v2/BrowPinchLeft";
    public const string BrowPinchRight = "/avatar/parameters/FT/v2/BrowPinchRight";
    public const string CheekPuffSuckLeft = "/avatar/parameters/FT/v2/CheekPuffSuckLeft";
    public const string CheekPuffSuckRight = "/avatar/parameters/FT/v2/CheekPuffSuckRight";
    public const string CheekSquintLeft = "/avatar/parameters/FT/v2/CheekSquintLeft";
    public const string CheekSquintRight = "/avatar/parameters/FT/v2/CheekSquintRight";
    public const string MouthDimpleLeft = "/avatar/parameters/FT/v2/MouthDimpleLeft";
    public const string MouthDimpleRight = "/avatar/parameters/FT/v2/MouthDimpleRight";
    public const string EyeLeftX = "/avatar/parameters/FT/v2/EyeLeftX";
    public const string EyeLeftY = "/avatar/parameters/FT/v2/EyeLeftY";
    public const string EyeRightX = "/avatar/parameters/FT/v2/EyeRightX";
    public const string EyeRightY = "/avatar/parameters/FT/v2/EyeRightY";
    public const string EyeOpenLeft = "/avatar/parameters/FT/v2/EyeOpenLeft";
    public const string EyeOpenRight = "/avatar/parameters/FT/v2/EyeOpenRight";
    public const string EyeSquintLeft = "/avatar/parameters/FT/v2/EyeSquintLeft";
    public const string EyeSquintRight = "/avatar/parameters/FT/v2/EyeSquintRight";
    public const string EyeWideLeft = "/avatar/parameters/FT/v2/EyeWideLeft";
    public const string EyeWideRight = "/avatar/parameters/FT/v2/EyeWideRight";
    public const string PupilDilation = "/avatar/parameters/FT/v2/PupilDilation";
    public const string PupilDiameterRight = "/avatar/parameters/FT/v2/PupilDiameterRight";
    public const string PupilDiameterLeft = "/avatar/parameters/FT/v2/PupilDiameterLeft";
    public const string PupilDiameter = "/avatar/parameters/FT/v2/PupilDiameter";
    public const string JawForward = "/avatar/parameters/FT/v2/JawForward";
    public const string JawLeft = "/avatar/parameters/FT/v2/JawLeft";
    public const string JawOpen = "/avatar/parameters/FT/v2/JawOpen";
    public const string JawRight = "/avatar/parameters/FT/v2/JawRight";
    public const string LipFunnelLowerLeft = "/avatar/parameters/FT/v2/LipFunnelLowerLeft";
    public const string LipFunnelLowerRight = "/avatar/parameters/FT/v2/LipFunnelLowerRight";
    public const string LipFunnelUpperLeft = "/avatar/parameters/FT/v2/LipFunnelUpperLeft";
    public const string LipFunnelUpperRight = "/avatar/parameters/FT/v2/LipFunnelUpperRight";
    public const string LipPuckerLowerLeft = "/avatar/parameters/FT/v2/LipPuckerLowerLeft";
    public const string LipPuckerLowerRight = "/avatar/parameters/FT/v2/LipPuckerLowerRight";
    public const string LipPuckerUpperLeft = "/avatar/parameters/FT/v2/LipPuckerUpperLeft";
    public const string LipPuckerUpperRight = "/avatar/parameters/FT/v2/LipPuckerUpperRight";
    public const string LipSuckLowerLeft = "/avatar/parameters/FT/v2/LipSuckLowerLeft";
    public const string LipSuckLowerRight = "/avatar/parameters/FT/v2/LipSuckLowerRight";
    public const string LipSuckUpperLeft = "/avatar/parameters/FT/v2/LipSuckUpperLeft";
    public const string LipSuckUpperRight = "/avatar/parameters/FT/v2/LipSuckUpperRight";
    public const string MouthClosed = "/avatar/parameters/FT/v2/MouthClosed";
    public const string MouthFrownLeft = "/avatar/parameters/FT/v2/MouthFrownLeft";
    public const string MouthFrownRight = "/avatar/parameters/FT/v2/MouthFrownRight";
    public const string MouthLowerDownLeft = "/avatar/parameters/FT/v2/MouthLowerDownLeft";
    public const string MouthLowerDownRight = "/avatar/parameters/FT/v2/MouthLowerDownRight";
    public const string MouthLowerX = "/avatar/parameters/FT/v2/MouthLowerX";
    public const string MouthPressLeft = "/avatar/parameters/FT/v2/MouthPressLeft";
    public const string MouthPressRight = "/avatar/parameters/FT/v2/MouthPressRight";
    public const string MouthRaiserLower = "/avatar/parameters/FT/v2/MouthRaiserLower";
    public const string MouthRaiserUpper = "/avatar/parameters/FT/v2/MouthRaiserUpper";
    public const string MouthSmileLeft = "/avatar/parameters/FT/v2/MouthSmileLeft";
    public const string MouthSmileRight = "/avatar/parameters/FT/v2/MouthSmileRight";
    public const string MouthStretchLeft = "/avatar/parameters/FT/v2/MouthStretchLeft";
    public const string MouthStretchRight = "/avatar/parameters/FT/v2/MouthStretchRight";
    public const string MouthTightenerLeft = "/avatar/parameters/FT/v2/MouthTightenerLeft";
    public const string MouthTightenerRight = "/avatar/parameters/FT/v2/MouthTightenerRight";
    public const string MouthUpperUpLeft = "/avatar/parameters/FT/v2/MouthUpperUpLeft";
    public const string MouthUpperUpRight = "/avatar/parameters/FT/v2/MouthUpperUpRight";
    public const string MouthUpperX = "/avatar/parameters/FT/v2/MouthUpperX";
    public const string NoseSneerLeft = "/avatar/parameters/FT/v2/NoseSneerLeft";
    public const string NoseSneerRight = "/avatar/parameters/FT/v2/NoseSneerRight";
    public const string TongueOut = "/avatar/parameters/FT/v2/TongueOut";
    public const string TongueRoll = "/avatar/parameters/FT/v2/TongueRoll";
    public const string TongueX = "/avatar/parameters/FT/v2/TongueX";
    public const string TongueY = "/avatar/parameters/FT/v2/TongueY";
    public const string EyesClosedAmount = "/tracking/eye/EyesClosedAmount";
    public const string CenterPitchYaw = "/tracking/eye/CenterPitchYaw";

    public static readonly string[] AllAddresses =
    {
        BrowInnerUpLeft, BrowInnerUpRight, BrowLowererLeft, BrowLowererRight,
        BrowOuterUpLeft, BrowOuterUpRight, BrowPinchLeft, BrowPinchRight,
        CheekPuffSuckLeft, CheekPuffSuckRight, CheekSquintLeft, CheekSquintRight,
        MouthDimpleLeft, MouthDimpleRight, EyeLeftX, EyeLeftY, EyeRightX, EyeRightY,
        EyeOpenLeft, EyeOpenRight, EyeSquintLeft, EyeSquintRight, EyeWideLeft, EyeWideRight,
        PupilDilation, PupilDiameterRight, PupilDiameterLeft, PupilDiameter,
        JawForward, JawLeft, JawOpen, JawRight, LipFunnelLowerLeft, LipFunnelLowerRight,
        LipFunnelUpperLeft, LipFunnelUpperRight, LipPuckerLowerLeft, LipPuckerLowerRight,
        LipPuckerUpperLeft, LipPuckerUpperRight, LipSuckLowerLeft, LipSuckLowerRight,
        LipSuckUpperLeft, LipSuckUpperRight, MouthClosed, MouthFrownLeft, MouthFrownRight,
        MouthLowerDownLeft, MouthLowerDownRight, MouthLowerX, MouthPressLeft, MouthPressRight,
        MouthRaiserLower, MouthRaiserUpper, MouthSmileLeft, MouthSmileRight, MouthStretchLeft,
        MouthStretchRight, MouthTightenerLeft, MouthTightenerRight, MouthUpperUpLeft,
        MouthUpperUpRight, MouthUpperX, NoseSneerLeft, NoseSneerRight, TongueOut, TongueRoll,
        TongueX, TongueY, EyesClosedAmount, CenterPitchYaw
    };

    private static readonly ExpressionIndex[] AddressToIndex = new ExpressionIndex[(int)ExpressionIndex.Count];

    static Expressions()
    {
        for (int i = 0; i < AllAddresses.Length; i++)
        {
            AddressToIndex[i] = (ExpressionIndex)i;
        }
    }

    public static ExpressionIndex GetIndex(string address)
    {
        for (int i = 0; i < AllAddresses.Length; i++)
        {
            if (AllAddresses[i] == address)
            {
                return AddressToIndex[i];
            }
        }

        return ExpressionIndex.Count;
    }
}