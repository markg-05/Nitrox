namespace NitroxClient.GameLogic;

internal readonly record struct FabricatorBatchQueueEntry(TechType RecipeType, int Remaining);

internal sealed class FabricatorBatchQueue
{
    private FabricatorBatchQueueEntry entry;

    public bool HasPending => entry.Remaining > 0;

    public void Set(TechType recipeType, int remaining)
    {
        entry = remaining > 0 ? new FabricatorBatchQueueEntry(recipeType, remaining) : default;
    }

    public bool TryGet(out FabricatorBatchQueueEntry queuedEntry)
    {
        queuedEntry = entry;
        return HasPending;
    }

    public void MarkOneStarted()
    {
        if (!HasPending)
        {
            return;
        }
        Set(entry.RecipeType, entry.Remaining - 1);
    }

    public void Cancel() => entry = default;
}
