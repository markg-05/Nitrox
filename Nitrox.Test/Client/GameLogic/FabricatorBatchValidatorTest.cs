using NitroxClient.GameLogic;

namespace Nitrox.Test.Client.GameLogic;

[TestClass]
public sealed class FabricatorBatchValidatorTest
{
    [TestMethod]
    public void ExactIngredientsDetermineMaximumRecipeInstances()
    {
        FabricatorBatchSnapshot snapshot = Snapshot(
            6,
            6,
            RepeatItem(TechType.Titanium, 5),
            [new FabricatorBatchIngredient(TechType.Titanium, 2)],
            [Size(1, 1)]);

        FabricatorBatchValidation result = FabricatorBatchValidator.Validate(snapshot);

        result.Maximum.Should().Be(2);
        result.Failure.Should().Be(FabricatorBatchFailure.None);
    }

    [TestMethod]
    public void InsufficientMultipliedIngredientsRejectQuantity()
    {
        FabricatorBatchSnapshot snapshot = Snapshot(
            4,
            4,
            RepeatItem(TechType.Titanium, 3),
            [new FabricatorBatchIngredient(TechType.Titanium, 2)],
            [Size(1, 1)]);

        FabricatorBatchValidator.ValidateQuantity(snapshot, 2).Should().Be(FabricatorBatchFailure.MissingIngredients);
    }

    [TestMethod]
    public void CreativeModeIsBoundedByOutputCapacityInsteadOfIngredients()
    {
        FabricatorBatchSnapshot snapshot = Snapshot(
            2,
            2,
            [],
            [new FabricatorBatchIngredient(TechType.Titanium, 99)],
            [Size(1, 1)],
            requiresIngredients: false);

        FabricatorBatchValidator.Validate(snapshot).Maximum.Should().Be(4);
    }

    [TestMethod]
    public void PrimaryCraftAmountAndLinkedOutputsAllConsumeCapacity()
    {
        // Three output entries model a recipe that produces two primary items and one linked item.
        FabricatorBatchSnapshot snapshot = Snapshot(
            3,
            2,
            RepeatItem(TechType.Titanium, 2),
            [new FabricatorBatchIngredient(TechType.Titanium, 1)],
            [Size(1, 1), Size(1, 1), Size(1, 1)]);

        FabricatorBatchValidator.Validate(snapshot).Maximum.Should().Be(2);
    }

    [TestMethod]
    public void ConsumedIngredientsFreeSpaceForOutputs()
    {
        FabricatorBatchSnapshot snapshot = Snapshot(
            2,
            2,
            RepeatItem(TechType.Titanium, 4),
            [new FabricatorBatchIngredient(TechType.Titanium, 2)],
            [Size(2, 1)]);

        FabricatorBatchValidator.Validate(snapshot).Maximum.Should().Be(2);
    }

    [TestMethod]
    public void FragmentedDimensionsUseVanillaPackingRules()
    {
        FabricatorBatchSnapshot snapshot = Snapshot(
            3,
            3,
            [new FabricatorBatchItem(TechType.Copper, Size(2, 2))],
            [],
            [Size(2, 2)],
            requiresIngredients: false);

        FabricatorBatchValidation result = FabricatorBatchValidator.Validate(snapshot);

        result.Maximum.Should().Be(0);
        result.Failure.Should().Be(FabricatorBatchFailure.InventoryFull);
    }

    [TestMethod]
    public void EveryIntermediateRecipeLayoutMustFit()
    {
        FabricatorBatchSnapshot snapshot = Snapshot(
            2,
            2,
            [
                new FabricatorBatchItem(TechType.Titanium, Size(1, 2)),
                new FabricatorBatchItem(TechType.Titanium, Size(1, 2))
            ],
            [new FabricatorBatchIngredient(TechType.Titanium, 1)],
            [Size(2, 1)]);

        // One vertical ingredient plus one horizontal output cannot pack into 2x2, although
        // the theoretical final state of two horizontal outputs would fit.
        FabricatorBatchValidator.ValidateQuantity(snapshot, 2).Should().Be(FabricatorBatchFailure.InventoryFull);
        new ItemsContainer(2, 2, null, string.Empty, null)
            .HasRoomFor([Size(2, 1), Size(2, 1)])
            .Should().BeTrue();
    }

    private static FabricatorBatchSnapshot Snapshot(
        int width,
        int height,
        IReadOnlyList<FabricatorBatchItem> inventory,
        IReadOnlyList<FabricatorBatchIngredient> ingredients,
        IReadOnlyList<Vector2int> outputs,
        bool requiresIngredients = true) =>
        new(width, height, inventory, ingredients, outputs, requiresIngredients);

    private static List<FabricatorBatchItem> RepeatItem(TechType techType, int count) =>
        Enumerable.Range(0, count).Select(_ => new FabricatorBatchItem(techType, Size(1, 1))).ToList();

    private static Vector2int Size(int width, int height) => new(width, height);
}
