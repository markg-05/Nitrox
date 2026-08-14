using System.Reflection.Emit;
using HarmonyLib;
using Nitrox.Model.Configuration;
using NitroxPatcher.Patches.Dynamic;
using NitroxTest.Patcher;

namespace Nitrox.Test.Patcher.Patches;

[TestClass]
public sealed class ExosuitGrapplingArmPhysicsPatchTest
{
    [TestMethod]
    public void FixedUpdateReplacesOnlyPullAccelerationAndMaxDistance()
    {
        MethodInfo target = GetTargetMethod(typeof(ExosuitGrapplingArm_FixedUpdate_Patch));
        List<CodeInstruction> instructions = PatchTestHelper.GetInstructionsFromMethod(target).ToList();
        List<CodeInstruction> transformed = ExosuitGrapplingArm_FixedUpdate_Patch.Transpiler(instructions.Clone()).ToList();
        MethodInfo pullGetter = GetPatchMethod(typeof(ExosuitGrapplingArm_FixedUpdate_Patch), "GetPullAcceleration");
        MethodInfo distanceGetter = GetPatchMethod(typeof(ExosuitGrapplingArm_FixedUpdate_Patch), "GetMaxDistance");

        CountCalls(transformed, pullGetter).Should().Be(1);
        CountCalls(transformed, distanceGetter).Should().Be(1);
        CountConstants(transformed, PrawnGrapplingArmSettings.VANILLA_PULL_ACCELERATION).Should().Be(0);
        CountConstants(transformed, PrawnGrapplingArmSettings.VANILLA_MAX_DISTANCE).Should().Be(0);
        CountConstants(transformed, 400f).Should().Be(1, "the target reaction force must remain vanilla");
    }

    [TestMethod]
    public void OnHitReplacesBothLaunchSpeedConstantsOnly()
    {
        MethodInfo target = GetTargetMethod(typeof(ExosuitGrapplingArm_OnHit_Patch));
        List<CodeInstruction> instructions = PatchTestHelper.GetInstructionsFromMethod(target).ToList();
        List<CodeInstruction> transformed = ExosuitGrapplingArm_OnHit_Patch.Transpiler(instructions.Clone()).ToList();
        MethodInfo launchGetter = GetPatchMethod(typeof(ExosuitGrapplingArm_OnHit_Patch), "GetLaunchSpeed");

        CountCalls(transformed, launchGetter).Should().Be(2);
        CountConstants(transformed, PrawnGrapplingArmSettings.VANILLA_LAUNCH_SPEED).Should().Be(0);
        CountConstants(transformed, 15f).Should().Be(1, "the grappling sound radius must remain vanilla");
    }

    private static MethodInfo GetTargetMethod(Type patchType)
    {
        return (MethodInfo)patchType.GetField("TARGET_METHOD", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(null)!;
    }

    private static MethodInfo GetPatchMethod(Type patchType, string methodName)
    {
        return patchType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!;
    }

    private static int CountCalls(IEnumerable<CodeInstruction> instructions, MethodInfo method)
    {
        return instructions.Count(instruction => instruction.opcode == OpCodes.Call && Equals(instruction.operand, method));
    }

    private static int CountConstants(IEnumerable<CodeInstruction> instructions, float value)
    {
        return instructions.Count(instruction => instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float actual && actual.Equals(value));
    }
}
