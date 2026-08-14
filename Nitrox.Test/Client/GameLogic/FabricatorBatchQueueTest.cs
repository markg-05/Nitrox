using NitroxClient.GameLogic;

namespace Nitrox.Test.Client.GameLogic;

[TestClass]
public sealed class FabricatorBatchQueueTest
{
    [TestMethod]
    public void MarkOneStartedCountsOnlyUnstartedRecipes()
    {
        FabricatorBatchQueue queue = new();
        queue.Set(TechType.Titanium, 2);

        queue.MarkOneStarted();

        queue.TryGet(out FabricatorBatchQueueEntry entry).Should().BeTrue();
        entry.RecipeType.Should().Be(TechType.Titanium);
        entry.Remaining.Should().Be(1);

        queue.MarkOneStarted();
        queue.HasPending.Should().BeFalse();
    }

    [TestMethod]
    public void CancelDropsAllUnstartedRecipes()
    {
        FabricatorBatchQueue queue = new();
        queue.Set(TechType.CopperWire, 4);

        queue.Cancel();

        queue.HasPending.Should().BeFalse();
        queue.TryGet(out _).Should().BeFalse();
    }

    [TestMethod]
    public void SeparateFabricatorQueuesRemainIndependent()
    {
        FabricatorBatchQueue firstFabricator = new();
        FabricatorBatchQueue secondFabricator = new();
        firstFabricator.Set(TechType.Titanium, 2);
        secondFabricator.Set(TechType.CopperWire, 3);

        firstFabricator.MarkOneStarted();

        firstFabricator.TryGet(out FabricatorBatchQueueEntry first).Should().BeTrue();
        secondFabricator.TryGet(out FabricatorBatchQueueEntry second).Should().BeTrue();
        first.Remaining.Should().Be(1);
        second.Remaining.Should().Be(3);
        second.RecipeType.Should().Be(TechType.CopperWire);
    }
}
