using System;

namespace Nitrox.Model.Configuration;

[Serializable]
public sealed class PrawnGrapplingArmSettings
{
    public const float VANILLA_MAX_DISTANCE = 35f;
    public const float VANILLA_PULL_ACCELERATION = 15f;
    public const float VANILLA_LAUNCH_SPEED = 25f;

    public const float DEFAULT_MAX_DISTANCE = VANILLA_MAX_DISTANCE * 3f;
    public const float DEFAULT_PULL_ACCELERATION = VANILLA_PULL_ACCELERATION * 2f;
    public const float DEFAULT_LAUNCH_SPEED = VANILLA_LAUNCH_SPEED * 2f;

    public float MaxDistance { get; }
    public float PullAcceleration { get; }
    public float LaunchSpeed { get; }

    public PrawnGrapplingArmSettings(float maxDistance, float pullAcceleration, float launchSpeed)
    {
        MaxDistance = maxDistance;
        PullAcceleration = pullAcceleration;
        LaunchSpeed = launchSpeed;
    }
}
