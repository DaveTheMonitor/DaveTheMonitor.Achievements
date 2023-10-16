using HarmonyLib;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace DaveTheMonitor.Achievements.Patches
{
    internal static class CraftPatch
    {
        private static MethodBase _dragEndCraftInventory;
        private static MethodBase _dragItemEnd;
        private static MethodBase _craftItem;
        private static MethodBase _burnFurnace;

        public static MethodInfo[] TargetMethods()
        {
            Type type = AccessTools.TypeByName("StudioForge.TotalMiner.Screens2.CraftingScreen");
            _dragEndCraftInventory = type.GetMethod("DragEndCraftInventory", BindingFlags.Public | BindingFlags.Instance);
            _dragItemEnd = type.GetMethod("DragItemEnd", BindingFlags.NonPublic | BindingFlags.Instance);
            _craftItem = type.GetMethod("CraftItem", BindingFlags.NonPublic | BindingFlags.Instance);
            _burnFurnace = AccessTools.Method("StudioForge.TotalMiner.Blocks.FurnaceBlock:BurnFurnace");
            return new MethodInfo[]
            {
                (MethodInfo)_dragEndCraftInventory,
                (MethodInfo)_dragItemEnd,
                (MethodInfo)_craftItem,
                (MethodInfo)_burnFurnace
            };
        }

        public static void Patch(Harmony harmony)
        {
            HarmonyMethod transpiler = new HarmonyMethod(typeof(CraftPatch).GetMethod(nameof(Transpiler), BindingFlags.Public | BindingFlags.Static));
            MethodInfo[] targets = TargetMethods();
            foreach (MethodInfo info in targets)
            {
                harmony.Patch(info, transpiler: transpiler);
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            if (original == _burnFurnace)
            {
                MethodInfo reduceSmeltItems = AccessTools.TypeByName("StudioForge.TotalMiner.Blueprint").GetMethod("ReduceSmeltItems", BindingFlags.Public | BindingFlags.Instance);
                List<CodeInstruction> ops = new List<CodeInstruction>(instructions);
                int index = ops.FindIndex((CodeInstruction op) => op.Calls(reduceSmeltItems));

                if (index != -1)
                {
                    ops.Insert(index++, new CodeInstruction(OpCodes.Ldloc_0));
                    ops.Insert(index++, new CodeInstruction(OpCodes.Ldarg_0));
                    ops.Insert(index++, CodeInstruction.LoadField(original.DeclaringType, "product"));
                    ops.Insert(index++, CodeInstruction.LoadField(AccessTools.TypeByName("StudioForge.TotalMiner.Blueprint"), "Result"));
                    ops.Insert(index++, new CodeInstruction(OpCodes.Call, typeof(CraftPatch).GetMethod(nameof(PreReduce), BindingFlags.NonPublic | BindingFlags.Static)));
                }

                return ops;
            }
            else
            {
                MethodInfo reduceCraftItems = AccessTools.TypeByName("StudioForge.TotalMiner.Blueprint").GetMethod("ReduceCraftItems", BindingFlags.Public | BindingFlags.Instance);
                List<CodeInstruction> ops = new List<CodeInstruction>(instructions);
                int index = ops.FindIndex((CodeInstruction op) => op.Calls(reduceCraftItems));

                if (index != -1)
                {
                    ops.Insert(index++, new CodeInstruction(OpCodes.Ldarg_0));
                    ops.Insert(index++, CodeInstruction.LoadField(original.DeclaringType, "player"));
                    ops.Insert(index++, new CodeInstruction(OpCodes.Ldarg_0));
                    ops.Insert(index++, new CodeInstruction(OpCodes.Callvirt, original.DeclaringType.GetMethod("GetCraftResult", BindingFlags.Public | BindingFlags.Instance)));
                    ops.Insert(index++, new CodeInstruction(OpCodes.Call, typeof(CraftPatch).GetMethod(nameof(PreReduce), BindingFlags.NonPublic | BindingFlags.Static)));
                }

                return ops;
            }
        }

        private static void PreReduce(object player, InventoryItem item)
        {
            if (item.ItemID == Item.None)
            {
                return;
            }
            AchievementsPlugin.Instance.ItemCraft((ITMPlayer)player, item.ItemID);
        }
    }
}
