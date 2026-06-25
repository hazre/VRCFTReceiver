using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using HarmonyLib;
using ResoniteModLoader;

namespace VRCFTReceiver;

public class VrcftReceiver : ResoniteMod
{
    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<string> KeyIp = new ModConfigurationKey<string>("osc_ip", "IP Address of OSC Server", () => "127.0.0.1");

    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<int> KeyReceiverPort = new ModConfigurationKey<int>("receiver_port", "Which port should the OSC data be received from?", () => 9000);

    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<string> AvatarName = new ModConfigurationKey<string>("avatar_name", "The name of the avatar to send to vrcft", () => "default");

    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<int> TrackingTimeoutSeconds = new ModConfigurationKey<int>("tracking_timeout_seconds", "Seconds until tracking is considered inactive, -1 to Disable", () => -1);


    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> EnableEyeTracking = new ModConfigurationKey<bool>("enable_eye_tracking", "Enable eye tracking?", () => true);


    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> EnableFaceTracking = new ModConfigurationKey<bool>("enable_face_tracking", "Enable mouth tracking?", () => true);

    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> ReverseEyesY = new ModConfigurationKey<bool>("reverse_eyes_y", "Reverse eye tracking y direction", () => false);

    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> ReverseEyesX = new ModConfigurationKey<bool>("reverse_eyes_x", "Reverse eye tracking x direction", () => false);


    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> EnablePupilDilation = new ModConfigurationKey<bool>("enable_pupil_dilation", "Enable pupil dilation?", () => true);

    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<float> PupilDilationScale = new ModConfigurationKey<float>("pupil_dialation_scale", "Scale applied to pupil diameter values before sending to Resonite", () => 0.08f);


    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> LogParamErrors = new ModConfigurationKey<bool>("log_param_errors", "Log Param Errors? (Null or Unknown Params)", () => true);

    [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> DebugLogging = new ModConfigurationKey<bool>("debug_logging", "Enable debug Logging", () => false);

    public static ModConfiguration Config;
    public static Driver VrcftDriver;
    public override string Name => "VRCFTReceiver";
    public override string Author => "hazre, ginjake, NepuShiro";
    public override string Version => "2.2.0";
    public override string Link => "https://github.com/hazre/VRCFTReceiver";

    public override void OnEngineInit()
    {
        try
        {
            Config = GetConfiguration();

            Harmony harmony = new Harmony("dev.hazre.VRCFTReceiver");
            harmony.PatchAll();

            Engine engine = Engine.Current;
            if (engine != null)
            {
                engine.RunPostInit(() =>
                {
                    DevCreateNewForm.AddAction("Editor", "VRCFT Address Debug (MOD)", x => _ = new Wizard(x));

                    RegisterDriver(engine);
                });
            }
            else
            {
                ResoniteMod.Error("OnEngineInit failed: Engine.Current is null");
            }
        }
        catch (Exception ex)
        {
            ResoniteMod.Error($"OnEngineInit failed: {ex}");
            throw;
        }
    }

    private static void RegisterDriver(Engine engine)
    {
        try
        {
            if (engine.InputInterface != null)
            {
                VrcftDriver = new Driver();
                engine.InputInterface.RegisterInputDriver(VrcftDriver);
                ResoniteMod.Msg("Driver initialized successfully");
            }
            else
            {
                ResoniteMod.Error("RegisterDriver failed: Engine.InputInterface is null");
            }
        }
        catch (Exception ex)
        {
            ResoniteMod.Error($"Driver initialization failed: {ex}");
        }
    }

    [HarmonyPatch(typeof(UserRoot), "OnStart")]
    private class VrcftReceiverPatch
    {
        public static void Postfix(UserRoot __instance)
        {
            if (__instance.ActiveUser.IsLocalUser)
            {
                VrcftDriver?.AvatarChange();
            }
        }
    }

    [HarmonyPatch(typeof(EyeManager), "UpdateFromEyeTracking")]
    public static class EyeManagerPatch
    {
        public static void Postfix(EyeManager __instance, IEyeDataSourceComponent eyeData, ref bool simulatePupilSize)
        {
            if (VrcftReceiver.Config.GetValue(VrcftReceiver.EnablePupilDilation)) return;
            if (__instance.Slot.ActiveUser == null || !__instance.Slot.ActiveUser.IsLocalUser) return;

            simulatePupilSize = true;
        }
    }
}