using System.Reflection;
using NitroxClient.GameLogic;

namespace NitroxPatcher.Patches.Dynamic;

/// <summary>
/// Starts the next queued recipe only after vanilla has picked up every primary and linked
/// output and fully reset the Fabricator's crafting state.
/// </summary>
public sealed partial class CrafterLogic_ResetCrafter_Patch : NitroxPatch, IDynamicPatch
{
    private static readonly MethodInfo TARGET_METHOD = Reflect.Method((CrafterLogic t) => t.ResetCrafter());

    public static void Postfix(CrafterLogic __instance)
    {
        if (__instance.TryGetComponent(out FabricatorBatchManager manager))
        {
            manager.OnCrafterReset();
        }
    }
}
