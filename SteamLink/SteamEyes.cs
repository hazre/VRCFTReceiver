using System.Runtime.InteropServices;
using Elements.Core;
using ReSounding;

namespace Impressive;
public class SteamEyes
{
    public SteamLinkEye EyeLeft = new();
    public SteamLinkEye EyeRight = new();
    public SteamLinkEye EyeCombined => new()
    {
        Openness = MathX.Max(EyeLeft.Openness, EyeRight.Openness),
        EyeRotation = CombinedEyesDir
    };

    private readonly ReactiveProperty<float> _leftBrowInnerUp = new();
    private readonly ReactiveProperty<float> _leftBrowOuterUp = new();
    private readonly ReactiveProperty<float> _leftBrowLowerer = new();
    private readonly ReactiveProperty<float> _leftBrowPinch = new();

    private readonly ReactiveProperty<float> _rightBrowInnerUp = new();
    private readonly ReactiveProperty<float> _rightBrowOuterUp = new();
    private readonly ReactiveProperty<float> _rightBrowLowerer = new();
    private readonly ReactiveProperty<float> _rightBrowPinch = new();

    public SteamEyes()
    {
        Action leftBrowUpdate = () => UpdateBrows(
            EyeLeft,
            _leftBrowInnerUp,
            _leftBrowOuterUp,
            _leftBrowLowerer,
            _leftBrowPinch
        );

        Action rightBrowUpdate = () => UpdateBrows(
            EyeRight,
            _rightBrowInnerUp,
            _rightBrowOuterUp,
            _rightBrowLowerer,
            _rightBrowPinch
        );

        _leftBrowInnerUp.OnChanged += leftBrowUpdate;
        _leftBrowOuterUp.OnChanged += leftBrowUpdate;
        _leftBrowLowerer.OnChanged += leftBrowUpdate;
        _leftBrowPinch.OnChanged += leftBrowUpdate;

        _rightBrowInnerUp.OnChanged += rightBrowUpdate;
        _rightBrowOuterUp.OnChanged += rightBrowUpdate;
        _rightBrowLowerer.OnChanged += rightBrowUpdate;
        _rightBrowPinch.OnChanged += rightBrowUpdate;
    }

    private static void UpdateBrows(
        SteamLinkEye eye,
        ReactiveProperty<float> browInnerUp,
        ReactiveProperty<float> browOuterUp,
        ReactiveProperty<float> browLowerer,
        ReactiveProperty<float> browPinch)
    {
        float browLowererValue = browPinch - browLowerer;
        eye.InnerBrowVertical = browInnerUp - browLowererValue;
        eye.OuterBrowVertical = browOuterUp - browLowererValue;
    }

    public floatQ CombinedEyesDir
    {
        get
        {
            if (EyeLeft.IsValid && EyeRight.IsValid && EyeLeft.IsTracking && EyeRight.IsTracking)
                _lastValidCombined = MathX.Slerp(EyeLeft.EyeRotation, EyeRight.EyeRotation, 0.5f);
            else if (EyeLeft.IsValid && EyeLeft.IsTracking)
                _lastValidCombined = EyeLeft.EyeRotation;
            else if (EyeRight.IsValid && EyeRight.IsTracking)
                _lastValidCombined = EyeRight.EyeRotation;

            return _lastValidCombined;
        }
    }

    private floatQ _lastValidCombined = floatQ.Identity;

    #region EyesDir

    // Left eye direction
    [OSCMap("/avatar/parameters/v2/EyeLeftX")]
    public float EyeLeftX { set => EyeLeft.SetDirectionFromXY(X: value); }

    [OSCMap("/avatar/parameters/v2/EyeLeftY")]
    public float EyeLeftY { set => EyeLeft.SetDirectionFromXY(Y: value); }

    // Right eye direction
    [OSCMap("/avatar/parameters/v2/EyeRightX")]
    public float EyeRightX { set => EyeRight.SetDirectionFromXY(X: value); }

    [OSCMap("/avatar/parameters/v2/EyeRightY")]
    public float EyeRightY { set => EyeRight.SetDirectionFromXY(Y: value); }

    #endregion


    #region Eyelids

    // Right eyes
    [OSCMap("/avatar/parameters/v2/EyeOpenRight")]
    public float EyeOpenRight { set => EyeRight.Openness = 1f - MathX.Sqrt(value); }

    [OSCMap("/avatar/parameters/v2/EyeWideRight")]
    public float EyeWideRight { set => EyeRight.Widen = value; }

    [OSCMap("/avatar/parameters/v2/EyeSquintRight")]
    public float EyeSquintRight { set => EyeRight.Squeeze = value; }


    // Left eyes
    [OSCMap("/avatar/parameters/v2/EyeOpenLeft")]
    public float EyeOpenLeft { set => EyeLeft.Openness = 1f - MathX.Sqrt(value); }

    [OSCMap("/avatar/parameters/v2/EyeWideLeft")]
    public float EyeWideLeft { set => EyeLeft.Widen = value; }

    [OSCMap("/avatar/parameters/v2/EyeSquintLeft")]
    public float EyeSquintLeft { set => EyeLeft.Squeeze = value; }

    #endregion

    #region Brows

    // Left brow OSC mappings
    [OSCMap("/avatar/parameters/v2/BrowInnerUpLeft")]
    public float BrowInnerUpLeft { set => _leftBrowInnerUp.Value = value; }

    [OSCMap("/avatar/parameters/v2/BrowOuterUpLeft")]
    public float BrowOuterUpLeft { set => _leftBrowOuterUp.Value = value; }

    [OSCMap("/avatar/parameters/v2/BrowLowererLeft")]
    public float BrowLowererLeft { set => _leftBrowLowerer.Value = value; }

    [OSCMap("/avatar/parameters/v2/BrowPinchLeft")]
    public float BrowPinchLeft { set => _leftBrowPinch.Value = value; }

    // Right brow OSC mappings
    [OSCMap("/avatar/parameters/v2/BrowInnerUpRight")]
    public float BrowInnerUpRight { set => _rightBrowInnerUp.Value = value; }

    [OSCMap("/avatar/parameters/v2/BrowOuterUpRight")]
    public float BrowOuterUpRight { set => _rightBrowOuterUp.Value = value; }

    [OSCMap("/avatar/parameters/v2/BrowLowererRight")]
    public float BrowLowererRight { set => _rightBrowLowerer.Value = value; }

    [OSCMap("/avatar/parameters/v2/BrowPinchRight")]
    public float BrowPinchRight { set => _rightBrowPinch.Value = value; }

    #endregion
}

public struct SteamLinkEye
{
    public readonly bool IsTracking => IsValid && Openness > 0.1f;

    public readonly bool IsValid => EyeDirection.Magnitude > 0f && MathX.IsValid(EyeDirection);

    public float3 EyeDirection
    {
        readonly get => EyeRotation * float3.Forward;
        set => EyeRotation = floatQ.LookRotation(EyeDirection);
    }

    public floatQ EyeRotation;
    private float DirX;
    private float DirY;
    public float Openness;
    public float Widen;
    public float Squeeze;
    public float InnerBrowVertical;
    public float OuterBrowVertical;

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

public class ReactiveProperty<T>
{
    private T _value = default!;
    public event Action OnChanged = () => { };

    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                OnChanged?.Invoke();
            }
        }
    }

    public static implicit operator T(ReactiveProperty<T> property) => property.Value;
}
