using Elements.Core;

namespace VRCFTReceiver;

// based on BlueCyro's Impressive code https://github.com/BlueCyro/Impressive
public struct VrcftEye
{
    public readonly bool IsTracking => IsValid && Eyelid > 0.1f;

    public readonly bool IsValid => EyeDirection.Magnitude > 0f && EyeDirection.SqrMagnitude > 0f && EyeDirection.IsValid() && EyeDirection.IsValid() && EyeRotation.IsValid;

    public float3 EyeDirection
    {
        readonly get => EyeRotation * float3.Forward;
        set => EyeRotation = floatQ.LookRotation(EyeDirection);
    }

    public floatQ EyeRotation;

    private float _dirX;
    private float _dirY;

    public float Eyelid;

    public void SetDirectionFromXy(float? x = null, float? y = null)
    {
        _dirX = x ?? _dirX;
        _dirY = y ?? _dirY;

        // Get the angles out of the eye look
        float xAng = MathX.Asin(_dirX);
        float yAng = MathX.Asin(_dirY);

        // Convert to cartesian coordinates
        EyeRotation = floatQ.Euler(yAng * MathX.Rad2Deg, xAng * MathX.Rad2Deg, 0f);
    }
}