using System;
using System.Linq;

namespace VRCFTReceiver
{
  static class Utils
  {
    private const string k_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Random Random = new();

    public static string RandomString(int length = 6)
    {
      return new string(Enumerable.Repeat(k_chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
    }
  }
}