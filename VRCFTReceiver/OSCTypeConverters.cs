using Elements.Core;

namespace VRCFTReceiver;
public static class OSCTypeConverters
{
    public static float3 Convert(object[] data)
    {
        return new((float)data[0], (float)data[1], (float)data[2]);
    }
}