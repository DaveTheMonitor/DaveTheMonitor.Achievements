using StudioForge.TotalMiner.API;
using System;

namespace DaveTheMonitor.Achievements
{
    /// <summary>
    /// An achievement definition with no player-specific data.
    /// </summary>
    public sealed class Achievement
    {
        /// <summary>
        /// The mod that added this achievement.
        /// </summary>
        public ITMMod Mod { get; private set; }
        /// <summary>
        /// The ID of this achievement.
        /// </summary>
        public string Id { get; private set; }
        /// <summary>
        /// The name of this achievement.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The description of this achievement.
        /// </summary>
        public string Desc { get; private set; }
        /// <summary>
        /// The icon texture used by this achievement.
        /// </summary>
        public string Icon { get; private set; }
        /// <summary>
        /// The background texture used by this achievement.
        /// </summary>
        public string Background { get; private set; }
        /// <summary>
        /// True if this achievement has a progress func, otherwise False.
        /// </summary>
        public bool HasProgress { get; private set; }
        /// <summary>
        /// True if this achievement has an unlock func, otherwise False. If this is False the achievement may still be unlocked with AchievementManager.UnlockAchievement.
        /// </summary>
        public bool HasUnlock { get; private set; }
        private Func<ITMGame, ITMPlayer, bool>[] _unlockFunc;
        private Func<ITMGame, ITMPlayer, (float, string)> _progressFunc;

        /// <summary>
        /// Returns True if th unlock requirement for this achievement has been met, otherwise False.
        /// </summary>
        /// <param name="game">The active game instance.</param>
        /// <param name="player">The player to test the unlock for.</param>
        /// <returns></returns>
        public bool TestUnlock(ITMGame game, ITMPlayer player)
        {
            if (!HasUnlock)
            {
                return false;
            }

            foreach (Func<ITMGame, ITMPlayer, bool> func in _unlockFunc)
            {
                if (func(game, player))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the progress towards unlocking this achievement, from 0-1.
        /// </summary>
        /// <param name="game">The active game instance.</param>
        /// <param name="player">The player to test the progress for.</param>
        /// <param name="text">The progress text. Use this if you need to display the progress in text.</param>
        /// <returns>The progress towards unlocking this achievement.</returns>
        /// <remarks>
        /// <para>Do not use this method to test if an achievement has been unlocked, use TestUnlock instead. This method should only be used if you need to visibly display unlock progress.</para>
        /// </remarks>
        public float GetProgress(ITMGame game, ITMPlayer player, out string text)
        {
            if (!HasProgress)
            {
                text = null;
                return -1;
            }

            (float, string) result = _progressFunc(game, player);
            text = result.Item2;
            text ??= result.Item1.ToString("P");
            return result.Item1;
        }

        /// <summary>
        /// Updates this achievement's data with the specified data.
        /// </summary>
        /// <param name="data">The data to use.</param>
        public void UpdateInfo(AchievementDataXml data)
        {
            if (data.Name != null)
            {
                Name = data.Name;
            }
            if (data.Desc != null)
            {
                Desc = data.Desc;
            }
            if (data.Icon != null)
            {
                Icon = data.Icon;
            }
            if (data.Background != null)
            {
                Background = data.Background;
            }
        }

        /// <summary>
        /// Adds a method to be called to test if this achievement has been unlocked. An achievement can have multiple unlock conditions, only one has to return True to unlock the achievement.
        /// </summary>
        /// <param name="unlockFunc">The function to call.</param>
        public void AddUnlockCondition(Func<ITMGame, ITMPlayer, bool> unlockFunc)
        {
            if (unlockFunc == null)
            {
                throw new ArgumentNullException(nameof(unlockFunc));
            }

            HasUnlock = true;
            if (_unlockFunc == null)
            {
                _unlockFunc = new Func<ITMGame, ITMPlayer, bool>[1]
                {
                    unlockFunc
                };
            }
            else
            {
                int i = _unlockFunc.Length;
                Array.Resize(ref _unlockFunc, _unlockFunc.Length + 1);
                _unlockFunc[i] = unlockFunc;
            }
        }

        /// <summary>
        /// Adds a method to be called to get the unlock progress of this achievement. Currently an achievement can only have one progress func.
        /// </summary>
        /// <param name="progressFunc">The function to call.</param>
        public void AddProgressFunc(Func<ITMGame, ITMPlayer, (float, string)> progressFunc)
        {
            if (progressFunc == null)
            {
                throw new ArgumentNullException(nameof(progressFunc));
            }

            HasProgress = true;
            _progressFunc = progressFunc;
        }

        public Achievement(ITMMod mod, string id, string name, string desc, string icon, string background)
        {
            Mod = mod;
            Id = id;
            Name = name;
            Desc = desc;
            Icon = icon ?? "Default";
            Background = background ?? "Default";
            _unlockFunc = null;
            _progressFunc = null;
        }
    }
}
