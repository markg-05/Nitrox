using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Nitrox.Model.Configuration;
using Nitrox.Model.Subnautica.Packets;
using NitroxClient.GameLogic;

namespace NitroxPatcher.Patches.Dynamic;

public sealed partial class ExosuitGrapplingArm_OnHit_Patch : NitroxPatch, IDynamicPatch
{
    public static readonly MethodInfo TARGET_METHOD = Reflect.Method((ExosuitGrapplingArm t) => t.OnHit());

    private static readonly MethodInfo GET_LAUNCH_SPEED_METHOD = Reflect.Method(() => GetLaunchSpeed());
    public static bool Prefix(ExosuitGrapplingArm __instance)
    {
        if (!__instance.exosuit.GetPilotingMode())
        {
            // We suppress this method if it is called from another player pilot, so we can use our own implementation
            return false;
        }

        Resolve<ExosuitModuleEvent>().BroadcastArmAction(TechType.ExosuitGrapplingArmModule, __instance.exosuit, __instance, ExosuitArmAction.START_USE_TOOL);
        return true;
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
               .MatchStartForward(new CodeMatch(OpCodes.Ldc_R4, PrawnGrapplingArmSettings.VANILLA_LAUNCH_SPEED))
               .Repeat(matcher => matcher.Set(OpCodes.Call, GET_LAUNCH_SPEED_METHOD))
               .InstructionEnumeration();
    }

    internal static float GetLaunchSpeed()
    {
        return Resolve<LocalPlayer>().PrawnGrapplingArmSettings.LaunchSpeed;
    }
}
