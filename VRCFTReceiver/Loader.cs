using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using Elements.Core;

namespace VRCFTReceiver
{
    public class Loader : ResoniteMod
    {
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<string> KEY_IP = new("ws_ip", "IP Address of VRCFT OSC Server", () => "127.0.0.1");
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<int> KEY_RECEIVER_PORT = new("receiver_port", "Which port should the OSC data be received from?", () => 9000);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<int> KEY_SENDER_PORT = new("sender_port", "Which port should the OSC parameters message be sent to?", () => 9001);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> ENABLE_EYE_TRACKING = new("enable_eye_tracking", "Enable eye tracking?", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> ENABLE_FACE_TRACKING = new("enable_face_tracking", "Enable face tracking?", () => true);
        public static ModConfiguration config;
        public static VRCFT_Driver VRCFTDriver;
        public override string Name => "VRCFTReceiver";
        public override string Author => "hazre";
        public override string Version => "2.0.0";
        public override string Link => "https://github.com/hazre/VRCFTReceiver/";

        public override void OnEngineInit()
        {
            config = GetConfiguration();
            Harmony harmony = new Harmony("dev.hazre.VRCFTReceiver");
            harmony.PatchAll();

            Engine.Current.OnReady += () =>
            {
                VRCFTDriver = new VRCFT_Driver();
                Engine.Current.InputInterface.RegisterInputDriver(VRCFTDriver);
            };
        }

        [HarmonyPatch(typeof(UserRoot), "OnStart")]
        class VRCFTReceiverPatch
        {
            public static void Postfix(UserRoot __instance)
            {
                Msg($"Starting UserRoot");
                if (!__instance.ActiveUser.IsLocalUser) return;
                if (VRCFTDriver == null) return;
                VRCFTDriver.RequestTrackingData();
            }
        }
    }
}
