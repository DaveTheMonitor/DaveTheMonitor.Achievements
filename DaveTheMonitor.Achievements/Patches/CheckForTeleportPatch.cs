using HarmonyLib;
using Microsoft.Xna.Framework;
using StudioForge.TotalMiner.API;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace DaveTheMonitor.Achievements.Patches
{
    internal static class CheckForTeleportPatch
    {
        public static MethodInfo TargetMethod()
        {
            return AccessTools.TypeByName("StudioForge.TotalMiner.Actor").GetMethod("CheckForTeleport", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            MethodInfo teleportTo = original.DeclaringType.GetMethod("TeleportTo", BindingFlags.Public | BindingFlags.Instance, new Type[] { typeof(Vector3) });
            List<CodeInstruction> ops = new List<CodeInstruction>(instructions);
            int index = ops.FindIndex((CodeInstruction op) => op.Calls(teleportTo));

            if (index != -1)
            {
                ops.Insert(index++, new CodeInstruction(OpCodes.Ldarg_0));
                ops.Insert(index++, new CodeInstruction(OpCodes.Call, typeof(CheckForTeleportPatch).GetMethod(nameof(PreTeleport), BindingFlags.NonPublic | BindingFlags.Static)));
            }

            return ops;
        }

        private static void PreTeleport(object actor)
        {
            ITMActor self = (ITMActor)actor;
            if (self.IsPlayer)
            {
                AchievementsPlugin.Instance.UnlockAchievement((ITMPlayer)self, AchievementsPlugin.Instance.Mod, "Portals");
            }
        }
    }
}
