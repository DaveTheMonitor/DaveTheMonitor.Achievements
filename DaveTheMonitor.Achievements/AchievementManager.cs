using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StudioForge.Engine;
using StudioForge.Engine.Core;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace DaveTheMonitor.Achievements
{
    /// <summary>
    /// Manages achievement unlocks and save data.
    /// </summary>
    public sealed class AchievementManager
    {
        private struct TextureReplacement
        {
            public string Name;
            public Rectangle Dest;
            public Texture2D Texture;

            public TextureReplacement(string name, Rectangle dest, Texture2D texture)
            {
                Name = name;
                Dest = dest;
                Texture = texture;
            }
        }

        private struct AchievementId
        {
            public ITMMod Mod;
            public string Id;

            public override int GetHashCode()
            {
                return HashCode.Combine(Mod, Id);
            }

            public override bool Equals(object obj)
            {
                if (obj is AchievementId id)
                {
                    return Mod == id.Mod && Id == id.Id;
                }
                else
                {
                    return false;
                }
            }

            public bool Equals(AchievementId id)
            {
                return Mod == id.Mod && Id == id.Id;
            }

            public static bool operator ==(AchievementId left, AchievementId right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(AchievementId left, AchievementId right)
            {
                return left.Mod != right.Mod || left.Id != right.Id;
            }

            public AchievementId(Achievement achievement) : this(achievement.Mod, achievement.Id)
            {

            }

            public AchievementId(ITMMod mod, string id)
            {
                Mod = mod;
                Id = id;
            }
        }

        /// <summary>
        /// A list of all active achievements.
        /// </summary>
        public IReadOnlyList<Achievement> Achievements => _achievements;
        /// <summary>
        /// The texture used for drawing achievement icons. Use this with GetIcon if you need to draw achievement icons.
        /// </summary>
        public Texture2D IconTexture { get; private set; }
        /// <summary>
        /// The texture used for drawing achievement backgrounds. Use this with GetBackground if you need to draw achievement bcakgrounds.
        /// </summary>
        public Texture2D BackgroundTexture { get; private set; }
        /// <summary>
        /// Delegate for GetIcon.
        /// </summary>
        public Func<string, Rectangle> GetIconDelegate;
        /// <summary>
        /// Delegate for GetBackground.
        /// </summary>
        public Func<string, Rectangle> GetBackgroundDelegate;
        private AccessTools.FieldRef<object, SaveMapHead> _header;
        private ITMGame _game;
        private Dictionary<string, Rectangle> _icons;
        private Dictionary<string, Rectangle> _backgrounds;
        private List<Achievement> _achievements;
        private Dictionary<AchievementId, Achievement> _achievementsDictionary;
        private List<Achievement> _lockedAchievements;
        private HashSet<Achievement> _lockedAchievementsHashSet;
        private List<Achievement> _unlockedAchievements;
        private List<AchievementAnimData> _unlockAnim;
        private Dictionary<ulong, AchievementUnlockData> _unlocks;
        private HashSet<ulong> _loadedPlayers;
        private int _index;
        private float _timeSinceUnlock;

        /// <summary>
        /// Gets a list of all unlocked achievements for the specified player.
        /// </summary>
        /// <param name="player">The player to get the achievements for.</param>
        /// <returns>A list of all locked achievements for the specified player.</returns>
        public IReadOnlyList<Achievement> GetUnlockedAchievements(ITMPlayer player)
        {
            return _unlockedAchievements;
        }

        /// <summary>
        /// Gets a list of all locked achievements for the specified player.
        /// </summary>
        /// <param name="player">The player to get the achievements for.</param>
        /// <returns>A list of all locked achievements for the specified player.</returns>
        public IReadOnlyList<Achievement> GetLockedAchievements(ITMPlayer player)
        {
            return _lockedAchievements;
        }

        /// <summary>
        /// Adds a new achievement.
        /// </summary>
        /// <param name="mod">The mod that's adding the achievement.</param>
        /// <param name="id">The ID of the achievement.</param>
        /// <param name="name">The name of the achievement.</param>
        /// <param name="desc">The description of the achievement.</param>
        /// <param name="icon">The icon name of the achievement.</param>
        /// <param name="background">The background name of the achievement.</param>
        /// <returns>The achievement that was added.</returns>
        public Achievement AddAchievement(ITMMod mod, string id, string name, string desc, string icon, string background)
        {
            Achievement achievement = new Achievement(mod, id, name, desc, icon, background);
            AddAchievement(achievement);
            return achievement;
        }

        /// <summary>
        /// Adds a new achievement.
        /// </summary>
        /// <param name="mod">The mod that's adding the achievement.</param>
        /// <param name="data">The data of the achievement.</param>
        /// <returns>The achievement that was added.</returns>
        public Achievement AddAchievement(ITMMod mod, AchievementDataXml data)
        {
            Achievement achievement = new Achievement(mod, data.Id, data.Name, data.Desc, data.Icon, data.Background);
            AddAchievement(achievement);
            return achievement;
        }

        /// <summary>
        /// Adds an achievement.
        /// </summary>
        /// <param name="achievement">The achievement to add.</param>
        public void AddAchievement(Achievement achievement)
        {
            if (_achievementsDictionary.ContainsKey(new AchievementId(achievement)))
            {
                return;
            }

            _achievements.Add(achievement);
            _achievementsDictionary.Add(new AchievementId(achievement), achievement);
            _lockedAchievements.Add(achievement);
            _lockedAchievementsHashSet.Add(achievement);
        }

        /// <summary>
        /// Unlocks the specified achievement if it isn't already unlocked.
        /// </summary>
        /// <param name="player">The player to unlock the achievement for.</param>
        /// <param name="mod">The mod that added the achievement.</param>
        /// <param name="id">The ID of the achievement.</param>
        public void UnlockAchievement(ITMPlayer player, ITMMod mod, string id)
        {
            UnlockAchievement(player, GetAchievement(mod, id));
        }

        /// <summary>
        /// Unlocks the specified achievement if it isn't already unlocked.
        /// </summary>
        /// <param name="player">The player to unlock the achievement for.</param>
        /// <param name="achievement">The achievement to unlock.</param>
        public void UnlockAchievement(ITMPlayer player, Achievement achievement)
        {
            UnlockAchievement(player, achievement, true);
        }

        private void UnlockAchievement(ITMPlayer player, Achievement achievement, bool display)
        {
            if (achievement == null)
            {
                return;
            }

            if (_lockedAchievementsHashSet.Contains(achievement))
            {
                int index = _lockedAchievements.IndexOf(achievement);

                // We decrement the index when the unlocked achievement
                // is before it the loop index to prevent testing the
                // unlock condition for an achievement twice in a loop,
                // and an out of bonuds exception if the achievement
                // is unlocked on the same frame the loop finishes
                if (index <= _index)
                {
                    _index--;
                }
                _lockedAchievements.RemoveAt(index);
                _lockedAchievementsHashSet.Remove(achievement);
                _unlockedAchievements.Add(achievement);

                if (!_unlocks.TryGetValue(player.GamerID.ID, out AchievementUnlockData data))
                {
                    data = new AchievementUnlockData();
                    _unlocks.Add(player.GamerID.ID, data);
                }
                data.Unlock(achievement, _header(_game).DaysIntoGame);

                if (display)
                {
                    _unlockAnim.Add(new AchievementAnimData(achievement, 8, 1));

                    // We don't play the sound unless an achievement hasn't
                    // been unlocked for some time so the sound doesn't play
                    // several times when unlocking multiple achievements at
                    // the same time
                    if (_timeSinceUnlock >= 0.5f)
                    {
                        Sounds.PlaySound(ItemSoundGroup.GuiGamerJoined);
                    }
                    _timeSinceUnlock = 0;
                }
            }
        }

        /// <summary>
        /// Returns True if the specified achievement is unlocked.
        /// </summary>
        /// <param name="player">The player to test the unlock for.</param>
        /// <param name="mod">The mod that added the achievement.</param>
        /// <param name="id">The ID of the achievement.</param>
        /// <returns>True if the achievement is unlocked, otherwise False.</returns>
        public bool IsAchievementUnlocked(ITMPlayer player, ITMMod mod, string id)
        {
            return IsAchievementUnlocked(player, GetAchievement(mod, id));
        }

        /// <summary>
        /// Returns True if the specified achievement is unlocked.
        /// </summary>
        /// <param name="player">The player to test the unlock for.</param>
        /// <param name="achievement">The achievement to test.</param>
        /// <returns>True if the achievement is unlocked, otherwise False.</returns>
        public bool IsAchievementUnlocked(ITMPlayer player, Achievement achievement)
        {
            return _unlockedAchievements.Contains(achievement);
        }

        /// <summary>
        /// Returns True if the specified achievement is locked.
        /// </summary>
        /// <param name="player">The player to test the lock for.</param>
        /// <param name="mod">The mod that added the achievement.</param>
        /// <param name="id">The ID of the achievement.</param>
        /// <returns>True if the achievement is locked, otherwise False.</returns>
        public bool IsAchievementLocked(ITMPlayer player, ITMMod mod, string id)
        {
            return IsAchievementLocked(player, GetAchievement(mod, id));
        }

        /// <summary>
        /// Returns True if the specified achievement is locked.
        /// </summary>
        /// <param name="player">The player to test the lock for.</param>
        /// <param name="achievement">The achievement to test.</param>
        /// <returns>True if the achievement is locked, otherwise False.</returns>
        public bool IsAchievementLocked(ITMPlayer player, Achievement achievement)
        {
            return !IsAchievementUnlocked(player, achievement);
        }

        /// <summary>
        /// Gets the achievement from the specified mod with the specified ID
        /// </summary>
        /// <param name="mod">The mod that added the achievement.</param>
        /// <param name="id">The ID of the achievement.</param>
        /// <returns>The specified achievement, or null if not found.</returns>
        public Achievement GetAchievement(ITMMod mod, string id)
        {
            if (_achievementsDictionary.TryGetValue(new AchievementId(mod, id), out Achievement achievement))
            {
                return achievement;
            }
            return null;
        }

        /// <summary>
        /// Adds a method to be called to test if an achievement has been unlocked to the specified achievement. An achievement can have multiple unlock conditions, only one has to return True to unlock the achievement.
        /// </summary>
        /// <param name="mod">The mod that added the achievement.</param>
        /// <param name="id">The ID of the achievement.</param>
        /// <param name="unlockFunc">The function to call.</param>
        public void AddUnlockCondition(ITMMod mod, string id, Func<ITMGame, ITMPlayer, bool> unlockFunc)
        {
            GetAchievement(mod, id).AddUnlockCondition(unlockFunc);
        }

        /// <summary>
        /// Adds a method to be called to get the unlock progress of the specified achievement. Currently an achievement can only have one progress func.
        /// </summary>
        /// <param name="mod">The mod that added the achievement.</param>
        /// <param name="id">The ID of the achievement.</param>
        /// <param name="progressFunc">The function to call.</param>
        public void AddProgressFunc(ITMMod mod, string id, Func<ITMGame, ITMPlayer, (float, string)> progressFunc)
        {
            GetAchievement(mod, id).AddProgressFunc(progressFunc);
        }

        internal void Draw(Viewport vp)
        {
            SpriteBatchSafe spriteBatch = CoreGlobals.SpriteBatch;
            //if (_iconTexture != null)
            //{
            //    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            //    spriteBatch.Draw(_iconTexture, new Vector2(0, 0), Color.White);
            //    spriteBatch.End();
            //}

            //if (_backgroundTexture != null)
            //{
            //    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            //    spriteBatch.Draw(_backgroundTexture, new Vector2(0, 128), Color.White);
            //    spriteBatch.End();
            //}

            if (_unlockAnim.Count == 0)
            {
                return;
            }

            int count = Math.Min(_unlockAnim.Count, 3);
            int margin = 8;
            int screenPadding = 32;
            int w = 384;
            int h = 64 + 16;
            int x = vp.Width - w - screenPadding;
            int y = (count * (h + margin)) + screenPadding;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            for (int i = count - 1; i >= 0; i--)
            {
                AchievementAnimData anim = _unlockAnim[i];
                y -= h + margin;

                float transparency = 1;
                if (anim.CurrentTime < anim.Transition)
                {
                    transparency = MathHelper.Lerp(0, 1, anim.CurrentTime / anim.Transition);
                }
                else if (anim.CurrentTime > anim.Duration - anim.Transition)
                {
                    transparency = MathHelper.Lerp(1, 0, (1 - (anim.Duration - anim.CurrentTime)) / anim.Transition);
                }

                DrawAchievementBox(spriteBatch, anim.Achievement, x, y, w, h, transparency);

                anim.CurrentTime += Services.ElapsedTime;
                if (anim.CurrentTime >= anim.Duration)
                {
                    _unlockAnim.RemoveAt(i);
                }
                else
                {
                    _unlockAnim[i] = anim;
                }
            }
            spriteBatch.End();
        }

        private void DrawAchievementBox(SpriteBatchSafe spriteBatch, Achievement achievement, int x, int y, int w, int h, float transparency)
        {
            SpriteFont font = CoreGlobals.GameFont16;
            spriteBatch.DrawFilledBox(new Rectangle(x, y, w, h), 2, Color.White * transparency, Color.Black * 0.8f * transparency);
            x += 8;
            y += 8;
            if (achievement.Icon != null)
            {
                Rectangle bg = GetBackground(achievement.Background);
                Rectangle icon = GetIcon(achievement.Icon);
                spriteBatch.Draw(BackgroundTexture, new Rectangle(x, y, 64, 64), bg, Color.White * transparency);
                spriteBatch.Draw(IconTexture, new Rectangle(x, y, 64, 64), icon, Color.White * transparency);
                x += 64 + 8;
            }
            Vector2 measure = font.MeasureString("Achievement Unlocked!");
            spriteBatch.DrawString(font, "Achievement Unlocked!", new Vector2(x, y), Color.Gold * transparency, 0, Vector2.Zero, 1.14f, SpriteEffects.None, 0);
            y += (int)measure.Y;
            spriteBatch.DrawString(font, achievement.Name, new Vector2(x, y), Color.White * transparency, 0, Vector2.Zero, 1.14f, SpriteEffects.None, 0);
        }

        #region Texture Loading

        internal void LoadTextures(string path)
        {
            // Mods can overwrite other mods' textures. This is intentional
            // in case a mod wants to change existing achievement textures
            if (Directory.Exists(Path.Combine(path, @"Textures\Achievements")))
            {
                Dictionary<string, Texture2D> icons = null;
                Dictionary<string, Texture2D> backgrounds = null;
                string iconsPath = Path.Combine(path, @"Textures\Achievements\Icons");
                string bgPath = Path.Combine(path, @"Textures\Achievements\Backgrounds");
                GraphicsDevice graphicsDevice = CoreGlobals.GraphicsDevice;

                if (Directory.Exists(iconsPath))
                {
                    foreach (string file in Directory.GetFiles(iconsPath, "*.*", SearchOption.AllDirectories))
                    {
                        if (!IsValidImageFile(file))
                        {
                            continue;
                        }

                        Texture2D texture = Texture2D.FromFile(graphicsDevice, file);
                        if (texture.Width != 64 || texture.Height != 64)
                        {
                            texture.Dispose();
                            continue;
                        }

                        string name = Path.GetFileNameWithoutExtension(file);
                        if (icons == null)
                        {
                            icons = new Dictionary<string, Texture2D>();
                        }
                        icons.Add(name, texture);
                    }
                }
                if (Directory.Exists(bgPath))
                {
                    foreach (string file in Directory.GetFiles(bgPath, "*.*", SearchOption.AllDirectories))
                    {
                        if (!IsValidImageFile(file))
                        {
                            continue;
                        }

                        Texture2D texture = Texture2D.FromFile(graphicsDevice, file);
                        if (texture.Width != 64 || texture.Height != 64)
                        {
                            texture.Dispose();
                            continue;
                        }

                        string name = Path.GetFileNameWithoutExtension(file);
                        if (backgrounds == null)
                        {
                            backgrounds = new Dictionary<string, Texture2D>();
                        }
                        backgrounds.Add(name, texture);
                    }
                }

                if (icons != null)
                {
                    ExpandIconAtlas(icons, out Dictionary<string, Rectangle> src);
                    foreach (Texture2D texture in icons.Values)
                    {
                        texture.Dispose();
                    }
                    if (src != null)
                    {
                        foreach (KeyValuePair<string, Rectangle> pair in src)
                        {
                            _icons.Add(pair.Key, pair.Value);
                        }
                    }
                }
                if (backgrounds != null)
                {
                    ExpandBackgroundAtlas(backgrounds, out Dictionary<string, Rectangle> src);
                    foreach (Texture2D texture in backgrounds.Values)
                    {
                        texture.Dispose();
                    }
                    if (src != null)
                    {
                        foreach (KeyValuePair<string, Rectangle> pair in src)
                        {
                            _backgrounds.Add(pair.Key, pair.Value);
                        }
                    }
                }
            }
        }

        private bool IsValidImageFile(string file)
        {
            string ext = Path.GetExtension(file);
            if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".tif", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".tiff", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".dds", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private void ExpandIconAtlas(Dictionary<string, Texture2D> textures, out Dictionary<string, Rectangle> src)
        {
            Texture2D atlas = ExpandAtlas(_icons.Count, textures, _icons, IconTexture, out src);
            if (atlas != IconTexture)
            {
                IconTexture?.Dispose();
                IconTexture = atlas;
            }
        }

        private void ExpandBackgroundAtlas(Dictionary<string, Texture2D> textures, out Dictionary<string, Rectangle> src)
        {
            Texture2D atlas = ExpandAtlas(_backgrounds.Count, textures, _backgrounds, BackgroundTexture, out src);
            if (atlas != BackgroundTexture)
            {
                BackgroundTexture?.Dispose();
                BackgroundTexture = atlas;
            }
        }

        private Texture2D ExpandAtlas(int origCount, Dictionary<string, Texture2D> textures, Dictionary<string, Rectangle> src, Texture2D origAtlas, out Dictionary<string, Rectangle> newTexturesSrc)
        {
            List<TextureReplacement> existingTextures = null;
            List<TextureReplacement> newTextures = null;
            newTexturesSrc = null;

            foreach (KeyValuePair<string, Texture2D> pair in textures)
            {
                if (src.TryGetValue(pair.Key, out Rectangle rect))
                {
                    existingTextures ??= new List<TextureReplacement>();
                    existingTextures.Add(new TextureReplacement(pair.Key, rect, pair.Value));
                }
                else
                {
                    newTextures ??= new List<TextureReplacement>();
                    newTextures.Add(new TextureReplacement(pair.Key, Rectangle.Empty, pair.Value));
                }
            }

            Texture2D atlas;
            if (newTextures != null && newTextures.Count > 0)
            {
                int textureSize = 64;
                int maxWidth = 2048;
                int rowLength = maxWidth / textureSize;
                int textureCount = origCount;

                int newWidth = Math.Min((textureCount + newTextures.Count) * textureSize, maxWidth);
                int newHeight = ((int)Math.Floor((double)(textureCount + newTextures.Count - 1) / rowLength) + 1) * textureSize;

                atlas = new Texture2D(CoreGlobals.GraphicsDevice, newWidth, newHeight);
                if (origAtlas != null)
                {
                    Color[] origData = new Color[origAtlas.Width * origAtlas.Height];
                    origAtlas.GetData(origData);
                    atlas.SetData(0, new Rectangle(0, 0, origAtlas.Width, origAtlas.Height), origData, 0, origData.Length);
                }

                int i = textureCount;
                foreach (TextureReplacement texture in newTextures)
                {
                    int x = (i % rowLength) * textureSize;
                    int y = (int)Math.Floor((double)i / (double)rowLength) * textureSize;
                    Rectangle dest = new Rectangle(x, y, textureSize, textureSize);
                    Color[] data = new Color[texture.Texture.Width * texture.Texture.Height];
                    texture.Texture.GetData(data);
                    atlas.SetData(0, dest, data, 0, data.Length);

                    newTexturesSrc ??= new Dictionary<string, Rectangle>();
                    newTexturesSrc.Add(texture.Name, dest);
                    i++;
                }
            }
            else
            {
                atlas = origAtlas;
            }

            if (existingTextures != null && existingTextures.Count > 0)
            {
                foreach (TextureReplacement texture in existingTextures)
                {
                    Color[] data = new Color[texture.Texture.Width * texture.Texture.Height];
                    texture.Texture.GetData(data);
                    atlas.SetData(0, texture.Dest, data, 0, data.Length);
                }
            }

            return atlas;
        }

        #endregion

        internal void LoadAchievements(ITMGame game, ITMMod mod)
        {
            string achievementsPath = Path.Combine(mod.FullPath, "Achievements");
            if (Directory.Exists(achievementsPath))
            {
                foreach (string file in Directory.GetFiles(achievementsPath, "*.*", SearchOption.AllDirectories))
                {
                    if (Deserialize(file, out AchievementDataXml data))
                    {
                        if (data.Id != null)
                        {
                            if (data.Mod != null && data.Mod != mod.ID)
                            {
                                ITMMod targetMod = game.GetMod(data.Mod);
                                if (targetMod != null)
                                {
                                    Achievement achievement = GetAchievement(targetMod, data.Id);
                                    if (achievement != null)
                                    {
                                        achievement.UpdateInfo(data);
                                    }
                                }
                            }
                            else
                            {
                                if (data.GameMode.HasValue && game.World.GameMode != data.GameMode)
                                {
                                    continue;
                                }

                                data.Name ??= data.Id;
                                data.Desc ??= data.Name;
                                data.Icon ??= "Default";
                                data.Background ??= "Default";
                                Achievement achievement = new Achievement(mod, data.Id, data.Name, data.Desc, data.Icon, data.Background);
                                AddAchievement(achievement);
                            }
                        }
                    }
                }
            }
        }

        private bool Deserialize(string file, out AchievementDataXml data)
        {
            using (Stream stream = File.OpenRead(file))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AchievementDataXml));
                    object obj = serializer.Deserialize(stream);
                    if (obj == null)
                    {
                        data = default(AchievementDataXml);
                        return false;
                    }
                    data = (AchievementDataXml)obj;
                    return true;
                }
                catch (Exception e)
                {
                    data = default(AchievementDataXml);
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the texture src Rectangle for the specified icon. Use this with IconTexture if you need to draw an achievement icon.
        /// </summary>
        /// <param name="name">The name of the icon to get.</param>
        /// <returns>The src Rectangle for the specified icon.</returns>
        /// <exception cref="TextureMissingException">Texture does not exist</exception>
        public Rectangle GetIcon(string name)
        {
            if (_icons.TryGetValue(name, out Rectangle src))
            {
                return src;
            }
            throw new TextureMissingException(name);
        }

        /// <summary>
        /// Gets the texture src Rectangle for the specified background. Use this with BackgroundTexture if you need to draw an achievement background.
        /// </summary>
        /// <param name="name">The name of the background to get.</param>
        /// <returns>The src Rectangle for the specified background.</returns>
        /// <exception cref="TextureMissingException">Texture does not exist</exception>
        public Rectangle GetBackground(string name)
        {
            if (_backgrounds.TryGetValue(name, out Rectangle src))
            {
                return src;
            }
            throw new TextureMissingException(name);
        }

        internal void Update(ITMPlayer player)
        {
            UnlockFromData(player);

            if (_lockedAchievements.Count == 0)
            {
                return;
            }

            // We test 3 achievements/update for performance
            // when there's several achievements with possibly expensive
            // unlock methods
            // TODO: test 3 unlocks instead of 3 achievements
            // eg. an achievement with 6 unlock funcs should take 2
            // frames to test
            for (int i = 0; i < 3; i++)
            {
                // TODO: why can _index be < -1?
                if (_index <= -1)
                {
                    _index = _lockedAchievements.Count - 1;
                    break;
                }

                Achievement achievement = _lockedAchievements[_index];
                if (achievement.TestUnlock(_game, player))
                {
                    UnlockAchievement(player, achievement);
                }
                _index--;
            }

            _timeSinceUnlock += Services.ElapsedTime;
        }

        internal void RemovePlayer(ITMPlayer player)
        {
            _loadedPlayers.Remove(player.GamerID.ID);
        }

        private void UnlockFromData(ITMPlayer player)
        {
            if (_loadedPlayers.Add(player.GamerID.ID))
            {
                if (_unlocks.TryGetValue(player.GamerID.ID, out AchievementUnlockData data))
                {
                    List<Achievement> achievements = data.GetUnlockedAchievements(_game, this);
                    if (achievements != null && achievements.Count > 0)
                    {
                        foreach (Achievement achievement in achievements)
                        {
                            UnlockAchievement(player, achievement, false);
                        }
                    }
                }
            }
        }

        internal void Unload()
        {
            IconTexture.Dispose();
            BackgroundTexture.Dispose();
            IconTexture = null;
            BackgroundTexture = null;
        }

        internal void WriteUnlockState(BinaryWriter writer)
        {
            writer.Write(_unlocks.Count);
            foreach (KeyValuePair<ulong, AchievementUnlockData> pair in _unlocks)
            {
                writer.Write(pair.Key);
                pair.Value.WriteState(writer);
            }
        }

        internal void ReadUnlockState(BinaryReader reader, int gameVersion, int version)
        {
            _unlocks.Clear();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                ulong id = reader.ReadUInt64();
                AchievementUnlockData data = new AchievementUnlockData();
                data.ReadState(reader, version);
                _unlocks.Add(id, data);
            }
        }

        /// <summary>
        /// Creates a new AchievementManager.
        /// </summary>
        /// <param name="game">The active game instance.</param>
        public AchievementManager(ITMGame game)
        {
            GetIconDelegate = GetIcon;
            GetBackgroundDelegate = GetBackground;
            _game = game;
            _achievements = new List<Achievement>();
            _achievementsDictionary = new Dictionary<AchievementId, Achievement>();
            _lockedAchievements = new List<Achievement>();
            _lockedAchievementsHashSet = new HashSet<Achievement>();
            _unlockedAchievements = new List<Achievement>();
            _unlockAnim = new List<AchievementAnimData>();
            _icons = new Dictionary<string, Rectangle>();
            _backgrounds = new Dictionary<string, Rectangle>();
            _unlocks = new Dictionary<ulong, AchievementUnlockData>();
            _loadedPlayers = new HashSet<ulong>();
            _index = 0;
            _header = AccessTools.FieldRefAccess<SaveMapHead>(game.GetType(), "Header");
        }
    }
}
