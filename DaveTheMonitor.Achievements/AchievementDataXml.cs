using StudioForge.TotalMiner;

namespace DaveTheMonitor.Achievements
{
    /// <summary>
    /// XML data for and achievement defintion.
    /// </summary>
    public struct AchievementDataXml
    {
        /// <summary>
        /// The ID of the mod that added the achievement. Only used if modifying an existing achievement.
        /// </summary>
        public string Mod;
        /// <summary>
        /// The ID of the achievement.
        /// </summary>
        public string Id;
        /// <summary>
        /// The name of the achievement.
        /// </summary>
        public string Name;
        /// <summary>
        /// The description of the achievement.
        /// </summary>
        public string Desc;
        /// <summary>
        /// The icon used by the achievement.
        /// </summary>
        public string Icon;
        /// <summary>
        /// The background used by the achievement.
        /// </summary>
        public string Background;
        /// <summary>
        /// The gamemode this achievement is present in. Null for all gamemodes.
        /// </summary>
        public GameMode? GameMode;
    }
}
