using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Shops;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using xTile;
using xTile.ObjectModel;
using Object = StardewValley.Object;

namespace Restauranteer
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry : Mod
    {

        public static IMonitor SMonitor;
        public static IModHelper SHelper;
        public static ModConfig Config;

        public static ModEntry context;
        
        public static string orderKey = "aedenthorn.Restauranteer/order";
        public static string fridgeKey = "aedenthorn.Restauranteer/fridge";
        public static Texture2D emoteSprite;
        public static Vector2 fridgeHideTile = new Vector2(-42000, -42000);
        public static Dictionary<string, int> npcOrderNumbers = new Dictionary<string, int>();
        public static Dictionary<string, NetRef<Chest>> fridgeDict = new();
        private Harmony harmony;
        internal static IBetterCrafting BetterCraftingApi;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            context = this;

            SMonitor = Monitor;
            SHelper = helper;

            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            Helper.Events.GameLoop.DayEnding += GameLoop_DayEnding;
            Helper.Events.GameLoop.OneSecondUpdateTicked += GameLoop_OneSecondUpdateTicked;
            Helper.Events.Content.AssetRequested += Content_AssetRequested;
            Helper.Events.Display.MenuChanged += Display_MenuChanged;


            GameLocation.RegisterTileAction($"{ModManifest.UniqueID}_kitchen", ActivateKitchen);
            GameLocation.RegisterTileAction($"{ModManifest.UniqueID}_restaurant", ActivateKitchen);

            harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll(typeof(ModEntry).Assembly);

            npcOrderNumbers = new Dictionary<string, int>();
        }

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            fridgeDict.Clear();
            npcOrderNumbers.Clear();
            emoteSprite = SHelper.ModContent.Load<Texture2D>(Path.Combine("assets", "emote.png"));
            foreach (var name in Config.RestaurantLocations)
            {
                var l = Game1.getLocationFromName(name);
                if (l is not null && l is not FarmHouse)
                {
                    for (int x = 0; x < l.Map.GetLayer("Buildings").Tiles.Array.GetLength(0); x++)
                    {
                        for (int y = 0; y < l.Map.GetLayer("Buildings").Tiles.Array.GetLength(1); y++)
                        {
                            if (l.Map.GetLayer("Buildings").Tiles[x, y] is not null && l.Map.GetLayer("Buildings").Tiles[x, y].Properties.TryGetValue("Action", out PropertyValue p) && (p == "fridge" || p == "DropBox GusFridge"))
                            {
                                Vector2 v = new Vector2(x, y);
                                if (Config.AddFridgeObjects && !l.objects.TryGetValue(v, out Object obj) && !Game1.player.team.SpecialOrderActive("Gus"))
                                {
                                    Chest fridge = new Chest("216", v, 217, 2)
                                    {
                                        shakeTimer = 50
                                    };
                                    fridge.modData[fridgeKey] = "true";
                                    fridge.fridge.Value = true;
                                    l.objects[v] = fridge;
                                }
                                else if (!Config.AddFridgeObjects && l.objects.TryGetValue(v, out obj) && obj.modData.ContainsKey(fridgeKey))
                                {
                                    l.objects.Remove(v);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GameLoop_DayEnding(object sender, StardewModdingAPI.Events.DayEndingEventArgs e)
        {
            if (!Config.ModEnabled || (Config.RequireEvent && !Game1.player.eventsSeen.Contains("980558")))
                return;

            foreach (var name in Config.RestaurantLocations)
            {
                var fridge = Game1.getLocationFromName(name).GetFridge();
                var miniFridge = GetMiniFridge(Game1.getLocationFromName(name));

                if (fridge != null)
                    fridge.Items.Clear();

                if (miniFridge != null)
                    miniFridge.Items.Clear();
            }
            Helper.GameContent.InvalidateCache("Data/Shops");
        }

        private void GameLoop_OneSecondUpdateTicked(object sender, StardewModdingAPI.Events.OneSecondUpdateTickedEventArgs e)
        {
            if(Config.ModEnabled && Context.IsPlayerFree && Config.RestaurantLocations.Contains(Game1.player.currentLocation.Name) && (!Config.RequireEvent || Game1.player.eventsSeen.Contains("980558")) && !Game1.player.team.SpecialOrderActive("Gus"))
            {
                UpdateOrders();
            }
        }

        private void Content_AssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (!Config.ModEnabled)
                return;
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Farm"))
            {
                e.Edit(delegate (IAssetData data)
                {
                    var dict = data.AsDictionary<string, string>();
                    if(dict.Data.TryGetValue(Config.EventKey, out string str))
                    {
                        dict.Data[Config.EventKey] = str.Replace(Config.EventReplacePart, string.Format(Config.EventReplaceWith, Helper.Translation.Get("gus-event-string")));
                    }
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/Saloon"))
            {
                e.Edit(delegate (IAssetData data)
                {
                    var map = data.AsMap();
                    if (Config.PatchSaloonMap)
                    {
                        map.PatchMap(Helper.ModContent.Load<Map>(Path.Combine("assets", "SaloonKitchen.tmx")), targetArea: new Microsoft.Xna.Framework.Rectangle(10, 12, 8, 5), patchMode: PatchMapMode.Replace);
                    }
                    foreach(var tile in Config.KitchenTiles)
                    {
                        try
                        {
                            map.Data.GetLayer("Buildings").Tiles[tile.X, tile.Y].Properties["Action"] = "aedenthorn.Restauranteer_kitchen";
                        }
                        catch { }
                    }
                }, StardewModdingAPI.Events.AssetEditPriority.Late);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/schedules/Emily") && !string.IsNullOrEmpty(Config.EmilySaloonString))
            {
                e.Edit(delegate (IAssetData data)
                {
                    var dict = data.AsDictionary<string, string>();
                    Regex ex = new Regex(@"Saloon [0-9]+ [0-9]+[^/]*", RegexOptions.Compiled);
                    Monitor.Log($"Replacing Emily saloon string with {Config.EmilySaloonString}");
                    foreach (var key in dict.Data.Keys)
                    {
                        dict.Data[key] = ex.Replace(dict.Data[key], Config.EmilySaloonString);
                    }
                }, StardewModdingAPI.Events.AssetEditPriority.Late);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            {
                e.Edit(
                    asset =>
                    {     
                        if (!Context.IsWorldReady || !Config.ModEnabled || !Config.SellCurrentRecipes || Game1.player.team.SpecialOrderActive("Gus"))
                            return;

                        var data = asset.AsDictionary<string, ShopData>().Data;

                        foreach (var npc in Game1.getLocationFromName("Saloon").characters)
                        {                        
                            if (npc.modData.TryGetValue(orderKey, out string dataString))
                            {
                                OrderData orderData = JsonConvert.DeserializeObject<OrderData>(dataString);
                                
                                if (data.TryGetValue("Saloon", out var shopData) && orderData != null && orderData.dishName != null && orderData.dish != null && !Game1.player.cookingRecipes.ContainsKey(orderData.dishName))
                                {
                                    
                                    var shopItem = new ShopItemData();
                                    shopItem.IsRecipe = true;
                                    shopItem.Price = orderData.dishPrice;
                                    shopItem.ItemId = orderData.dish;
                                    shopItem.AvailableStock = 1;
                                    shopItem.AvailableStockLimit = LimitedStockMode.Player;
                                    shopItem.AvoidRepeat = true;
                                    shopData.Items.Add(shopItem);
                                    SMonitor.Log($"Recipe for {orderData.dishName} has been added to Saloon stock from current order.");
                                }
                            }
                        }
                    }, StardewModdingAPI.Events.AssetEditPriority.Late);
            }
        }

        private void Display_MenuChanged(object sender, StardewModdingAPI.Events.MenuChangedEventArgs e)
        {
            if (!Context.IsWorldReady || e.NewMenu == e.OldMenu || e.NewMenu == null)
                return;

            if (!Config.ModEnabled || !Config.RestaurantLocations.Contains(Game1.player.currentLocation.Name))
                return;


            if (e.NewMenu is ShopMenu menu && menu.ShopId == "Saloon")
            {
                Helper.GameContent.InvalidateCache("Data/Shops");
                return;
            }

            if (e.NewMenu.GetType().ToString() == "LoveOfCooking.Menu.CookingMenu" || e.NewMenu.GetType().ToString() == "Leclair.Stardew.BetterCrafting.Menus.BetterCraftingPage")
                return;


            if (e.NewMenu is CraftingPage page && page.cooking)
            {
                var fridge = Game1.player.currentLocation.GetFridge();
                var miniFridge = GetMiniFridge(Game1.player.currentLocation);

                List<IInventory> items = new();

                if (fridge != null)
                    items.Add(fridge.Items);

                if (miniFridge != null)
                    items.Add(miniFridge.Items);

                if (!items.Any())
                    return;

                var containers = page.GetType().GetField("_materialContainers", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                containers.SetValue(page, items);
                return;
            }

            return;

        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {

            object obj = Helper.ModRegistry.GetApi("blueberry.LoveOfCooking");
            if (obj is not null)
            {
                harmony.Patch( 
                    original: AccessTools.Constructor(obj.GetType().Assembly.GetType("LoveOfCooking.Menu.CookingMenu"), new Type[] { typeof(List<CraftingRecipe>), typeof(Dictionary<IInventory, Chest>), typeof(string)  }),
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.LoveOfCooking_CookingMenu_Prefix))
                );
            }

            IBetterCrafting betterCrafting = Helper.ModRegistry.GetApi<IBetterCrafting>("leclair.bettercrafting");
            BetterCraftingApi = betterCrafting;

            if (BetterCraftingApi is not null)
                BetterCraftingApi.MenuSimplePopulateContainers += BetterCraftingApi_MenuSimplePopulateContainers;


            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Mod Enabled",
                getValue: () => Config.ModEnabled,
                setValue: value => Config.ModEnabled = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Require Event for Saloon",
                getValue: () => Config.RequireEvent,
                setValue: value => Config.RequireEvent = value,
                tooltip: () => "If enabled, requires the player to have seen Gus' 5 Hearts event before being able to use the Saloon kitchen."
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Auto Fill Fridge",
                getValue: () => Config.AutoFillFridge,
                setValue: value => Config.AutoFillFridge = value,
                tooltip: () => "If enabled, auto fills Saloon/custom restaurant locations fridge/mini-fridge with ingredients from requested dishes."
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Add Fridge Objects",
                getValue: () => Config.AddFridgeObjects,
                setValue: value => Config.AddFridgeObjects = value,
                tooltip: () => "If enabled, adds fridge to Saloon/custom restaurant locations to specified tile in map file."
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Reveal Gift Taste",
                getValue: () => Config.RevealGiftTaste,
                setValue: value => Config.RevealGiftTaste = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Sell Current Recipes",
                getValue: () => Config.SellCurrentRecipes,
                setValue: value => Config.SellCurrentRecipes = value,
                tooltip: () => "If enabled, adds unowned recipes for requested dishes to Gus' shop to purchase."
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Patch Saloon Map",
                getValue: () => Config.PatchSaloonMap,
                setValue: value => Config.PatchSaloonMap = value,
                tooltip: () => "If enabled, patches the Saloon map to add the kitchen."
            );
            configMenu.AddKeybind(
                mod: ModManifest,
                name: () => "Show Dish Name Key",
                getValue: () => Config.ModKey,
                setValue: value => Config.ModKey = value,
                tooltip: () => "Sets the key to display the dish name instead of the image when held down."
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Order Chance / s",
                getValue: () => Config.OrderChance + "",
                setValue: delegate(string value) { if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float val)){ Config.OrderChance = val; } },
                tooltip: () => "Sets the global chance percentage for an NPC to request a dish."
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Loved Dish Order Chance",
                getValue: () => Config.LovedDishChance + "",
                setValue: delegate(string value) { if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float val)){ Config.LovedDishChance = val; } },
                tooltip: () => "Sets the global chance percentage for an NPC to request a loved dish over a liked/netural one."
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Price Multiplier",
                getValue: () => Config.PriceMarkup + "",
                setValue: delegate(string value) { if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float val)){ Config.PriceMarkup = val; } },
                tooltip: () => "Sets the value which each dish price is multiplied by to create the amount the player is paid for each fulfilled order."
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Max NPC Orders Per Night",
                getValue: () => Config.MaxNPCOrdersPerNight,
                setValue: value => Config.MaxNPCOrdersPerNight = value,
                tooltip: () => "Sets the maximum number of orders an NPC can request in one night."
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Loved Friendship Change",
                getValue: () => Config.LovedFriendshipChange,
                setValue: value => Config.LovedFriendshipChange= value,
                tooltip: () => "Sets the amount of friendship points given for a fulfilled loved order."
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Liked Friendship Change",
                getValue: () => Config.LikedFriendshipChange,
                setValue: value => Config.LikedFriendshipChange = value,
                tooltip: () => "Sets the amount of friendship points given for a fulfilled liked order."
            );
        }

        private static void LoveOfCooking_CookingMenu_Prefix(ref Dictionary<IInventory, Chest> materialContainers)
        {
            if (!Config.ModEnabled || !Config.RestaurantLocations.Contains(Game1.currentLocation.Name))
                return;
            var fridge = Game1.currentLocation.GetFridge();
            var miniFridge = GetMiniFridge(Game1.currentLocation);
            if (materialContainers is null)
                materialContainers = new Dictionary<IInventory, Chest>();
            if (fridge != null)
                materialContainers.TryAdd(fridge.Items, fridge);
            if (miniFridge != null)
                materialContainers.TryAdd(miniFridge.Items, miniFridge);
        }

        private void BetterCraftingApi_MenuSimplePopulateContainers(ISimplePopulateContainersEvent e)
        {
            if (!Config.ModEnabled || !Config.RestaurantLocations.Contains(Game1.currentLocation.Name))
                return;

            var fridge = Game1.currentLocation.GetFridge();
            var miniFridge = GetMiniFridge(Game1.currentLocation);

            if (fridge != null)
                e.Containers.Add(new(fridge, Game1.currentLocation));
            if (miniFridge != null)
                e.Containers.Add(new(miniFridge, Game1.currentLocation));

        }
    }
}