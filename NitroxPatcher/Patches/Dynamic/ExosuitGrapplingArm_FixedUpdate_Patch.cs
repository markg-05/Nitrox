using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Nitrox.Model.Configuration;
using Nitrox.Model.DataStructures;
using Nitrox.Model.Subnautica.Packets;
using NitroxClient.Communication.Abstract;
using NitroxClient.GameLogic;
using UnityEngine;

namespace NitroxPatcher.Patches.Dynamic;

public sealed partial class ExosuitGrapplingArm_FixedUpdate_Patch : NitroxPatch, IDynamicPatch
{
    private static readonly MethodInfo TARGET_METHOD = Reflect.Method((ExosuitGrapplingArm t) => t.FixedUpdate());
    private static readonly MethodInfo GET_PULL_ACCELERATION_METHOD = Reflect.Method(() => GetPullAcceleration());
    private static readonly MethodInfo GET_MAX_DISTANCE_METHOD = Reflect.Method(() => GetMaxDistance());

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
               .MatchStartForward(new CodeMatch(OpCodes.Ldc_R4, PrawnGrapplingArmSettings.VANILLA_PULL_ACCELERATION))
               .Set(OpCodes.Call, GET_PULL_ACCELERATION_METHOD)
               .MatchStartForward(new CodeMatch(OpCodes.Ldc_R4, PrawnGrapplingArmSettings.VANILLA_MAX_DISTANCE))
               .Set(OpCodes.Call, GET_MAX_DISTANCE_METHOD)
               .InstructionEnumeration();
    }


    public static void Postfix(ExosuitGrapplingArm __instance)
    {
        if (__instance.exosuit.TryGetIdOrWarn(out NitroxId id) &&
            Resolve<SimulationOwnership>().HasAnyLockType(id) &&
            !__instance.hook.resting)
        {
            Exosuit.Arm armSide = ExosuitModuleEvent.GetArmSide(__instance);
            Rigidbody rb = __instance.hook.RequireComponent<Rigidbody>();
            Resolve<IPacketSender>().Send(new GrapplingHookMovement(id, armSide, rb.position.ToDto(), rb.velocity.ToDto(), rb.rotation.ToDto()));
        }
    }

    internal static float GetPullAcceleration()
    {
        return Resolve<LocalPlayer>().PrawnGrapplingArmSettings.PullAcceleration;
    }

    internal static float GetMaxDistance()
    {
        return Resolve<LocalPlayer>().PrawnGrapplingArmSettings.MaxDistance;
    }
}
