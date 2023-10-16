using HarmonyLib;
using StudioForge.TotalMiner.API;
using System;
using System.Reflection;

namespace DaveTheMonitor.Achievements.Patches
{
    internal static class ArcadeMachinePatch
    {
        private static Type _arcadeGameSelector;

        public static MethodInfo TargetMethod()
        {
            _arcadeGameSelector = AccessTools.TypeByName("StudioForge.TotalMiner.Arcade.ArcadeGameSelector");
            return AccessTools.TypeByName("StudioForge.TotalMiner.ArcadeMachine").GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance);
        }

        public static void Prefix(object __instance, object ___tmPlayer)
        {
            // We only want to unlock the achievement when playing an actual
            // arcade game, not when selecting a game. We can't patch
            // StartGame because it's abstract, this is the next best thing.
            if (__instance.GetType() == _arcadeGameSelector)
            {
                return;
            }

            AchievementsPlugin.Instance.UnlockAchievement((ITMPlayer)___tmPlayer, AchievementsPlugin.Instance.Mod, "Retro");
        }
    }
}
