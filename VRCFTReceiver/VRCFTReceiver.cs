using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using VRC.OSCQuery;

namespace VRCFTReceiver;

public partial class VRCFTReceiver : ResoniteMod
{
    public override string Name => "VRCFTReceiver";
    public override string Author => "hazre";
    public override string Version => typeof(VRCFTReceiver).Assembly.GetName().Version.ToString();
    public override string Link => "https://github.com/hazre/VRCFTReceiver";
    public static ModConfiguration? Config;
    private static OSCQuery? _oscQuery;

    public override void OnEngineInit()
    {
        Harmony harmony = new("dev.hazre.VRCFTReceiver");
        Config = GetConfiguration();
        Config?.Save(true);
        harmony.PatchAll();
        Msg("Patched successfully!");
        Engine engine = Engine.Current;
        engine.RunPostInit(() =>
        {
            try
            {
                engine.InputInterface.RegisterInputDriver(new VRCFTDriver());
                InitializeOSCQuery();
            }
            catch (Exception ex)
            {
                Msg($"Failed to initialize drivers! Exception: {ex}");
            }
        });
    }

    [HarmonyPatch(typeof(UserRoot), "OnStart")]
    class VRCFTReceiverPatch
    {
        public static void Postfix(UserRoot __instance)
        {
            Msg($"Starting UserRoot");
            if (__instance.ActiveUser.IsLocalUser)
            {
                AvatarChange();
            }
            else
            {
                Warn("Driver is not initialized!");
            };
        }
    }

    private void InitializeOSCQuery()
    {
        try
        {
            var tcpPort = Extensions.GetAvailableTcpPort();
            // since we only receive osc, I think this can be whatever.
            var udpPort = Extensions.GetAvailableUdpPort();
            _oscQuery = new OSCQuery(udpPort, tcpPort);
            Msg("OSCQuery initialized successfully!");
        }
        catch (Exception ex)
        {
            Msg($"Failed to initialize OSCQuery: {ex}");
        }
    }

    public static void AvatarChange()
    {
        if (_oscQuery != null && _oscQuery?.profiles != null)
        {
            foreach (var profile in _oscQuery.profiles)
            {
                if (profile.name.StartsWith("VRCFT"))
                {
                    OSCQuery.SendMessage(profile.address, profile.port, "/avatar/change", "default");
                }
            }
        }
    }
}
