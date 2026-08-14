using System.ComponentModel.DataAnnotations;
using Nitrox.Model.Configuration;

namespace Nitrox.Test.Model.Configuration;

[TestClass]
public sealed class SubnauticaServerOptionsTest
{
    private static readonly string[] GrapplingPropertyNames =
    [
        nameof(SubnauticaServerOptions.PrawnGrapplingArmMaxDistance),
        nameof(SubnauticaServerOptions.PrawnGrapplingArmPullAcceleration),
        nameof(SubnauticaServerOptions.PrawnGrapplingArmLaunchSpeed)
    ];

    [TestMethod]
    public void GrapplingSettingsUseConfiguredMultipliersByDefault()
    {
        SubnauticaServerOptions options = new();

        options.PrawnGrapplingArmMaxDistance.Should().Be(105f);
        options.PrawnGrapplingArmPullAcceleration.Should().Be(30f);
        options.PrawnGrapplingArmLaunchSpeed.Should().Be(50f);
    }

    [TestMethod]
    public void GrapplingSettingsAcceptAnyFinitePositiveValue()
    {
        foreach (string propertyName in GrapplingPropertyNames)
        {
            foreach (float value in new[] { float.Epsilon, 1f, float.MaxValue })
            {
                ValidateProperty(propertyName, value).Should().BeTrue($"{propertyName} should accept {value}");
            }
        }
    }

    [TestMethod]
    public void GrapplingSettingsRejectNonPositiveAndNonFiniteValues()
    {
        foreach (string propertyName in GrapplingPropertyNames)
        {
            foreach (float value in new[] { -1f, 0f, float.NaN, float.NegativeInfinity, float.PositiveInfinity })
            {
                ValidateProperty(propertyName, value).Should().BeFalse($"{propertyName} should reject {value}");
            }
        }
    }

    private static bool ValidateProperty(string propertyName, float value)
    {
        SubnauticaServerOptions options = new();
        PropertyInfo property = typeof(SubnauticaServerOptions).GetProperty(propertyName)!;
        property.SetValue(options, value);
        ValidationContext context = new(options) { MemberName = propertyName };
        return Validator.TryValidateProperty(property.GetValue(options), context, []);
    }
}
