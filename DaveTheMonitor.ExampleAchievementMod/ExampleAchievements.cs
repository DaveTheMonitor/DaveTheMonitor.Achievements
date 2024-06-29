using Microsoft.Xna.Framework;
using StudioForge.BlockWorld;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace DaveTheMonitor.ExampleAchievementMod
{
    internal sealed class ExampleAchievements
    {
        private ITMMod _mod;
        private ITMGame _game;
        private dynamic _plugin;
        private Action<ITMPlayer, ITMMod, string> _unlockAchievement;

        public void Initialize(ITMGame game, dynamic plugin)
        {
            // We use dynamic as we can't directly reference the Achievements assembly.
            // All public methods on the plugin are available this way.
            _game = game;
            _plugin = plugin;

            // We can get delegates for the achievement-related methods on the plugin and
            // use those instead of dynamic. This can slightly improve performance.
            // If there's a delegate for a method available, it is always a property named
            // {MethodName}Delegate.
            // We can get delegates for:
            // - object AchievementsPlugin.Achievements.GetAchievement(ITMMod, string)
            // - void AchievementsPlugin.Achievements.UnlockAchievement(ITMPlayer, ITMMod, string)
            // - bool AchievementsPlugin.Achievements.IsAchievementUnlocked(ITMPlayer, ITMMod, string)
            // - bool AchievementsPlugin.Achievements.IsAchievementLocked(ITMPlayer, ITMMod, string)
            // - bool AchievementsPlugin.Achievements.HasItemCrafted(ITMPlayer, Item)
            // - int AchievementsPlugin.Achievements.GetItemCrafted(ITMPlayer, Item)
            // - Rectangle AchievementManager.GetIcon(string)
            // - Rectangle AchievementManager.GetBackground(string)
            _unlockAchievement = plugin.Achievements.UnlockAchievementDelegate;

            AddExampleAchievement1(plugin);
            AddExampleAchievement2(game, plugin);
            AddExampleAchievement3(plugin);

            // We need to remove any game/player events when the mod is
            // unloaded/hot loaded. We can add a hot load event through the plugin
            // We can remove events on unload by calling RemoveEvents from
            // ITMplugin.Achievements.UnloadMod.
            plugin.Achievements.AddEventHotReload(new Action(HotReload));
        }

        private void HotReload()
        {
            RemoveEvents();
            // If we were using player events (eg. ITMPlayer.Event_ActorKilled),
            // we would remove them here.
        }

        public void RemoveEvents()
        {
            // Here we want to remove any game events (eg. game.AddEventItemSwing).
            // Player events (eg. ITMPlayer.Event_ActorKilled) only need to removed
            // on hot reload.

            _game.RemoveEventBlockPlaced(Block.LockedChest, PlaceLockedChest);
            // Crafting events don't need to be removed, as they're associated
            // with the plugin which also gets reloaded, not the game instance.
        }

        #region ExampleAchievement1

        private void AddExampleAchievement1(dynamic plugin)
        {
            // ExampleAchievement1 uses a per-update unlock condition. The condition is
            // called periodically. The progress func is called when the Achievements
            // Menu is opened and the achievement is locked.
            // Note: For an actual mod we'd want to use the BlockMined event here,
            // but for this example we're using a per-update condition.
            plugin.Achievements.AddUnlockCondition(_mod, "ExampleAchievement1", new Func<ITMGame, ITMPlayer, bool>(Example1UnlockCondition));
            plugin.Achievements.AddProgressFunc(_mod, "ExampleAchievement1", new Func<ITMGame, ITMPlayer, (float, string)>(Example1Progress));
        }

        private bool Example1UnlockCondition(ITMGame game, ITMPlayer player)
        {
            // This method returns True if the achievement should be unlocked.
            // It will be called periodically until the achievement is unlocked.

            return player.GetAction(Item.Wood, ItemAction.Mined) >= 10;
        }

        private (float, string) Example1Progress(ITMGame game, ITMPlayer player)
        {
            // This method returns the progress towards unlocking the achievement
            // as a tuple.
            // The first item is a float between 0-1, 0 being 0% and 1 being 100%.
            // The second item is the text to display. If null, "{x}%" will be displayed.

            int count = player.GetAction(Item.Wood, ItemAction.Mined);
            if (count >= 10)
            {
                // If the requirement has been met, we return a hardcoded value to
                // prevent percentages over 100%. If the achievement is unlocked
                // using events, this may happen after reloading or if the mod
                // was added after the condition has already been met. Otherwise
                // this situation is unlikely if the achievement is set up
                // properly, but still possible.
                return (1, "10/10");
            }

            return (count / 10f, $"{count}/10");
        }

        #endregion

        #region ExampleAchievement2

        private void AddExampleAchievement2(ITMGame game, dynamic plugin)
        {
            // ExampleAchievement2 uses an game event-based unlock.
            game.AddEventBlockPlaced(Block.LockedChest, PlaceLockedChest);
        }

        private void PlaceLockedChest(Block block, GlobalPoint3D p, ITMHand hand)
        {
            // NPCs can trigger this event, so we want to make sure
            // it's a player triggering it before trying to unlock
            // the achievement.
            if (!hand.Owner.IsPlayer)
            {
                return;
            }

            // Achievements can't be unlocked twice and UnlockAchievement
            // already tests if the achievement is unlocked before trying
            // to unlock it, so calling IsAchievementLocked is unnecessary.
            _unlockAchievement(hand.Player, _mod, "ExampleAchievement2");
        }

        #endregion

        #region ExampleAchievement3

        private void AddExampleAchievement3(dynamic plugin)
        {
            // ExampleAchievement3 uses a crafting based unlock. For these
            // achievements, we want to use plugin.Achievements.AddEventItemCrafted
            // instead of ITMPlayer.Event_ItemCrafted, since that event doesn't
            // currently work properly
            plugin.Achievements.AddEventItemCrafted(new Action<ITMPlayer, Item>(ItemCrafted));
        }

        private void ItemCrafted(ITMPlayer player, Item item)
        {
            if (item == Item.Furnace)
            {
                // Achievements can't be unlocked twice and UnlockAchievement
                // already tests if the achievement is unlocked before trying
                // to unlock it, so calling IsAchievementLocked is unnecessary.
                _unlockAchievement(player, _mod, "ExampleAchievement3");
            }
        }

        #endregion

        public ExampleAchievements(ITMMod mod)
        {
            _mod = mod;
        }
    }
}
