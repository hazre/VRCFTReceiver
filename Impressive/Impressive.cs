using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using System;
using VRC.OSCQuery;

namespace Impressive;

public partial class Impressive : ResoniteMod
{
    public override string Name => "Impressive";
    public override string Author => "Cyro";
    public override string Version => typeof(Impressive).Assembly.GetName().Version.ToString();
    public override string Link => "https://github.com/RileyGuy/Impressive";
    public static ModConfiguration? Config;
    private static OSCQuery? _oscQuery;

    public override void OnEngineInit()
    {
        Harmony harmony = new("net.Cyro.Impressive");
        Config = GetConfiguration();
        Config?.Save(true);
        harmony.PatchAll();
        Msg("Patched successfully!");
        Engine engine = Engine.Current;
        engine.RunPostInit(() =>
        {
            try
            {
                engine.InputInterface.RegisterInputDriver(new SteamLinkDriver());
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
            // Use a default UDP port, you can make this configurable later
            _oscQuery = new OSCQuery(9001);
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
