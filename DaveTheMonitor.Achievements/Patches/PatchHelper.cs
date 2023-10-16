using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DaveTheMonitor.Achievements.Patches
{
    internal static class PatchHelper
    {
        private static List<PatchInfo> _patches;
        internal static Harmony _harmony;

        static PatchHelper()
        {
            _patches = new List<PatchInfo>();
            _harmony = new Harmony("DaveTheMonitor.Achievements");
        }

        internal static void Patch(Type type)
        {
            MethodInfo target = (MethodInfo)type.GetMethod("TargetMethod", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            HarmonyMethod prefix = null;
            HarmonyMethod postfix = null;
            HarmonyMethod transpiler = null;
            MethodInfo patch = type.GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
            if (patch != null)
            {
                prefix = new HarmonyMethod(patch);
            }
            patch = type.GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
            if (patch != null)
            {
                postfix = new HarmonyMethod(patch);
            }
            patch = type.GetMethod("Transpiler", BindingFlags.Static | BindingFlags.Public);
            if (patch != null)
            {
                transpiler = new HarmonyMethod(patch);
            }
            _harmony.Patch(target, prefix, postfix, transpiler);
        }

        internal static void Unpatch()
        {
            _harmony.UnpatchAll("DaveTheMonitor.Achievements");
        }

        public static PatchInfo[] GetPatchedMethods()
        {
            return _patches.ToArray();
        }
    }
}
