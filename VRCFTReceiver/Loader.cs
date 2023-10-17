using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;

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
        public static ModConfigurationKey<int> KEY_TIMEOUT = new("ws_timeout", "OSC Receiving Timeout (in ms).", () => 10_000);
        public static ModConfiguration config;
        public static VRCFTOSC OSC = new VRCFTOSC();

        public override string Name => "VRCFTReceiver";
        public override string Author => "hazre";
        public override string Version => "1.0.3";
        public override string Link => "https://github.com/hazre/VRCFTReceiver/";

        public override void OnEngineInit()
        {
            config = GetConfiguration(); // Initialize config here
            Harmony harmony = new Harmony("me.hazre.VRCFTReceiver");
            harmony.PatchAll();

            Engine.Current.OnReady += () =>
            {
                OSC.Init(config.GetValue(KEY_IP), config.GetValue(KEY_RECEIVER_PORT), config.GetValue(KEY_TIMEOUT));
            };
        }

        public static ValueStream<float> CreateStream(World world, string parameter)
        {
            var stream = world.LocalUser.GetStreamOrAdd<ValueStream<float>>(parameter, stream =>
            {
                stream.Name = parameter;
                stream.SetUpdatePeriod(0, 0);
                stream.Encoding = ValueEncoding.Quantized;
                stream.FullFrameBits = 10;
                stream.FullFrameMin = -1;
                stream.FullFrameMax = 1;
            });

            if (world.LocalUser.Root != null)
            {
                var dvslot = world.LocalUser.Root.Slot.FindChildOrAdd("VRCFTReceiver", true);
                CreateVariable(dvslot, parameter, stream);
            }

            return stream;
        }

        public static void CreateVariable(Slot dvslot, string parameter, ValueStream<float> stream)
        {
            var dv = dvslot.AttachComponent<DynamicValueVariable<float>>();
            dv.VariableName.Value = "User/" + parameter;
            var dvdriver = dvslot.AttachComponent<ValueDriver<float>>();
            dvdriver.ValueSource.Target = stream;
            dvdriver.DriveTarget.Target = dv.Value;
        }

        [HarmonyPatch(typeof(UserRoot), "OnStart")]
        class VRCFTReceiverPatch
        {
            public static void Postfix(UserRoot __instance)
            {
                Loader.Msg("Starting UserRoot");
                if (!__instance.ActiveUser.IsLocalUser) return;
                OSC.SendAvatarRequest(config.GetValue(KEY_IP), config.GetValue(KEY_SENDER_PORT));

                var dvslot = __instance.Slot.FindChildOrAdd("VRCFTReceiver", true);

                if (VRCFTOSC.VRCFTDictionary.TryGetValue(__instance.World, out var vrcftDictionary))
                {
                    foreach (var pair in vrcftDictionary)
                    {
                        var parameter = pair.Key.Substring(pair.Key.LastIndexOf('/') + 1);
                        var stream = pair.Value;
                        CreateVariable(dvslot, parameter, stream);
                    }
                }
            }
        }
    }
}
