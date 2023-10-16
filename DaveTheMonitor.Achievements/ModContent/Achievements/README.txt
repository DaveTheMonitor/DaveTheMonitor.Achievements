Other mods can add achievements by defining them with XML, and implementing their unlock behavior through C#. View the example achievement mod's source code on GitHub to see how to implement achievement unlocks through per-update conditions and events.

PLAYER NOTES:
Hot reloading mods may affect the progress of certain achievements and re-lock any achievements unlocked since the last save. If you need to hot reload and have made progress since your last save, consider saving the game first to keep your progress.

MOD DEVELOPER NOTES:
Longer achievement descriptions are not cut. Try to keep achievements with progress functions to 1 line descriptions, or 2 lines if the achievement doesn't have a progress function.

When implementing crafting-related achievements, do not use ITMPlayer.Event_ItemCrafted or ITMPlayer.GetAction(item, ItemAction.Crafted). They do not work properly. The AchievementsPlugin has methods that can be used instead until the issue is fixed:
- void AddEventItemCrafted(Action<ITMPlayer, Item> action)
- void RemoveEventItemCrafted(Action<ITMPlayer, Item> action)
- int GetItemCrafted(ITMPlayer player, Item item)
- bool HasItemCrafted(ITMPlayer player, Item item)

Achievement icon and background textures can be overwritten by other mods, regardless of which mod originally added it.

Remember to remove any events added to ITMPlayer by adding an action to hot reload with AchievementsPlugin.AddEventHotReload(Action action), otherwise the game may crash when the previously added events are triggered.