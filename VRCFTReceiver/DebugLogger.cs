using Microsoft.VisualBasic;
using ResoniteModLoader;

namespace VRCFTReceiver;

public class DebugLogger
{
    private static bool ShouldDebug => VrcftReceiver.Config.GetValue(VrcftReceiver.DebugLogging);

    public static void Msg(object obj) => Msg(obj?.ToString() ?? "Null");

    public static void Msg(string message)
    {
        if (!ShouldDebug) return;
        ResoniteMod.Msg(message);
    }

    public static void Warn(object obj) => Warn(obj?.ToString() ?? "Null");

    public static void Warn(string message)
    {
        if (!ShouldDebug) return;
        ResoniteMod.Warn(message);
    }

    public static void Error(object obj) => Error(obj?.ToString() ?? "Null");

    public static void Error(string message)
    {
        if (!ShouldDebug) return;
        ResoniteMod.Error(message);
    }

    public static void Debug(object obj) => Debug(obj?.ToString() ?? "Null");

    public static void Debug(string message)
    {
        if (!ShouldDebug) return;
        ResoniteMod.Debug(message);
    }
}