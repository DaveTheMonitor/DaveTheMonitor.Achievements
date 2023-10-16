using HarmonyLib;
using System.Reflection;

namespace DaveTheMonitor.Achievements.Patches
{
    internal static class HotLoadPatch
    {
        public static MethodInfo TargetMethod()
        {
            return AccessTools.TypeByName("StudioForge.TotalMiner.ModManager").GetMethod("HotLoadMods", BindingFlags.Public | BindingFlags.Static);
        }

        public static void Prefix()
        {
            AchievementsPlugin.Instance.HotLoad();
        }
    }
}
