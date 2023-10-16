using Microsoft.Xna.Framework.Input;
using StudioForge.Engine.Core;
using System;
using System.IO;
using System.Xml.Serialization;

namespace DaveTheMonitor.Achievements
{
    /// <summary>
    /// Config for the Achievements mod.
    /// </summary>
    public static class AchievementsConfig
    {
        private struct AchievementConfigXml
        {
            public Keys MenuKey;
        }

        /// <summary>
        /// The key that opens the Achievement Menu.
        /// </summary>
        public static Keys MenuKey { get; set; }

        /// <summary>
        /// Loads a config from the default location.
        /// </summary>
        public static void LoadConfig()
        {
            string configDir = Path.Combine(FileSystem.RootPath, "ModConfig");
            string configFile = Path.Combine(configDir, "DaveTheMonitor.Achievements.xml");
            if (!File.Exists(configFile))
            {
                DefaultConfig();
                return;
            }

            if (Deserialize(configFile, out AchievementConfigXml config))
            {
                MenuKey = config.MenuKey;
            }
            else
            {
                DefaultConfig();
            }
        }

        /// <summary>
        /// Saves the current config to the default location.
        /// </summary>
        public static void SaveConfig()
        {
            string configDir = Path.Combine(FileSystem.RootPath, "ModConfig");
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            string configFile = Path.Combine(configDir, "DaveTheMonitor.Achievements.xml");
            using (Stream stream = File.OpenWrite(configFile))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AchievementConfigXml));
                serializer.Serialize(stream, CreateConfigXml());
            }
        }

        private static void DefaultConfig()
        {
            MenuKey = Keys.F5;
        }

        private static AchievementConfigXml CreateConfigXml()
        {
            return new AchievementConfigXml()
            {
                MenuKey = MenuKey
            };
        }

        private static bool Deserialize(string file, out AchievementConfigXml data)
        {
            using (Stream stream = File.OpenRead(file))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AchievementConfigXml));
                    object obj = serializer.Deserialize(stream);
                    if (obj == null)
                    {
                        data = default(AchievementConfigXml);
                        return false;
                    }
                    data = (AchievementConfigXml)obj;
                    return true;
                }
                catch (Exception e)
                {
                    data = default(AchievementConfigXml);
                    return false;
                }
            }
        }
    }
}
