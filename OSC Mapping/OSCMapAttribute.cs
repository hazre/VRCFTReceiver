namespace VRCFTReceiver;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class OSCMapAttribute : Attribute
{
    public OSCMapAttribute(params string[] paths)
    {
        Paths = paths;
    }
    public readonly string[] Paths;
}