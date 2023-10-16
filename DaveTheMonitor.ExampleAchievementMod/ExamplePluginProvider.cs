using StudioForge.TotalMiner.API;
using System.Collections.Generic;
using System.Reflection;

namespace DaveTheMonitor.ExampleAchievementMod
{
    internal sealed class ExamplePluginProvider : ITMPluginProvider
    {
        public ITMPlugin GetPlugin() => new ExamplePlugin();
        public ITMPluginArcade GetPluginArcade() => null;
        public ITMPluginBiome GetPluginBiome() => null;
        public ITMPluginBlocks GetPluginBlocks() => null;
        public ITMPluginConfig GetPluginConfig() => null;
        public ITMPluginGUI GetPluginGUI() => null;
        public ITMPluginNet GetPluginNet() => null;
    }
}