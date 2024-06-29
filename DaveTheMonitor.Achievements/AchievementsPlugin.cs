using DaveTheMonitor.Achievements.Patches;
using DaveTheMonitor.Achievements.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StudioForge.BlockWorld;
using StudioForge.Engine.Core;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DaveTheMonitor.Achievements
{
    /// <summary>
    /// The main plugin for the Achievements mod.
    /// </summary>
    public sealed class AchievementsPlugin : ITMPlugin
    {
        /// <summary>
        /// The current mod save version.
        /// </summary>
        public static int SaveVersion => 1;
        /// <summary>
        /// The active instance of this plugin.
        /// </summary>
        public static AchievementsPlugin Instance { get; private set; }
        /// <summary>
        /// The active AchievementManager
        /// </summary>
        public AchievementManager AchievementManager { get; private set; }
        /// <summary>
        /// The Achievements mod.
        /// </summary>
        public ITMMod Mod { get; private set; }
        /// <summary>
        /// Delegate for GetAchievement.
        /// </summary>
        public Func<ITMMod, string, object> GetAchievementDelegate { get; private set; }
        /// <summary>
        /// Delegate for UnlockAchievement.
        /// </summary>
        public Action<ITMPlayer, ITMMod, string> UnlockAchievementDelegate { get; private set; }
        /// <summary>
        /// Delegate for IsAchievementUnlocked.
        /// </summary>
        public Func<ITMPlayer, ITMMod, string, bool> IsAchievementUnlockedDelegate { get; private set; }
        /// <summary>
        /// Delegate for IsAchievementLocked.
        /// </summary>
        public Func<ITMPlayer, ITMMod, string, bool> IsAchievementLockedDelegate { get; private set; }
        /// <summary>
        /// Delegate for GetItemCrafted.
        /// </summary>
        public Func<ITMPlayer, Item, int> GetItemCraftedDelegate { get; private set; }
        /// <summary>
        /// Delegate for HasItemCrafted.
        /// </summary>
        public Func<ITMPlayer, Item, bool> HasItemCraftedDelegate { get; private set; }
        private Action[] _hotReloadEvents;
        private HashSet<ulong> _playerEvents;
        internal ITMGame _game;
        private Item[] _chefItems;
        private HashSet<Item> _chefItemsHashSet;
        private HashSet<Item> _jewelerItems;

        /// <summary>
        /// ITMPlugin.Callback implementation
        /// </summary>
        public void Callback(string data, GlobalPoint3D? p, ITMActor actor, ITMActor contextActor)
        {
            
        }

        /// <summary>
        /// ITMPlugin.Draw implementation
        /// </summary>
        public void Draw(ITMPlayer player, ITMPlayer virtualPlayer, Viewport vp)
        {
            AchievementManager.Draw(vp);
        }

        /// <summary>
        /// ITMPlugin.HandleInput implementation
        /// </summary>
        public bool HandleInput(ITMPlayer player)
        {
#if DEBUG
            // used to get a screenshot for a block for an icon
            if (InputManager.IsKeyPressedNew(player.PlayerIndex, Keys.G))
            {
                player.Position = new Vector3(2.5f, 1f, 2.5f);
                player.ViewDirection = Vector3.Normalize(new Vector3(-1, -0.4f, -1));
                return true;
            }
#endif
            if (InputManager.IsKeyPressedNew(player.PlayerIndex, AchievementsConfig.MenuKey))
            {
                AchievementsMenu menu = new AchievementsMenu(null, -1, _game, player, AchievementManager);
                _game.OpenPauseMenu(player, new NewGuiMenu[] { menu }, false);
                return true;
            }
            return false;
        }

        /// <summary>
        /// ITMPlugin.Initialize implementation
        /// </summary>
        public void Initialize(ITMPluginManager mgr, ITMMod mod)
        {
            AchievementsContent.LoadContent(Path.Combine(mod.FullPath, "Content"));
            Mod = mod;
            GetAchievementDelegate = GetAchievement;
            UnlockAchievementDelegate = UnlockAchievement;
            IsAchievementUnlockedDelegate = IsAchievementUnlocked;
            IsAchievementLockedDelegate = IsAchievementLocked;
            _playerEvents = new HashSet<ulong>();
            _craftEvents = null;
            _crafted = new Dictionary<ulong, int[]>();
            _hotReloadEvents = null;

            PatchHelper.Patch(typeof(HotLoadPatch));
            PatchHelper.Patch(typeof(BookWritePatch));
            PatchHelper.Patch(typeof(ArcadeMachinePatch));
            CheckForTeleportPatch.TargetMethod();
            PatchHelper.Patch(typeof(CheckForTeleportPatch));
            CraftPatch.Patch(PatchHelper._harmony);
            Instance = this;
            AchievementsConfig.LoadConfig();
        }

        /// <summary>
        /// ITMPlugin.InitializeGame implementation
        /// </summary>
        public void InitializeGame(ITMGame game)
        {
            _game = game;
            AchievementManager = new AchievementManager(game);

            _chefItems = GetCraftableFood().ToArray();
            _chefItemsHashSet = new HashSet<Item>();
            foreach (Item item in _chefItems)
            {
                _chefItemsHashSet.Add(item);
            }
            _jewelerItems = new HashSet<Item>
            {
                Item.RingOfBob,
                Item.AmuletOfFury,
                Item.NecklaceOfKnowledge,
                Item.SpiderRing,
                Item.PredatorAmulet,
                Item.NecklaceOfHypocrisy,
                Item.RingOfExemption,
                Item.AmuletOfStarlight,
                Item.NecklaceOfFarsight
            };

            foreach (ITMMod mod in game.GetActiveMods())
            {
                AchievementManager.LoadTextures(mod.FullPath);
                AchievementManager.LoadAchievements(game, mod);
            }

            AddAchievementConditions();
            AddGameEvents();

            if (game.World.Header.DateSaved != 0)
            {
                ReadSaveData();
            }
        }

        private List<Item> GetCraftableFood()
        {
            // We dynamically find craftable foods for better mod compatibility
            // This allows modded food count towards the achievement.
            List<Item> list = new List<Item>();
            HashSet<Item> blueprints = new HashSet<Item>();
            foreach (BlueprintXML bp in Globals1.BlueprintData)
            {
                blueprints.Add(bp.Result.ItemID);
            }

            foreach (ItemTypeDataXML type in Globals1.ItemTypeData)
            {
                if ((type.SubType & ItemSubType.Edible) > 0 && blueprints.Contains(type.ItemID))
                {
                    if (!list.Contains(type.ItemID))
                    {
                        list.Add(type.ItemID);
                    }
                }
            }

            return list;
        }

        private void AddAchievementConditions()
        {
            AddProgressFunc(Mod, "Chef", ChefProgress);
            AddProgressFunc(Mod, "Farmer", FarmerProgress);
            AddUnlockCondition(Mod, "DemolitionExpert", DemolitionExpertCondition);
            AddUnlockCondition(Mod, "ForgedByAncients", ForgedByAncientsCondition);
            AddUnlockCondition(Mod, "OneHitWonder", OneHitWonderCondition);
            AddUnlockCondition(Mod, "Photographer", PhotographerCondition);
            AddUnlockCondition(Mod, "RockBottom", RockBottomCondition);
        }

        #region Events

        private void AddGameEvents()
        {
            _game.AddEventItemSwing(Item.Binoculars, UseBinoculars);
            _game.AddEventItemSwing(Item.DecalApplicator, UseDecalApplicator);
            _game.AddEventBlockPlaced(Block.Crop, PlaceCrop);
            _game.AddEventBlockPlaced(Block.Painting, PlacePainting);
            _game.AddEventBlockPlaced(Block.WifiTransmitter, PlaceWifiBlock);
            _game.AddEventBlockPlaced(Block.WifiReceiver, PlaceWifiBlock);
            _game.AddEventBlockMined(Block.SpiderEgg, DestroySpiderEgg);
            AddEventItemCrafted(ItemCrafted);
        }

        private void RemoveGameEvents()
        {
            _game.RemoveEventItemSwing(Item.Binoculars, UseBinoculars);
            _game.RemoveEventItemSwing(Item.DecalApplicator, UseDecalApplicator);
            _game.RemoveEventBlockPlaced(Block.Crop, PlaceCrop);
            _game.RemoveEventBlockPlaced(Block.Painting, PlacePainting);
            _game.RemoveEventBlockPlaced(Block.WifiTransmitter, PlaceWifiBlock);
            _game.RemoveEventBlockPlaced(Block.WifiReceiver, PlaceWifiBlock);
            _game.RemoveEventBlockMined(Block.SpiderEgg, DestroySpiderEgg);
            RemoveEventItemCrafted(ItemCrafted);
        }

        private void AddPlayerEvents(ITMPlayer player)
        {
            if (_playerEvents.Contains(player.GamerID.ID))
            {
                return;
            }

            _playerEvents.Add(player.GamerID.ID);
            player.BlockOpened += OpenBlockHandler;
            player.WisdomScrollFound += FindWisdomHandler;
        }

        private void RemovePlayerEvents(ITMPlayer player)
        {
            if (!_playerEvents.Contains(player.GamerID.ID))
            {
                return;
            }

            _playerEvents.Remove(player.GamerID.ID);
            player.BlockOpened -= OpenBlockHandler;
            player.WisdomScrollFound -= FindWisdomHandler;
        }

        private void OpenBlockHandler(object sender, BlockEventArgs e)
        {
            // Using HasChanged here means this achievement won't unlock for previously opened chests
            // It might be worth looking into how this could be avoided so previously opened chests
            // still unlock the achievement, both for eventual multiplayer support and worlds played
            // before adding the mod
            if ((e.BlockID == Block.Chest || e.BlockID == Block.Crate || e.BlockID == Block.LockedChest) && !_game.World.Map.HasChanged(e.Point))
            {
                UnlockAchievement((ITMPlayer)sender, Mod, "TreasureHunter");
            }
        }

        private void FindWisdomHandler(object sender, IntEventArgs e)
        {
            UnlockAchievement((ITMPlayer)sender, Mod, "Wise");
        }

        private void UseBinoculars(Item item, ITMHand hand)
        {
            if (hand.Owner.IsPlayer)
            {
                UnlockAchievement(hand.Player, Mod, "Sightseer");
            }
        }

        private void UseDecalApplicator(Item item, ITMHand hand)
        {
            if (hand.Owner.IsPlayer && hand.Player.SwingFace != BlockFace.ProxyDefault)
            {
                UnlockAchievement(hand.Player, Mod, "GraffitiArtist");
            }
        }

        private void PlaceCrop(Block block, GlobalPoint3D p, ITMHand hand)
        {
            if (!hand.Owner.IsPlayer)
            {
                return;
            }

            ITMPlayer player = hand.Player;
            if (player.GetAction(Item.Crop, ItemAction.Used) + 1 >= 5)
            {
                UnlockAchievement(player, Mod, "Farmer");
            }
        }

        private void PlacePainting(Block block, GlobalPoint3D p, ITMHand hand)
        {
            if (!hand.Owner.IsPlayer)
            {
                return;
            }

            UnlockAchievement(hand.Player, Mod, "Painter");
        }

        private void PlaceWifiBlock(Block block, GlobalPoint3D p, ITMHand hand)
        {
            if (!hand.Owner.IsPlayer)
            {
                return;
            }

            ITMPlayer player = hand.Player;
            if (block == Block.WifiTransmitter && player.HasAction(Item.WifiReceiver, ItemAction.Used))
            {
                UnlockAchievement(hand.Player, Mod, "WPT");
            }
            else if (block == Block.WifiReceiver && player.HasAction(Item.WifiTransmitter, ItemAction.Used))
            {
                UnlockAchievement(hand.Player, Mod, "WPT");
            }
        }

        private void DestroySpiderEgg(Block block, ushort aux, GlobalPoint3D p, ITMHand hand)
        {
            if (!hand.Owner.IsPlayer)
            {
                return;
            }

            UnlockAchievement(hand.Player, Mod, "Exterminator");
        }

        private void ItemCrafted(ITMPlayer player, Item item)
        {
            if (item == Item.Workbench)
            {
                UnlockAchievement(player, Mod, "Carpenter");
            }
            else if (item == Item.TitaniumPickaxe)
            {
                UnlockAchievement(player, Mod, "ExpertMiner");
            }
            else if (_jewelerItems.Contains(item))
            {
                UnlockAchievement(player, Mod, "Jeweler");
            }
            else if (_chefItemsHashSet.Contains(item) && IsAchievementLocked(player, Mod, "Chef"))
            {
                TryUnlockChef(player);
            }
        }

        private void TryUnlockChef(ITMPlayer player)
        {
            int total = 0;
            foreach (Item item in _chefItems)
            {
                if (HasItemCrafted(player, item))
                {
                    total++;
                }

                if (total >= 5)
                {
                    UnlockAchievement(player, Mod, "Chef");
                    break;
                }
            }
        }

        #endregion

        #region Unlock Conditions

        private bool DemolitionExpertCondition(ITMGame game, ITMPlayer player)
        {
            foreach (InventoryItem item in player.Inventory.Items)
            {
                if (item.ItemID == Item.TNT || item.ItemID == Item.C4)
                {
                    return true;
                }
            }
            return false;
        }

        private bool ForgedByAncientsCondition(ITMGame game, ITMPlayer player)
        {
            foreach (InventoryItem item in player.Inventory.Items)
            {
                if (Globals1.ItemTypeData[(int)item.ItemID].Rarity == ItemRarityType.Rare)
                {
                    return true;
                }
            }
            return false;
        }

        private bool OneHitWonderCondition(ITMGame game, ITMPlayer player)
        {
            foreach (InventoryItem item in player.Inventory.Items)
            {
                if (item.ItemID == Item.SledgeHammer || item.ItemID == Item.GreenstoneGoldSledgeHammer)
                {
                    return true;
                }
            }
            return false;
        }

        private bool PhotographerCondition(ITMGame game, ITMPlayer player)
        {
            foreach (InventoryItem item in player.Inventory.Items)
            {
                if (item.ItemID == Item.Camera)
                {
                    return true;
                }
            }
            return false;
        }

        private bool RockBottomCondition(ITMGame game, ITMPlayer player)
        {
            return player.Position.Y <= 1.5f;
        }

        #endregion

        #region Progress

        private (float, string) ChefProgress(ITMGame game, ITMPlayer player)
        {
            int total = 0;
            foreach (Item item in _chefItems)
            {
                if (HasItemCrafted(player, item))
                {
                    total++;
                }

                if (total >= 5)
                {
                    return (1, "5/5");
                }
            }

            return (total / 5f, $"{total}/5");
        }

        private (float, string) FarmerProgress(ITMGame game, ITMPlayer player)
        {
            int total = player.GetAction(Item.Crop, ItemAction.Used);
            if (total >= 5)
            {
                return (1, "5/5");
            }

            return (total / 5f, $"{total}/5");
        }

        #endregion

        /// <summary>
        /// ITMPlugin.PlayerJoined implementation
        /// </summary>
        public void PlayerJoined(ITMPlayer player)
        {
            AddPlayerEvents(player);
        }

        /// <summary>
        /// ITMPlugin.PlayerLeft implementation
        /// </summary>
        public void PlayerLeft(ITMPlayer player)
        {
            RemovePlayerEvents(player);
        }

        /// <summary>
        /// ITMPlugin.RegisterLuaFunctions implementation
        /// </summary>
        public object[] RegisterLuaFunctions(ITMScriptInstance si) => Array.Empty<object>();

        /// <summary>
        /// ITMPlugin.UnloadMod implementation
        /// </summary>
        public void UnloadMod()
        {
            Unload();
        }

        internal void HotLoad()
        {
            Unload();
            List<ITMPlayer> players = new List<ITMPlayer>();
            _game.GetAllPlayers(players);
            foreach (ITMPlayer player in players)
            {
                RemovePlayerEvents(player);
            }

            if (_hotReloadEvents != null && _hotReloadEvents.Length > 0)
            {
                foreach (Action action in _hotReloadEvents)
                {
                    action();
                }
            }
        }

        private void Unload()
        {
            PatchHelper.Unpatch();
            AchievementsContent.UnloadContent();
            AchievementManager.Unload();
            Instance = null;
            AchievementManager = null;

            RemoveGameEvents();
        }

        /// <summary>
        /// ITMPlugin.Update implementation
        /// </summary>
        public void Update()
        {

        }

        /// <summary>
        /// ITMPlugin.Update implementation
        /// </summary>
        public void Update(ITMPlayer player)
        {
            AddPlayerEvents(player);
            AchievementManager.Update(player);
        }

        /// <summary>
        /// ITMPlugin.WorldSaved implementation
        /// </summary>
        public void WorldSaved(int version)
        {
            WriteSaveData();
        }

        private void WriteSaveData()
        {
            string achievementsFile = Path.Combine(FileSystem.RootPath, _game.World.WorldPath, "achievements.dat");
            string dataFile = Path.Combine(FileSystem.RootPath, _game.World.WorldPath, "achievementsdata.dat");

            using (Stream stream = File.OpenWrite(achievementsFile))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Globals1.SaveVersion);
                    writer.Write(SaveVersion);
                    AchievementManager.WriteUnlockState(writer);
                }
            }

            using (Stream stream = File.OpenWrite(dataFile))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Globals1.SaveVersion);
                    writer.Write(SaveVersion);
                    WriteState(writer);
                }
            }
        }

        private void WriteState(BinaryWriter writer)
        {
            writer.Write(_crafted.Count);
            foreach (KeyValuePair<ulong, int[]> pair in _crafted)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value.Length);
                foreach (int v in pair.Value)
                {
                    writer.Write(v);
                }
            }
        }

        private void ReadSaveData()
        {
            string achievementsFile = Path.Combine(FileSystem.RootPath, _game.World.WorldPath, "achievements.dat");
            string dataFile = Path.Combine(FileSystem.RootPath, _game.World.WorldPath, "achievementsdata.dat");

            if (File.Exists(achievementsFile))
            {
                using (Stream stream = File.OpenRead(achievementsFile))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        int gameVersion = reader.ReadInt32();
                        int version = reader.ReadInt32();
                        AchievementManager.ReadUnlockState(reader, gameVersion, version);
                    }
                }
            }

            if (File.Exists(dataFile))
            {
                using (Stream stream = File.OpenRead(dataFile))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        int gameVersion = reader.ReadInt32();
                        int version = reader.ReadInt32();
                        ReadState(reader, gameVersion, version);
                    }
                }
            }
        }

        private void ReadState(BinaryReader reader, int gameVersion, int version)
        {
            _crafted.Clear();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                ulong id = reader.ReadUInt64();
                int length = reader.ReadInt32();
                int[] arr = new int[Math.Max(length, Globals1.ItemData.Length)];
                for (int j = 0; j < length; j++)
                {
                    arr[j] = reader.ReadInt32();
                }

                _crafted.Add(id, arr);
            }
        }

        /// <summary>
        /// Gets the achievement from the specified mod with the specified ID
        /// </summary>
        /// <param name="mod">The mod that added the achievement.</param>
        /// <param name="id">The ID of the achievement.</param>
        /// <returns>The specified achievement, or null if not found.</returns>
        public Achievement GetAchievement(ITMMod mod, string id)
        {
            return AchievementManager.GetAchievement(mod, id);
        }

        /// <summary>
        /// Adds a method to be called to test if an achievement has been unlocked to the specified achievement. An achievement can have multiple unlock conditions, only one has to return True to unlock the achievement.
        /// </summary>
        /// <param name="mod">The mod that added the achievement.</param>
        /// <param name="id">The ID of the achievement.</param>
        /// <param name="unlockFunc">The function to call.</param>
        public void AddUnlockCondition(ITMMod mod, string id, Func<ITMGame, ITMPlayer, bool> unlockFunc)
        {
            AchievementManager.AddUnlockCondition(mod, id, unlockFunc);
        }

        /// <summary>
        /// Adds a method to be called to get the unlock progress of the specified achievement. Currently an achievement can only have one progress func.
        /// </summary>
        /// <param name="mod">The mod that added the achievement.</param>
        /// <param name="id">The ID of the achievement.</param>
        /// <param name="progressFunc">The function to call.</param>
        public void AddProgressFunc(ITMMod mod, string id, Func<ITMGame, ITMPlayer, (float, string)> progressFunc)
        {
            AchievementManager.AddProgressFunc(mod, id, progressFunc);
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
            return AchievementManager.IsAchievementUnlocked(player, mod, id);
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
            return AchievementManager.IsAchievementLocked(player, mod, id);
        }

        /// <summary>
        /// Unlocks the specified achievement if it isn't already unlocked.
        /// </summary>
        /// <param name="player">The player to unlock the achievement for.</param>
        /// <param name="mod">The mod that added the achievement.</param>
        /// <param name="id">The ID of the achievement.</param>
        public void UnlockAchievement(ITMPlayer player, ITMMod mod, string id)
        {
            AchievementManager.UnlockAchievement(player, mod, id);
        }

        /// <summary>
        /// Adds an action to be called when mods are hot reloaded. Use this to remove any events from players/actors on hot reload.
        /// </summary>
        /// <param name="action">The action to add.</param>
        public void AddEventHotReload(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_hotReloadEvents == null)
            {
                _hotReloadEvents = new Action[1];
            }
            else
            {
                Array.Resize(ref _hotReloadEvents, _hotReloadEvents.Length + 1);
            }
            _hotReloadEvents[_hotReloadEvents.Length - 1] = action;
        }

        /// <summary>
        /// Removes an action previously added with AddEventHotReload.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        public void RemoveEventHotReload(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            Action[] array = new Action[_hotReloadEvents.Length - 1];
            int newIndex = 0;
            for (int i = 0; i < _hotReloadEvents.Length; i++)
            {
                Action e = _hotReloadEvents[i];
                if (e == action)
                {
                    continue;
                }

                array[newIndex] = e;
                newIndex++;
            }

            _hotReloadEvents = array;
        }

        #region ItemCraft
        // This is a real ItemCraft event that actually functions properly
        // Once ITMPlayer.Event_ItemCraft and action log are fixed, everything
        // in this region can be removed

        private Action<ITMPlayer, Item>[] _craftEvents;
        private Dictionary<ulong, int[]> _crafted;

        /// <summary>
        /// Adds an action to be called when a player crafts an item. Use this instead of ITMPlayer.Event_ItemCrafted.
        /// </summary>
        /// <param name="action">The action to add.</param>
        public void AddEventItemCrafted(Action<ITMPlayer, Item> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_craftEvents == null)
            {
                _craftEvents = new Action<ITMPlayer, Item>[1];
            }
            else
            {
                Array.Resize(ref _craftEvents, _craftEvents.Length + 1);
            }
            _craftEvents[_craftEvents.Length - 1] = action;
        }

        /// <summary>
        /// Removes an action previously added with AddEventItemCrafted.
        /// </summary>
        /// <param name="action">The action to add.</param>
        public void RemoveEventItemCrafted(Action<ITMPlayer, Item> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            Action<ITMPlayer, Item>[] array = new Action<ITMPlayer, Item>[_craftEvents.Length - 1];
            int newIndex = 0;
            for (int i = 0; i < _craftEvents.Length; i++)
            {
                Action<ITMPlayer, Item> e = _craftEvents[i]; 
                if (e == action)
                {
                    continue;
                }

                array[newIndex] = e;
                newIndex++;
            }

            _craftEvents = array;
        }

        internal void ItemCraft(ITMPlayer player, Item item)
        {
            if (!_crafted.TryGetValue(player.GamerID.ID, out int[] items))
            {
                items = new int[Globals1.ItemData.Length];
                _crafted.Add(player.GamerID.ID, items);
            }

            items[(int)item]++;

            if (_craftEvents != null && _craftEvents.Length > 0)
            {
                foreach (Action<ITMPlayer, Item> action in _craftEvents)
                {
                    action(player, item);
                }
            }
        }

        /// <summary>
        /// Returns the number of times the specified item has been crafted. Note that this returns how many times the player has crafted the item, not how many of the item has been crafted.
        /// </summary>
        /// <param name="player">The player to test crafts for.</param>
        /// <param name="item">The item to test crafts for.</param>
        /// <returns>The number of times the player has crafted the specified item.</returns>
        public int GetItemCrafted(ITMPlayer player, Item item)
        {
            if (_crafted.TryGetValue(player.GamerID.ID, out int[] items))
            {
                return items[(int)item];
            }
            return 0;
        }

        /// <summary>
        /// Returns True if the player has crafted the specified item.
        /// </summary>
        /// <param name="player">The player to test crafts for.</param>
        /// <param name="item">The item to test crafts for.</param>
        /// <returns>True if the player has crafted the specified item, otherwise False.</returns>
        public bool HasItemCrafted(ITMPlayer player, Item item)
        {
            return GetItemCrafted(player, item) > 0;
        }

        #endregion

        public AchievementsPlugin(List<Assembly> loadedAssemblies)
        {

        }
    }
}
