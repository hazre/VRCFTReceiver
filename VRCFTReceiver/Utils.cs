using System;
using System.Linq;

namespace VRCFTReceiver;

internal static class Utils
{
    private const string KChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Random Random = new Random();

    public static string RandomString(int length = 6)
    {
        return new string(Enumerable.Repeat(KChars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
    }
}