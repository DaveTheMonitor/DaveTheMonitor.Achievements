using Microsoft.Xna.Framework.Graphics;
using StudioForge.BlockWorld;
using StudioForge.TotalMiner.API;
using System;

namespace DaveTheMonitor.ExampleAchievementMod
{
    public sealed class ExamplePlugin : ITMPlugin
    {
        public ITMMod Mod { get; private set; }
        private ITMGame _game;
        private ExampleAchievements _achievements;

        public void Callback(string data, GlobalPoint3D? p, ITMActor actor, ITMActor contextActor)
        {
            
        }

        public void Draw(ITMPlayer player, ITMPlayer virtualPlayer, Viewport vp)
        {
            
        }

        public bool HandleInput(ITMPlayer player)
        {
            return false;
        }

        public void Initialize(ITMPluginManager mgr, ITMMod mod)
        {
            Mod = mod;
        }

        public void InitializeGame(ITMGame game)
        {
            _game = game;
            if (game.IsModActive("DaveTheMonitor.Achievements"))
            {
                // Here we call any methods we need to add our achievement conditions.
                // For maintainability, anything requiring the Achievements mod is
                // in the ExampleAchievements class, not the plugin.
                ITMMod achievementsMod = game.GetMod("DaveTheMonitor.Achievements");

                _achievements = new ExampleAchievements(Mod);
                _achievements.Initialize(game, achievementsMod.Plugin);
            }
        }

        public void PlayerJoined(ITMPlayer player)
        {
            
        }

        public void PlayerLeft(ITMPlayer player)
        {
            
        }

        public object[] RegisterLuaFunctions(ITMScriptInstance si) => Array.Empty<object>();

        public void UnloadMod()
        {
            _achievements.RemoveEvents();
        }

        public void Update()
        {
            
        }

        public void Update(ITMPlayer player)
        {
            
        }

        public void WorldSaved(int version)
        {
            
        }
    }
}
