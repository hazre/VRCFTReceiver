using System.Reflection;
using ResoniteModLoader;

namespace VRCFTReceiver;

public partial class VRCFTReceiver : ResoniteMod
{
    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<bool> Enabled_Config = new("Enabled", "Enable face & eye tracking?", () => true);
    public static bool Enabled => Config!.GetValue(Enabled_Config);

    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<int> Port_Config = new("Port", "Which port should the OSC data be received from?", () => 9000);
}