using StudioForge.TotalMiner.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DaveTheMonitor.Achievements
{
    internal sealed class AchievementsPluginProvider : ITMPluginProvider
    {
        private List<Assembly> _assemblies;

        public ITMPlugin GetPlugin() => new AchievementsPlugin();
        public ITMPluginArcade GetPluginArcade() => null;
        public ITMPluginBiome GetPluginBiome() => null;
        public ITMPluginBlocks GetPluginBlocks() => null;
        public ITMPluginConfig GetPluginConfig() => null;
        public ITMPluginGUI GetPluginGUI() => null;
        public ITMPluginNet GetPluginNet() => null;


        private void Load(string name, string assemblyName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly loadedAssembly in assemblies)
            {
                if (loadedAssembly.FullName == assemblyName)
                {
                    _assemblies.Add(loadedAssembly);
                    return;
                }
            }

            Stream stream = GetType().Assembly.GetManifestResourceStream("DaveTheMonitor.Achievements.Assembly." + name);
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            Assembly assembly = Assembly.Load(bytes);
            _assemblies.Add(assembly);
        }

        private void LoadEmbeddedAssemblies()
        {
            Load("Mono.Cecil.dll", "Mono.Cecil, Version=0.11.4.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e");
            Load("MonoMod.Common.dll", "MonoMod.Common, Version=22.7.31.1, Culture=neutral, PublicKeyToken=null");
            Load("0Harmony.dll", "0Harmony, Version=2.2.2.0, Culture=neutral, PublicKeyToken=null");
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs e)
        {
            foreach (Assembly assembly in _assemblies)
            {
                AssemblyName target = assembly.GetName();
                AssemblyName name = new AssemblyName(e.Name);
                // Testing (assembly.FullName == e.Name) requires an exact version match
                // Instead we test the name and version here so a later version than targeted can
                // be used. eg. Harmony targets MonoMod.Common v22.6.3.1, but we use v22.7.31.1
                // v22.7.31.1 can still be used by Harmony even though it isn't the exact version
                // because the major version is the same and the minor version is later.
                // This assumes assemblies are using SemVer
                if (name.Name == target.Name &&
                    name.CultureName == target.CultureName &&
                    //name.GetPublicKeyToken() == target.GetPublicKeyToken() &&
                    name.Version.Major == target.Version.Major &&
                    name.Version <= target.Version)
                {
                    return assembly;
                }
            }
            return null;
        }

        public AchievementsPluginProvider()
        {
            _assemblies = new List<Assembly>();
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            LoadEmbeddedAssemblies();
        }
    }
}