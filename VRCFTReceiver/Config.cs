using System.Reflection;
using ResoniteModLoader;

namespace VRCFTReceiver;

public partial class VRCFTReceiver : ResoniteMod
{
    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<bool> Enabled_Config = new("Enabled", "When checked, enables face & eye tracking", () => true);
    public static bool Enabled => Config!.GetValue(Enabled_Config);

    [AutoRegisterConfigKey]
    internal static ModConfigurationKey<int> Port_Config = new("Port", "The port on which OSC will listen", () => 9000);
}