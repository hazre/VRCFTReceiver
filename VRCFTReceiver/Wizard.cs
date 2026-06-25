using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;

namespace VRCFTReceiver;

public class Wizard
{
    public static Dictionary<string, float> OscValues = new Dictionary<string, float>();

    public Slot WizardSlot { get; private set; }
    public Slot DataSlot { get; private set; }

    private readonly UIBuilder _ui;

    public Wizard(Slot x)
    {
        WizardSlot = x;

        WizardSlot.AttachComponent<DynamicVariableSpace>().SpaceName.Value = "VRCFT";

        DataSlot = WizardSlot.AddSlot("Data");

        WizardSlot.PersistentSelf = false;
        WizardSlot.LocalScale *= 0.0008f;

        _ui = RadiantUI_Panel.SetupPanel(WizardSlot, "VRCFT Address Debug", new float2(1000f, 756f));
        RadiantUI_Constants.SetupEditorStyle(_ui);

        _ui.Canvas.AcceptPhysicalTouch.Value = false;

        _ui.ScrollArea();
        _ui.VerticalLayout(4f);
        _ui.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
        _ui.Style.MinHeight = 32f;
        _ui.Style.PreferredHeight = 32f;

        x.World.Coroutines.StartTask(ProcessWizard);

        WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);
    }

    private async Task ProcessWizard()
    {
        Dictionary<string, Dictionary<DynamicValueVariable<float>, PrimitiveMemberEditor>> elements = new Dictionary<string, Dictionary<DynamicValueVariable<float>, PrimitiveMemberEditor>>();

        Dictionary<string, float> lastValues = new Dictionary<string, float>();

        while (WizardSlot?.FilterWorldElement() != null)
        {
            await default(ToWorld);

            Dictionary<string, float> snapshot = new Dictionary<string, float>(OscValues);

            foreach (KeyValuePair<string, float> thing in snapshot)
            {
                if (elements.TryGetValue(thing.Key, out Dictionary<DynamicValueVariable<float>, PrimitiveMemberEditor> element))
                {
                    foreach (KeyValuePair<DynamicValueVariable<float>, PrimitiveMemberEditor> thing2 in element)
                    {
                        if (WizardSlot.World.CanCurrentThreadModify)
                        {
                            bool changed = !lastValues.TryGetValue(thing.Key, out float last) || Math.Abs(last - thing.Value) > 0.0001f;

                            thing2.Key.Value.Value = thing.Value;
                            thing2.Value.Slot.Parent[0][0].GetComponent<Text>().Color.Value = changed ? colorX.Green : colorX.Red;

                            lastValues[thing.Key] = thing.Value;
                        }
                    }
                }
                else
                {
                    if (WizardSlot.World.CanCurrentThreadModify)
                    {
                        DynamicValueVariable<float> var = DataSlot.FindChildOrAdd(thing.Key).AttachComponent<DynamicValueVariable<float>>();

                        var.VariableName.Value = DynamicVariableHelper.ProcessName(thing.Key.Split("/")[^1]);

                        elements.Add(thing.Key, new Dictionary<DynamicValueVariable<float>, PrimitiveMemberEditor>
                        {
                            { var, _ui.HorizontalElementWithLabel(thing.Key, 0.4f, () => _ui.PrimitiveMemberEditor(var.Value)) }
                        });

                        lastValues[thing.Key] = thing.Value;
                    }
                }
            }
        }
    }
}
