using Elements.Core;

namespace VRCFTReceiver
{
  // based on BlueCyro's Impressive code https://github.com/BlueCyro/Impressive
  public struct VRCFTEye
  {
    public readonly bool IsTracking => IsValid && Eyelid > 0.1f;

    public readonly bool IsValid => EyeDirection.Magnitude > 0f && EyeDirection.SqrMagnitude > 0f && MathX.IsValid(EyeDirection) && EyeDirection.IsValid() && EyeRotation != null;

    public float3 EyeDirection
    {
      readonly get => EyeRotation * float3.Forward;
      set => EyeRotation = floatQ.LookRotation(EyeDirection);
    }

    public floatQ EyeRotation;

    private float DirX;
    private float DirY;

    public float Eyelid;

    public void SetDirectionFromXY(float? X = null, float? Y = null)
    {
      DirX = X ?? DirX;
      DirY = Y ?? DirY;

      // Get the angles out of the eye look
      float xAng = MathX.Asin(DirX);
      float yAng = MathX.Asin(DirY);

      // Convert to cartesian coordinates
      EyeRotation = floatQ.Euler(yAng * MathX.Rad2Deg, xAng * MathX.Rad2Deg, 0f);
    }
  }
}