using UnityEngine;

namespace NitroxClient.GameLogic;

public sealed class FabricatorBatchManager : MonoBehaviour
{
    private static int startingBatchDepth;

    private readonly FabricatorBatchQueue queue = new();
    private Fabricator fabricator;

    public static bool IsStartingBatch => startingBatchDepth > 0;

    internal static FabricatorBatchValidation GetValidation(TechType recipeType) => FabricatorBatchValidator.Validate(recipeType);

    public static bool StartBatch(uGUI_CraftingMenu menu, Fabricator fabricator, uGUI_CraftingMenu.Node node, int quantity)
    {
        FabricatorBatchValidation validation = FabricatorBatchValidator.Validate(node.techType);
        if (!validation.CanCraft(quantity))
        {
            ShowFailure(validation.Failure);
            return false;
        }

        CrafterLogic crafterLogic = fabricator.logic;
        if (!crafterLogic || crafterLogic.inProgress)
        {
            return false;
        }

        FabricatorBatchManager manager = crafterLogic.gameObject.EnsureComponent<FabricatorBatchManager>();
        if (manager.queue.HasPending)
        {
            return false;
        }

        manager.fabricator = fabricator;
        if (quantity > 1)
        {
            manager.queue.Set(node.techType, quantity - 1);
        }

        startingBatchDepth++;
        try
        {
            // Re-enter the vanilla menu action so its sounds, progress icon and lock/close
            // behavior remain exactly the same for the first recipe instance.
            menu.Action(node);
        }
        finally
        {
            startingBatchDepth--;
        }

        if (crafterLogic.craftingTechType != node.techType || !crafterLogic.inProgress)
        {
            manager.queue.Cancel();
            return false;
        }

        return true;
    }

    public void OnCrafterReset()
    {
        if (!queue.TryGet(out FabricatorBatchQueueEntry entry))
        {
            return;
        }

        if (!fabricator || !fabricator.isActiveAndEnabled)
        {
            queue.Cancel();
            return;
        }

        FabricatorBatchValidation validation = FabricatorBatchValidator.Validate(entry.RecipeType);
        if (!validation.CanCraft(entry.Remaining))
        {
            ShowFailure(validation.Failure);
            queue.Cancel();
            return;
        }

        if (!fabricator.HasEnoughPower())
        {
            ErrorMessage.AddWarning(Language.main.Get("NoPower"));
            queue.Cancel();
            return;
        }

        // Update the queue before re-entering vanilla crafting so even an instant craft
        // cannot observe and start the same queued instance twice.
        queue.MarkOneStarted();
        if (!((ITreeActionReceiver)fabricator).PerformAction(entry.RecipeType))
        {
            queue.Cancel();
        }
    }

    private void OnDisable()
    {
        queue.Cancel();
    }

    internal static void ShowFailure(FabricatorBatchFailure failure)
    {
        string languageKey = failure switch
        {
            FabricatorBatchFailure.InventoryFull => "InventoryFull",
            FabricatorBatchFailure.MissingIngredients => "DontHaveNeededIngredients",
            _ => null
        };

        if (languageKey != null)
        {
            ErrorMessage.AddWarning(Language.main.Get(languageKey));
        }
    }
}
