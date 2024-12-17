using ResoniteModLoader;

namespace VRCFTReceiver;

public partial class VRCFTReceiver : ResoniteMod
{
    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<string> IP_Config = new("osc_ip", "IP Address of OSC Server", () => "127.0.0.1");
    public static string IP => Config!.GetValue(IP_Config) ?? "127.0.0.1";

    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<int> Port_Config = new("receiver_port", "Which port should the OSC data be received from?", () => 9000);
    public static int Port => Config!.GetValue(Port_Config);

    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<bool> EnableEyeTracking_Config = new("enable_eye_tracking", "Enable eye tracking?", () => true);
    public static bool EnableEyeTracking => Config!.GetValue(EnableEyeTracking_Config);

    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<bool> EnableFaceTracking_Config = new("enable_face_tracking", "Enable mouth tracking?", () => true);
    public static bool EnableFaceTracking => Config!.GetValue(EnableFaceTracking_Config);

    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<bool> ReverseEyesY_Config = new("reverse_eyes_y", "Reverse eye tracking y direction", () => false);
    public static bool ReverseEyesY => Config!.GetValue(ReverseEyesY_Config);

    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<bool> ReverseEyesX_Config = new("reverse_eyes_x", "Reverse eye tracking x direction", () => false);
    public static bool ReverseEyesX => Config!.GetValue(ReverseEyesX_Config);
}
