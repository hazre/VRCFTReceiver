using Elements.Core;
using FrooxEngine;
using System.Collections.Generic;

namespace VRCFTReceiver
{
    // https://github.com/dfgHiatus/VRCFT-Module-Wrapper/blob/master/VRCFTModuleWrapper/EyeDevice.cs
    internal class EyeDevice : IInputDriver
    {
        private Eyes eyes;
        public int UpdateOrder => 100;
        public World focus;
        private Dictionary<string, float> eyeAttributes = new Dictionary<string, float>()
        {
            { "EyeLeftX", 0f },
            { "EyeLeftY", 0f },
            { "EyeOpenLeft", 0f },
            { "EyeWideLeft", 0f },
            { "EyeSquintLeft", 0f },
            { "PupilDilation", 0f },
            { "EyeRightX", 0f },
            { "EyeRightY", 0f },
            { "EyeOpenRight", 0f },
            { "EyeWideRight", 0f },
            { "EyeSquintRight", 0f }
        };

        public void CollectDeviceInfos(DataTreeList list)
        {
            DataTreeDictionary dataTreeDictionary = new DataTreeDictionary();
            dataTreeDictionary.Add("Name", "VRCFT Eye Module");
            dataTreeDictionary.Add("Type", "Eye Tracking");
            dataTreeDictionary.Add("Model", "VRCFT");
            list.Add(dataTreeDictionary);
        }

        public void RegisterInputs(InputInterface inputInterface)
        {
            eyes = new Eyes(inputInterface, "VRCFT Eye Tracking");
        }

        public void TryGetValue(string key)
        {
            try
            {
                if (focus != null && VRCFTOSC.VRCFTDictionary[focus].TryGetValue("/avatar/parameters/FT/v2/" + key, out var stream) && (stream != null))
                {
                    eyeAttributes[key] = stream.Value;
                }
            }
            catch { }

        }

        public void UpdateInputs(float deltaTime)
        {
            UpdateEyesState();
            UpdateEyesPositions();
            eyes.ComputeCombinedEyeParameters();
            eyes.FinishUpdate();
        }

        private void UpdateEyesState()
        {
            eyes.IsEyeTrackingActive = Engine.Current.InputInterface.VR_Active;
            eyes.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
            focus = Engine.Current.WorldManager?.FocusedWorld;
        }

        private void UpdateEyesPositions()
        {
            if (focus != null)
            {
                foreach (var key in eyeAttributes.Keys)
                {
                    TryGetValue(key);
                }

                UpdateEye(
                    eyes.LeftEye,
                    eyeAttributes["EyeLeftX"],
                    eyeAttributes["EyeLeftY"],
                    eyeAttributes["EyeOpenLeft"],
                    eyeAttributes["EyeWideLeft"],
                    eyeAttributes["EyeSquintLeft"],
                    eyeAttributes["PupilDilation"]
                );

                UpdateEye(
                    eyes.RightEye,
                    eyeAttributes["EyeRightX"],
                    eyeAttributes["EyeRightY"],
                    eyeAttributes["EyeOpenRight"],
                    eyeAttributes["EyeWideRight"],
                    eyeAttributes["EyeSquintRight"],
                    eyeAttributes["PupilDilation"]
                );

                UpdateEye(
                    eyes.CombinedEye,
                    (eyeAttributes["EyeLeftX"] + eyeAttributes["EyeRightX"]) / 2,
                    (eyeAttributes["EyeLeftY"] + eyeAttributes["EyeRightY"]) / 2,
                    (eyeAttributes["EyeOpenLeft"] + eyeAttributes["EyeOpenRight"]) / 2,
                    (eyeAttributes["EyeWideLeft"] + eyeAttributes["EyeWideRight"]) / 2,
                    (eyeAttributes["EyeSquintLeft"] + eyeAttributes["EyeSquintRight"]) / 2,
                    eyeAttributes["PupilDilation"]
                );
            }
        }

        private void UpdateEye(
            Eye eye,
            float lookX,
            float lookY,
            float openness,
            float widen,
            float squeeze,
            float dilation
            )
        {
            eye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
            eye.IsTracking = Engine.Current.InputInterface.VR_Active;

            if (eye.IsTracking)
            {
                eye.UpdateWithDirection(Project2DTo3D(lookX, lookY));
                eye.RawPosition = float3.Zero;
                eye.PupilDiameter = dilation;
                eye.Openness = openness;
                eye.Widen = widen;
                eye.Squeeze = squeeze;
                eye.Frown = 0f;
            }
        }

        private float3 Project2DTo3D(float x, float y) => new float3(MathX.Tan(x), MathX.Tan(y), 1f).Normalized;
    }
}
