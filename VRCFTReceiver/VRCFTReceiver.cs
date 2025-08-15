using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using System;
using Elements.Core;

namespace VRCFTReceiver
{
	public class VRCFTReceiver : ResoniteMod
	{
		[AutoRegisterConfigKey]
		public static ModConfigurationKey<string> KEY_IP = new("osc_ip", "IP Address of OSC Server", () => "127.0.0.1");
		[AutoRegisterConfigKey]
		public static ModConfigurationKey<int> KEY_RECEIVER_PORT = new("receiver_port", "Which port should the OSC data be received from?", () => 9000);
		[AutoRegisterConfigKey]
		public static ModConfigurationKey<bool> ENABLE_EYE_TRACKING = new("enable_eye_tracking", "Enable eye tracking?", () => true);
		[AutoRegisterConfigKey]
		public static ModConfigurationKey<bool> ENABLE_FACE_TRACKING = new("enable_face_tracking", "Enable mouth tracking?", () => true);
		[AutoRegisterConfigKey]
		public static ModConfigurationKey<bool> REVERSE_EYES_Y = new("reverse_eyes_y", "Reverse eye tracking y direction", () => false);
		[AutoRegisterConfigKey]
		public static ModConfigurationKey<bool> REVERSE_EYES_X = new("reverse_eyes_x", "Reverse eye tracking x direction", () => false);
		[AutoRegisterConfigKey]
		public static ModConfigurationKey<int> TRACKING_TIMEOUT_SECONDS = new("tracking_timeout_seconds", "Seconds until tracking is considered inactive", () => 5);
		public static ModConfiguration config;
		public static Driver VRCFTDriver;
		public override string Name => "VRCFTReceiver";
		public override string Author => "hazre, ginjake";
		public override string Version => "2.1.0";
		public override string Link => "https://github.com/hazre/VRCFTReceiver";

		public override void OnEngineInit()
		{
			try
			{
				config = GetConfiguration();

				Harmony harmony = new Harmony("dev.hazre.VRCFTReceiver");
				harmony.PatchAll();

				TryDirectDriverInitialization();
			}
			catch (Exception ex)
			{
				UniLog.Error($"[VRCFTReceiver] OnEngineInit failed: {ex}");
				throw;
			}
		}

		[HarmonyPatch(typeof(UserRoot), "OnStart")]
		class VRCFTReceiverPatch
		{
			public static void Postfix(UserRoot __instance)
			{
				if (__instance.ActiveUser.IsLocalUser && VRCFTDriver != null)
				{
					VRCFTDriver.AvatarChange();
				}
			}
		}

		private static void TryDirectDriverInitialization()
		{
			try
			{
				var engine = Engine.Current;

				if (engine?.InputInterface != null && VRCFTDriver == null)
				{
					VRCFTDriver = new Driver();
					engine.InputInterface.RegisterInputDriver(VRCFTDriver);
					UniLog.Log("[VRCFTReceiver] Driver initialized successfully");
				}
			}
			catch (Exception ex)
			{
				UniLog.Error($"[VRCFTReceiver] Driver initialization failed: {ex}");
			}
		}
	}
}
