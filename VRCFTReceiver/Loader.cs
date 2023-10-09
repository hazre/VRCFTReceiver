using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;

namespace VRCFTReceiver
{
    public class Loader : ResoniteMod
    {

        public static VRCFTOSC OSC = new VRCFTOSC();
        public override string Name => "VRCFTReceiver";
        public override string Author => "hazre";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/hazre/VRCFTReceiver/";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.hazre.VRCFTReceiver");
            harmony.PatchAll();
            OSC.Init();
        }

        public static ValueStream<float> CreateStream(World world, string parameter)
        {
            var stream = world.LocalUser.GetStreamOrAdd<ValueStream<float>>(parameter, stream =>
            {
                stream.Name = parameter;
                //stream.SetInterpolation();
                //((Sync<float>)stream.GetSyncMember("InterpolationOffset")).Value = 0.15f;
                stream.SetUpdatePeriod(0, 0);
                stream.Encoding = ValueEncoding.Quantized;
                stream.FullFrameBits = 10;
                stream.FullFrameMin = -1;
                stream.FullFrameMax = 1;
            });

            var dvslot = world.LocalUser.Root.Slot.FindChildOrAdd("VRCFTReceiver", true);
            CreateVariable(dvslot, parameter, stream);

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
                if (!__instance.ActiveUser.IsLocalUser) return;
                OSC.SendAvatarRequest();

                var dvslot = __instance.Slot.FindChildOrAdd("VRCFTReceiver", true);

                if (VRCFTOSC.VRCFTDictionary.TryGetValue(__instance.World, out var vrcftDictionary)) { 
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