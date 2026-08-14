using FluentAssertions;
using UnityEngine;

namespace NitroxPatcher.Patches.Dynamic;

[TestClass]
public sealed class VehicleSpeedBoostTest
{
    [TestMethod]
    public void ActivatesForLocalPilotHoldingSprintAndMovingForward()
    {
        VehicleSpeedBoost.ShouldBoost(true, true, true, Vector3.forward).Should().BeTrue();
    }

    [TestMethod]
    public void RejectsIncompleteBoostInput()
    {
        VehicleSpeedBoost.ShouldBoost(true, true, false, Vector3.forward).Should().BeFalse();
        VehicleSpeedBoost.ShouldBoost(true, true, true, Vector3.zero).Should().BeFalse();
        VehicleSpeedBoost.ShouldBoost(true, true, true, Vector3.back).Should().BeFalse();
        VehicleSpeedBoost.ShouldBoost(true, true, true, Vector3.left).Should().BeFalse();
        VehicleSpeedBoost.ShouldBoost(true, true, true, Vector3.up).Should().BeFalse();
    }

    [TestMethod]
    public void RejectsDisabledInputAndNonLocalPilots()
    {
        VehicleSpeedBoost.ShouldBoost(true, false, true, Vector3.forward).Should().BeFalse();
        VehicleSpeedBoost.ShouldBoost(false, true, true, Vector3.forward).Should().BeFalse();
    }

    [TestMethod]
    public void AppliesConfiguredForceAndRestoresOriginalValue()
    {
        const float originalForce = 12.5f;
        float forwardForce = originalForce;

        float savedForce = VehicleSpeedBoost.ApplyTemporaryMultiplier(ref forwardForce, true, 3f);

        savedForce.Should().Be(originalForce);
        forwardForce.Should().Be(originalForce * 3f);

        VehicleSpeedBoost.Restore(ref forwardForce, savedForce);
        forwardForce.Should().Be(originalForce);
    }

    [TestMethod]
    public void InactiveBoostPreservesForce()
    {
        const float originalForce = 12.5f;
        float forwardForce = originalForce;

        float savedForce = VehicleSpeedBoost.ApplyTemporaryMultiplier(ref forwardForce, false, 3f);

        savedForce.Should().Be(originalForce);
        forwardForce.Should().Be(originalForce);
    }

    [TestMethod]
    public void ConfiguredMultiplierIsBoundedAndHandlesInvalidValues()
    {
        VehicleSpeedBoost.GetMultiplier(0f).Should().Be(1f);
        VehicleSpeedBoost.GetMultiplier(3.5f).Should().Be(3.5f);
        VehicleSpeedBoost.GetMultiplier(25f).Should().Be(20f);
        VehicleSpeedBoost.GetMultiplier(float.NaN).Should().Be(3f);
    }
}
