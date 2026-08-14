using Nitrox.Model.Subnautica.Packets;
using NitroxClient.Communication.Packets.Processors.Core;
using NitroxClient.GameLogic;
using NitroxClient.MonoBehaviours;
using UnityEngine;

namespace NitroxClient.Communication.Packets.Processors;

internal sealed class GrapplingHookMovementProcessor(LocalPlayer localPlayer) : IClientPacketProcessor<GrapplingHookMovement>
{
    private readonly LocalPlayer localPlayer = localPlayer;

    public Task Process(ClientProcessorContext context, GrapplingHookMovement packet)
    {
        Exosuit exosuit = NitroxEntity.RequireObjectFrom(packet.ExosuitId).RequireComponent<Exosuit>();
        IExosuitArm arm = packet.ArmSide == Exosuit.Arm.Left ? exosuit.leftArm : exosuit.rightArm;

        if (arm is not ExosuitGrapplingArm grapplingArm)
        {
            Log.Error($"{packet.ArmSide} arm of exosuit {packet.ExosuitId} is not a grappling arm");
            return Task.CompletedTask;
        }

        if (grapplingArm.hook.resting)
        {
            grapplingArm.rope.LaunchHook(localPlayer.PrawnGrapplingArmSettings.MaxDistance);
        }

        Rigidbody rb = grapplingArm.hook.RequireComponent<Rigidbody>();

        rb.position = packet.Position.ToUnity();
        rb.velocity = packet.Velocity.ToUnity();
        rb.rotation = packet.Rotation.ToUnity();
        return Task.CompletedTask;
    }
}
