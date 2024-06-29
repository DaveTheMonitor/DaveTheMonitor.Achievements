using Microsoft.Xna.Framework.Graphics;
using StudioForge.BlockWorld;
using StudioForge.TotalMiner.API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DaveTheMonitor.Achievements.Loader
{
    public class LoaderPlugin : ITMPlugin
    {
        public static LoaderPlugin Instance { get; private set; }
        public ITMPlugin TMPlugin { get; private set; }
        public dynamic Achievements => TMPlugin;
        private List<Assembly> _assemblies;
        private List<LoaderAssemblyLoadContext> _contexts;
        private Assembly _achievements;
        private bool _assembliesLoaded;
        private dynamic _pluginDynamic;

        public void Callback(string data, GlobalPoint3D? p, ITMActor actor, ITMActor contextActor) => TMPlugin.Callback(data, p, actor, contextActor);
        public void Draw(ITMPlayer player, ITMPlayer virtualPlayer, Viewport vp) => TMPlugin.Draw(player, virtualPlayer, vp);
        public bool HandleInput(ITMPlayer player) => TMPlugin.HandleInput(player);
        public void Initialize(ITMPluginManager mgr, ITMMod mod)
        {
            Instance = this;

            if (!_assembliesLoaded)
            {
                LoadAssemblies(mod.FullPath);
            }

            Type type = _achievements.GetType("DaveTheMonitor.Achievements.AchievementsPlugin");
            ConstructorInfo ctor = type.GetConstructor(new Type[] { typeof(List<Assembly>) });
            TMPlugin = (ITMPlugin)ctor.Invoke(new object[] { _assemblies });

            TMPlugin.Initialize(mgr, mod);
            _pluginDynamic = TMPlugin;
        }
        public void InitializeGame(ITMGame game) => TMPlugin.InitializeGame(game);
        public void PlayerJoined(ITMPlayer player) => TMPlugin.PlayerJoined(player);
        public void PlayerLeft(ITMPlayer player) => TMPlugin.PlayerLeft(player);
        public object[] RegisterLuaFunctions(ITMScriptInstance si) => TMPlugin.RegisterLuaFunctions(si);
        public void UnloadMod() => TMPlugin.UnloadMod();
        public void Update() => TMPlugin.Update();
        public void Update(ITMPlayer player) => TMPlugin.Update(player);
        public void WorldSaved(int version) => TMPlugin.WorldSaved(version);

        private void LoadAssemblies(string modPath)
        {
            _assemblies = new List<Assembly>();
            _contexts = new List<LoaderAssemblyLoadContext>();

            LoadEmbedded("0Harmony.dll", "0Harmony, Version=2.3.2.0, Culture=neutral, PublicKeyToken=null", false, false);
            _achievements = LoadFile(Path.Combine(modPath, "Vanilla", "DaveTheMonitor.Achievements.dll"), true);

            _assembliesLoaded = true;
        }

        private Assembly LoadEmbedded(string name, string assemblyName, bool collectible, bool forceLoad)
        {
            if (!forceLoad)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly loadedAssembly in assemblies)
                {
                    if (loadedAssembly.FullName == assemblyName)
                    {
                        _assemblies.Add(loadedAssembly);
                        return loadedAssembly;
                    }
                }
            }

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DaveTheMonitor.Achievements.Loader." + name);
            LoaderAssemblyLoadContext context = new LoaderAssemblyLoadContext(assemblyName, collectible);
            _contexts.Add(context);

            Assembly asm = context.LoadFromStream(stream);
            _assemblies.Add(asm);
            return asm;
        }

        private Assembly LoadFile(string file, bool collectible)
        {
            using Stream stream = File.OpenRead(file);
            LoaderAssemblyLoadContext context = new LoaderAssemblyLoadContext(Path.GetFileName(file), collectible);
            Assembly assembly = context.LoadFromStream(stream);
            _contexts.Add(context);
            _assemblies.Add(assembly);
            return assembly;
        }

        internal Assembly Resolve(AssemblyName name)
        {
            foreach (Assembly assembly in _assemblies)
            {
                AssemblyName target = assembly.GetName();
                if (name.Name == target.Name &&
                    name.CultureName == target.CultureName &&
                    name.Version.Major == target.Version.Major &&
                    name.Version <= target.Version)
                {
                    return assembly;
                }
            }
            return null;
        }
    }
}
