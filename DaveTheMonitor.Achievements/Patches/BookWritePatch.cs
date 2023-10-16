using HarmonyLib;
using StudioForge.TotalMiner.API;
using System.Reflection;

namespace DaveTheMonitor.Achievements.Patches
{
    internal static class BookWritePatch
    {
        public static MethodInfo TargetMethod()
        {
            return AccessTools.TypeByName("StudioForge.TotalMiner.Screens.BookOpenScreen").GetMethod("OnTextEntered", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static void Prefix(object ___player)
        {
            AchievementsPlugin.Instance.UnlockAchievement((ITMPlayer)___player, AchievementsPlugin.Instance.Mod, "Author");
        }
    }
}
