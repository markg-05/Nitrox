using System.Reflection;
using NitroxClient.GameLogic;
using NitroxClient.MonoBehaviours.Gui.InGame;

namespace NitroxPatcher.Patches.Dynamic;

/// <summary>
/// Replaces the standard Fabricator recipe click with the batch quantity selector.
/// Other crafting-tree clients, including the Workbench, keep vanilla behavior.
/// </summary>
public sealed partial class uGUI_CraftingMenu_Action_Patch : NitroxPatch, IDynamicPatch
{
    private static readonly MethodInfo TARGET_METHOD = Reflect.Method((uGUI_CraftingMenu t) => t.Action(default));

    public static bool Prefix(uGUI_CraftingMenu __instance, uGUI_CraftingMenu.Node sender)
    {
        if (FabricatorBatchManager.IsStartingBatch || sender.action != TreeAction.Craft || __instance.client is not Fabricator fabricator)
        {
            return true;
        }

        if (!__instance.interactable || !CrafterLogic.IsCraftRecipeUnlocked(sender.techType) || !fabricator.logic || fabricator.logic.inProgress)
        {
            return true;
        }

        __instance.gameObject.EnsureComponent<FabricatorBatchSelector>().Show(__instance, fabricator, sender);
        return false;
    }
}
