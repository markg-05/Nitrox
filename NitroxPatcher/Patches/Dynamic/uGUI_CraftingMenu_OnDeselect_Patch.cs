using System.Reflection;
using NitroxClient.MonoBehaviours.Gui.InGame;

namespace NitroxPatcher.Patches.Dynamic;

/// <summary>
/// Hides the quantity selector whenever vanilla closes or deselects the crafting menu.
/// </summary>
public sealed partial class uGUI_CraftingMenu_OnDeselect_Patch : NitroxPatch, IDynamicPatch
{
    private static readonly MethodInfo TARGET_METHOD = Reflect.Method((uGUI_CraftingMenu t) => t.OnDeselect());

    public static void Postfix(uGUI_CraftingMenu __instance)
    {
        if (__instance.TryGetComponent(out FabricatorBatchSelector selector))
        {
            selector.Hide(false);
        }
    }
}
