using StudioForge.TotalMiner.API;
using System.Collections.Generic;
using System.IO;

namespace DaveTheMonitor.Achievements
{
    internal sealed class AchievementUnlockData
    {
        private struct AchievementUnlock
        {
            public string ModId;
            public string AchievementId;
            public int Day;

            public AchievementUnlock(string modId, string achievmementId, int day)
            {
                ModId = modId;
                AchievementId = achievmementId;
                Day = day;
            }
        }

        private List<AchievementUnlock> _unlocks;

        public List<Achievement> GetUnlockedAchievements(ITMGame game, AchievementManager achievementManager)
        {
            List<Achievement> list = null;
            foreach (AchievementUnlock unlock in _unlocks)
            {
                ITMMod mod = game.GetMod(unlock.ModId);
                if (mod != null)
                {
                    Achievement achievement = achievementManager.GetAchievement(mod, unlock.AchievementId);
                    if (achievement != null)
                    {
                        list ??= new List<Achievement>();
                        list.Add(achievement);
                    }
                }
            }
            return list;
        }

        public void Unlock(Achievement achievement, int day)
        {
            Unlock(achievement.Mod.ID, achievement.Id, day);
        }

        public void Unlock(string modId, string achievementId, int day)
        {
            foreach (AchievementUnlock item in _unlocks)
            {
                if (item.ModId == modId && item.AchievementId == achievementId)
                {
                    return;
                }
            }

            AchievementUnlock unlock = new AchievementUnlock(modId, achievementId, day);
            _unlocks.Add(unlock);
        }

        public void WriteState(BinaryWriter writer)
        {
            List<string> modIds = new List<string>();
            foreach (AchievementUnlock unlock in _unlocks)
            {
                if (!modIds.Contains(unlock.ModId))
                {
                    modIds.Add(unlock.ModId);
                }
            }

            writer.Write(modIds.Count);
            foreach (string id in modIds)
            {
                writer.Write(id);
            }

            writer.Write(_unlocks.Count);
            foreach (AchievementUnlock unlock in _unlocks)
            {
                int index = modIds.IndexOf(unlock.ModId);
                writer.Write(index);
                writer.Write(unlock.AchievementId);
                writer.Write(unlock.Day);
            }
        }

        public void ReadState(BinaryReader reader, int version)
        {
            _unlocks.Clear();

            int count = reader.ReadInt32();
            List<string> modIds = new List<string>();
            for (int i = 0; i < count; i++)
            {
                modIds.Add(reader.ReadString());
            }

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string modId = modIds[reader.ReadInt32()];
                string achievementId = reader.ReadString();
                int day = reader.ReadInt32();
                AchievementUnlock unlock = new AchievementUnlock(modId, achievementId, day);
                _unlocks.Add(unlock);
            }
        }

        public AchievementUnlockData()
        {
            _unlocks = new List<AchievementUnlock>();
        }
    }
}
