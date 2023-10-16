using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;

namespace DaveTheMonitor.Achievements
{
    /// <summary>
    /// Extensions to make achievement unlocks easier to write.
    /// </summary>
    public static class AchievementExtensions
    {
        /// <summary>
        /// Returns True if the player has the specified action for an item.
        /// </summary>
        /// <param name="player">The player to test.</param>
        /// <param name="item">The item to get the actions for.</param>
        /// <param name="action">The action to get.</param>
        /// <returns>True if the player has the specified action, otherwise False.</returns>
        public static bool HasAction(this ITMPlayer player, Item item, ItemAction action)
        {
            return player.GetAction(item, action) > 0;
        }
    }
}
