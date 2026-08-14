using NitroxClient.GameLogic;

namespace Nitrox.Test.Client.GameLogic;

[TestClass]
public sealed class FabricatorQuantitySelectionTest
{
    [TestMethod]
    public void ResetStartsAtOneAndHonorsMaximum()
    {
        FabricatorQuantitySelection selection = new();

        selection.Reset(3);

        selection.Quantity.Should().Be(1);
        selection.CanDecrement.Should().BeFalse();
        selection.CanIncrement.Should().BeTrue();
        selection.CanFabricate.Should().BeTrue();
    }

    [TestMethod]
    public void IncrementAndDecrementClampToBounds()
    {
        FabricatorQuantitySelection selection = new();
        selection.Reset(2);

        selection.Increment();
        selection.Increment();
        selection.Quantity.Should().Be(2);
        selection.CanIncrement.Should().BeFalse();

        selection.Decrement();
        selection.Decrement();
        selection.Quantity.Should().Be(1);
        selection.CanDecrement.Should().BeFalse();
    }

    [TestMethod]
    public void LiveMaximumReductionClampsSelection()
    {
        FabricatorQuantitySelection selection = new();
        selection.Reset(5);
        selection.Increment();
        selection.Increment();
        selection.Increment();

        selection.SetMaximum(2);

        selection.Quantity.Should().Be(2);
        selection.CanIncrement.Should().BeFalse();
        selection.CanFabricate.Should().BeTrue();
    }

    [TestMethod]
    public void ZeroMaximumDisablesFabrication()
    {
        FabricatorQuantitySelection selection = new();

        selection.Reset(0);

        selection.Quantity.Should().Be(1);
        selection.CanIncrement.Should().BeFalse();
        selection.CanFabricate.Should().BeFalse();
    }
}
