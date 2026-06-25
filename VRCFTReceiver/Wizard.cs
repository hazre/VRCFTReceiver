using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteModLoader;

namespace VRCFTReceiver;

public class Wizard
{
    public static Dictionary<string, float> OscValues = new Dictionary<string, float>();

    public Slot Slot { get; }
    public Slot DataSlot { get; }
    public DynamicVariableSpace Space { get; }

    private readonly UIBuilder _ui;

    public Wizard(Slot x)
    {
        Slot = x;
        
        Space = Slot.AttachComponent<DynamicVariableSpace>();
        Space.SpaceName.Value = "VRCFT";
        
        DataSlot = Slot.AddSlot("Data");
        
        Slot.PersistentSelf = false;
        Slot.LocalScale *= 0.0008f;
        
        _ui = RadiantUI_Panel.SetupPanel(Slot, "VRCFT Address Debug", new float2(1000f, 756f));
        RadiantUI_Constants.SetupEditorStyle(_ui);
        
        _ui.Canvas.AcceptPhysicalTouch.Value = false;
        
        _ui.ScrollArea();
        _ui.VerticalLayout(4f);
        _ui.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
        _ui.Style.MinHeight = 32f;
        _ui.Style.PreferredHeight = 32f;
        
        Slot.World.Coroutines.StartTask(ProcessWizard);
    }

    private async Task ProcessWizard()
    {
        await default(ToWorld);
        while (Slot?.FilterWorldElement() != null)
        {
            try
            {
                await default(ToWorld);
                foreach ((string address, float oscValue) in new Dictionary<string, float>(OscValues))
                {
                    string addressName = DynamicVariableHelper.ProcessName(address.Split("/")[^1]);
                    if (Space.TryReadValue(addressName, out float dynValue))
                    {
                        Space.TryWriteValue(addressName, Math.Abs(dynValue - oscValue) > 0.0001f ? colorX.Green : colorX.Red);
                        Space.TryWriteValue(addressName, oscValue);
                    }
                    else
                    {
                        Slot dataSlot = DataSlot.FindChildOrAdd(address);

                        DynamicValueVariable<float> floatVar = dataSlot.AttachComponent<DynamicValueVariable<float>>();
                        floatVar.VariableName.Value = addressName;
                        floatVar.Value.Value = oscValue;

                        DynamicValueVariable<colorX> colorVar = dataSlot.AttachComponent<DynamicValueVariable<colorX>>();
                        colorVar.VariableName.Value = addressName;
                        colorVar.Value.Value = colorX.Red;

                        PrimitiveMemberEditor editor = _ui.HorizontalElementWithLabel(address, 0.4f, () => _ui.PrimitiveMemberEditor(floatVar.Value));
                        editor.Slot.Parent[0][0].GetComponent<Text>().Color.DriveFrom(colorVar.Value);
                    }
                }
                await default(ToBackground);
            }
            catch (Exception e)
            {
                ResoniteMod.Error(e);
            }
        }
    }
}