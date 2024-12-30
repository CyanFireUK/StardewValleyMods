using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using xTile;
using xTile.Layers;
using xTile.ObjectModel;
using Object = StardewValley.Object;

namespace ImportMap
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry : Mod
    {

        public static IManifest SModManifest;
        public static IMonitor SMonitor;
        public static IModHelper SHelper;
        public static ModConfig Config;

        public static ModEntry context;
        private static IAdvancedFluteBlocksApi fluteBlockApi;
        private static ITrainTracksApi trainTrackApi;
        private static bool inGame = false;

        private static string[] import;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            if (!Config.EnableMod)
                return;

            context = this;

            SModManifest = ModManifest;
            SMonitor = Monitor;
            SHelper = helper;

            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.Input.ButtonsChanged += Input_ButtonsChanged;
            Helper.ConsoleCommands.Add("importmap", "Import map data from import .tmx files.", OnConsoleCommandReceived);
            Helper.ConsoleCommands.Add("clearimportmap", "Clears map data imported from import .tmx files.", OnConsoleCommandReceived);
            Helper.ConsoleCommands.Add("nukemap", "Nuke the entire map.", OnConsoleCommandReceived);
            ChatCommands.Register("importmap", OnChatCommandReceived, name => $"{name}: Import map data from import .tmx files.");
            ChatCommands.Register("clearimportmap", OnChatCommandReceived, name => $"{name}: Clears map data imported from import .tmx files.");
            ChatCommands.Register("nukemap", OnChatCommandReceived, name => $"{name}: Nuke the entire map.");
        }

        internal void OnConsoleCommandReceived(string command, string[] args)
        {
            switch (command)
            {

                case "importmap":
                    Monitor.Log("Importing map(s)", LogLevel.Info);
                    DoImport();
                    return;

                case "clearimportmap":
                    Monitor.Log("Clearing imported map(s)", LogLevel.Info);
                    DoClearImport();
                    return;

                case "nukemap":
                    Game1.currentLocation.objects.Clear();
                    Game1.currentLocation.terrainFeatures.Clear();
                    Game1.currentLocation.overlayObjects.Clear();
                    Game1.currentLocation.resourceClumps.Clear();
                    Game1.currentLocation.largeTerrainFeatures.Clear();
                    Game1.currentLocation.furniture.Clear();
                    return;
            }
        }

        internal void OnChatCommandReceived(string[] command, ChatBox chat)
        {
            string raw = ArgUtility.GetRemainder(command, 0);
            string[] array = ArgUtility.SplitBySpaceQuoteAware(raw).Select(array => array.Trim(new char[] { '"' })).ToArray();

            var history = SHelper.Reflection.GetField<List<string>>(chat, "cheatHistory").GetValue();

            switch (array[0])
            {
                case "importmap":
                    inGame = true;
                    chat.clickAway();
                    chat.addInfoMessage("/" + raw);
                    history.Insert(0, "/" + raw);
                    Monitor.Log("Importing map(s)", LogLevel.Info);
                    DoImport();
                    return;

                case "clearimportmap":
                    inGame = true;
                    chat.clickAway();
                    chat.addInfoMessage("/" + raw);
                    history.Insert(0, "/" + raw);
                    Monitor.Log("Clearing imported map(s)", LogLevel.Info);
                    DoClearImport();
                    return;

                case "nukemap":
                    inGame = true;
                    chat.clickAway();
                    chat.addInfoMessage("/" + raw);
                    history.Insert(0, "/" + raw);
                    Game1.currentLocation.objects.Clear();
                    Game1.currentLocation.terrainFeatures.Clear();
                    Game1.currentLocation.overlayObjects.Clear();
                    Game1.currentLocation.resourceClumps.Clear();
                    Game1.currentLocation.largeTerrainFeatures.Clear();
                    Game1.currentLocation.furniture.Clear();
                    return;
            }
        }

        private static string FileName(string filePath)
        {
            return PathUtilities.GetSegments(filePath).Last().Split(".")[0];
        }

        private void Input_ButtonsChanged(object sender, StardewModdingAPI.Events.ButtonsChangedEventArgs e)
        {
            if (Config.EnableMod && Config.ImportKey.JustPressed())
            {
                inGame = true;
                Monitor.Log("Importing map(s)", LogLevel.Info);
                DoImport();
            }

            if (Config.EnableMod && Config.ClearImportKey.JustPressed())
            {
                inGame = true;
                Monitor.Log("Clearing imported map(s)", LogLevel.Info);
                DoClearImport();
            }
        }


        private static void DoClearImport()
        {
            if (!import.Any())
            {
                if (inGame == true)
                {
                    inGame = false;
                }
                return;
            }

            for (var i = 0; i < import.Length; i++)
            {
                Map map = SHelper.ModContent.Load<Map>(Path.Combine(SHelper.DirectoryPath, "assets", $"{Path.GetFileName(import[i])}"));
                if (map == null)
                {
                    if (inGame == true)
                    {
                        inGame = false;
                    }
                    return;
                }
                Dictionary<string, Layer> layersById = AccessTools.FieldRefAccess<Map, Dictionary<string, Layer>>(map, "m_layersById");
                if (layersById.TryGetValue("Flooring", out Layer flooringLayer))
                {
                    for (int y = 0; y < flooringLayer.LayerHeight; y++)
                    {
                        for (int x = 0; x < flooringLayer.LayerWidth; x++)
                        {
                            if (flooringLayer.Tiles[x, y] != null && flooringLayer.Tiles[x, y].TileIndex >= 0)
                            {
                                Game1.player.currentLocation.terrainFeatures.Remove(new Vector2(x, y));
                            }
                        }
                    }
                }
                if (trainTrackApi != null && layersById.TryGetValue("TrainTracks", out Layer trackLayer))
                {

                    for (int y = 0; y < trackLayer.LayerHeight; y++)
                    {
                        for (int x = 0; x < trackLayer.LayerWidth; x++)
                        {
                            if (trackLayer.Tiles[x, y] != null && trackLayer.Tiles[x, y].TileIndex >= 0)
                            {
                                trainTrackApi.RemoveTrack(Game1.player.currentLocation, new Vector2(x, y));
                            }
                        }
                    }
                }
                if (layersById.TryGetValue("FluteBlocks", out Layer fluteLayer))
                {
                    for (int y = 0; y < fluteLayer.LayerHeight; y++)
                    {
                        for (int x = 0; x < fluteLayer.LayerWidth; x++)
                        {
                            if (fluteLayer.Tiles[x, y] != null && fluteLayer.Tiles[x, y].TileIndex >= 0 && Game1.player.currentLocation.objects.ContainsKey(new Vector2(x, y)))
                            {
                                Game1.player.currentLocation.objects.Remove(new Vector2(x, y));
                            }
                        }
                    }
                }
                if (layersById.TryGetValue("DrumBlocks", out Layer drumLayer))
                {
                    for (int y = 0; y < drumLayer.LayerHeight; y++)
                    {
                        for (int x = 0; x < drumLayer.LayerWidth; x++)
                        {
                            if (drumLayer.Tiles[x, y] != null && drumLayer.Tiles[x, y].TileIndex >= 0 && Game1.player.currentLocation.objects.ContainsKey(new Vector2(x, y)))
                            {
                                Game1.player.currentLocation.objects.Remove(new Vector2(x, y));
                            }
                        }
                    }
                }
                if (layersById.TryGetValue("Objects", out Layer objLayer))
                {
                    var dict = SHelper.GameContent.Load<Dictionary<string, CropData>>("Data/Crops");

                    for (int y = 0; y < objLayer.LayerHeight; y++)
                    {
                        for (int x = 0; x < objLayer.LayerWidth; x++)
                        {
                            if (objLayer.Tiles[x, y] != null && objLayer.Tiles[x, y].TileIndex >= 0 && Game1.player.currentLocation.terrainFeatures.ContainsKey(new Vector2(x, y)) | Game1.player.currentLocation.objects.ContainsKey(new Vector2(x, y)))
                            {
                                if (dict.TryGetValue(objLayer.Tiles[x, y].TileIndex.ToString(), out var cropData))
                                {
                                    Game1.player.currentLocation.terrainFeatures.Remove(new Vector2(x, y));
                                    continue;
                                }
                                var cropkvp = dict.FirstOrDefault(kvp => kvp.Value.HarvestItemId == objLayer.Tiles[x, y].TileIndex.ToString());
                                if (cropkvp.Value != null)
                                {
                                    Game1.player.currentLocation.terrainFeatures.Remove(new Vector2(x, y));
                                }
                                else
                                {
                                    Game1.player.currentLocation.objects.Remove(new Vector2(x, y));
                                }
                            }
                        }
                    }
                }
                if (layersById.TryGetValue("Trees", out Layer treeLayer))
                {
                    for (int y = 0; y < treeLayer.LayerHeight; y++)
                    {
                        for (int x = 0; x < treeLayer.LayerWidth; x++)
                        {
                            if (treeLayer.Tiles[x, y] != null && treeLayer.Tiles[x, y].TileIndex >= 9 && Game1.player.currentLocation.terrainFeatures.ContainsKey(new Vector2(x, y)))
                            {
                                Game1.player.currentLocation.terrainFeatures.Remove(new Vector2(x, y));
                            }
                        }
                    }
                }
            }
        }



        private static void DoImport()
        {
            if (!import.Any())
            {
                SMonitor.Log("Import file(s) not found", LogLevel.Error);
                if (inGame == true)
                {
                    inGame = false;
                    Game1.chatBox.addMessage($"[{SModManifest.Name}]" + " " + "Import file(s) not found", Color.Red);
                }
                return;
            }

            for (var i = 0; i < import.Length; i++)
                    {
                        Map map = SHelper.ModContent.Load<Map>(Path.Combine(SHelper.DirectoryPath, "assets", $"{Path.GetFileName(import[i])}"));
                        if (map == null)
                        {
                            SMonitor.Log("Map is null", LogLevel.Error);
                            if (inGame == true)
                            {
                                inGame = false;
                                Game1.chatBox.addMessage($"[{SModManifest.Name}]" + " " + "Map is null", Color.Red);
                            }
                            return;
                        }
                        Dictionary<string, Layer> layersById = AccessTools.FieldRefAccess<Map, Dictionary<string, Layer>>(map, "m_layersById");
                        if (layersById.TryGetValue("Flooring", out Layer flooringLayer))
                        {
                            for (int y = 0; y < flooringLayer.LayerHeight; y++)
                            {
                                for (int x = 0; x < flooringLayer.LayerWidth; x++)
                                {
                                    if (flooringLayer.Tiles[x, y] != null && flooringLayer.Tiles[x, y].TileIndex >= 0)
                                    {
                                        Game1.player.currentLocation.terrainFeatures[new Vector2(x, y)] = new Flooring(flooringLayer.Tiles[x, y].TileIndex.ToString());
                                    }
                                }
                            }
                        }
                        if (trainTrackApi != null && layersById.TryGetValue("TrainTracks", out Layer trackLayer))
                        {

                            for (int y = 0; y < trackLayer.LayerHeight; y++)
                            {
                                for (int x = 0; x < trackLayer.LayerWidth; x++)
                                {
                                    if (trackLayer.Tiles[x, y] != null && trackLayer.Tiles[x, y].TileIndex >= 0)
                                    {
                                        PropertyValue switchData;
                                        PropertyValue speedData;
                                        trackLayer.Tiles[x, y].Properties.TryGetValue("Switches", out switchData);
                                        if (switchData != null)
                                        {
                                            SMonitor.Log($"Got switch data for tile {x},{y}: {switchData}");
                                        }
                                        trackLayer.Tiles[x, y].Properties.TryGetValue("Speed", out speedData);
                                        int speed = -1;
                                        if (speedData != null && int.TryParse(speedData, out speed))
                                        {
                                            SMonitor.Log($"Got speed for tile {x},{y}: {speed}");
                                        }
                                        trainTrackApi.TryPlaceTrack(Game1.currentLocation, new Vector2(x, y), trackLayer.Tiles[x, y].TileIndex, switchData == null ? null : switchData.ToString(), speed, true);
                                    }
                                }
                            }
                        }
                        if (layersById.TryGetValue("FluteBlocks", out Layer fluteLayer))
                        {
                            for (int y = 0; y < fluteLayer.LayerHeight; y++)
                            {
                                for (int x = 0; x < fluteLayer.LayerWidth; x++)
                                {
                                    if (fluteLayer.Tiles[x, y] != null && fluteLayer.Tiles[x, y].TileIndex >= 0 && !Game1.player.currentLocation.objects.ContainsKey(new Vector2(x, y)))
                                    {
                                        var block = new Object("464", 1, false, -1, 0);
                                        block.TileLocation = new Vector2(x, y);
                                        block.preservedParentSheetIndex.Value = (fluteLayer.Tiles[x, y].TileIndex % 24 * 100).ToString();
                                        if (fluteBlockApi != null)
                                        {
                                            var tone = fluteBlockApi.GetFluteBlockToneFromIndex(fluteLayer.Tiles[x, y].TileIndex / 24);
                                            if (tone != null)
                                            {
                                                block.modData["aedenthorn.AdvancedFluteBlocks/tone"] = tone;
                                            }
                                        }
                                        Game1.player.currentLocation.objects[new Vector2(x, y)] = block;
                                    }
                                }
                            }
                        }
                        if (layersById.TryGetValue("DrumBlocks", out Layer drumLayer))
                        {
                            for (int y = 0; y < drumLayer.LayerHeight; y++)
                            {
                                for (int x = 0; x < drumLayer.LayerWidth; x++)
                                {
                                    if (drumLayer.Tiles[x, y] != null && drumLayer.Tiles[x, y].TileIndex >= 0 && !Game1.player.currentLocation.objects.ContainsKey(new Vector2(x, y)))
                                    {
                                        var block = new Object("463", 1, false, -1, 0);
                                        block.TileLocation = new Vector2(x, y);
                                        block.preservedParentSheetIndex.Value = drumLayer.Tiles[x, y].TileIndex.ToString();
                                        Game1.player.currentLocation.objects[new Vector2(x, y)] = block;
                                    }
                                }
                            }
                        }
                        if (layersById.TryGetValue("Objects", out Layer objLayer))
                        {
                            var dict = SHelper.GameContent.Load<Dictionary<string, CropData>>("Data/Crops");

                            for (int y = 0; y < objLayer.LayerHeight; y++)
                            {
                                for (int x = 0; x < objLayer.LayerWidth; x++)
                                {
                                    if (objLayer.Tiles[x, y] != null && objLayer.Tiles[x, y].TileIndex >= 0 && !Game1.player.currentLocation.terrainFeatures.ContainsKey(new Vector2(x, y)) && !Game1.player.currentLocation.objects.ContainsKey(new Vector2(x, y)))
                                    {
                                        if (dict.TryGetValue(objLayer.Tiles[x, y].TileIndex.ToString(), out var cropData))
                                        {
                                            Crop crop = new Crop(objLayer.Tiles[x, y].TileIndex.ToString(), x, y, Game1.player.currentLocation);
                                            HoeDirt dirt = new HoeDirt(1, crop);
                                            Game1.player.currentLocation.terrainFeatures.Add(new Vector2(x, y), dirt);
                                            continue;
                                        }
                                        var cropkvp = dict.FirstOrDefault(kvp => kvp.Value.HarvestItemId == objLayer.Tiles[x, y].TileIndex.ToString());
                                        if (cropkvp.Value != null)
                                        {
                                            Crop crop = new Crop(cropkvp.Key, x, y, Game1.player.currentLocation);
                                            crop.growCompletely();
                                            HoeDirt dirt = new HoeDirt(1, crop);
                                            Game1.player.currentLocation.terrainFeatures.Add(new Vector2(x, y), dirt);
                                        }
                                        else
                                        {
                                            var obj = new Object(objLayer.Tiles[x, y].TileIndex.ToString(), 1, false, -1, 0);
                                            obj.TileLocation = new Vector2(x, y);
                                            Game1.player.currentLocation.objects[new Vector2(x, y)] = obj;
                                        }
                                    }
                                }
                            }
                        }
                        if (layersById.TryGetValue("Trees", out Layer treeLayer))
                        {
                            for (int y = 0; y < treeLayer.LayerHeight; y++)
                            {
                                for (int x = 0; x < treeLayer.LayerWidth; x++)
                                {
                                    Tree tree;
                                    if (treeLayer.Tiles[x, y] != null && treeLayer.Tiles[x, y].TileIndex >= 9 && !Game1.player.currentLocation.terrainFeatures.ContainsKey(new Vector2(x, y)))
                                    {
                                        switch (treeLayer.Tiles[x, y].TileIndex)
                                        {
                                            case 9:
                                                tree = new Tree("1", 5);
                                                break;
                                            case 10:
                                                tree = new Tree("2", 5);
                                                break;
                                            case 11:
                                                tree = new Tree("3", 5);
                                                break;
                                            case 12:
                                                tree = new Tree("6", 5);
                                                break;
                                            case 31:
                                                tree = new Tree("7", 5);
                                                break;
                                            case 32:
                                                tree = new Tree("9", 5);
                                                break;
                                            default:
                                                continue;
                                        }
                                        Game1.player.currentLocation.terrainFeatures[new Vector2(x, y)] = tree;
                                    }
                                }
                            }
                        }
                    }
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            fluteBlockApi = Helper.ModRegistry.GetApi<IAdvancedFluteBlocksApi>("aedenthorn.AdvancedFluteBlocks");
            trainTrackApi = Helper.ModRegistry.GetApi<ITrainTracksApi>("aedenthorn.TrainTracks");

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
                name: () => "Mod Enabled?",
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );
            configMenu.AddKeybindList(
                mod: ModManifest,
                name: () => "Import Key",
                getValue: () => Config.ImportKey,
                setValue: value => Config.ImportKey = value
            );
            configMenu.AddKeybindList(
                mod: ModManifest,
                name: () => "Clear Import Key",
                getValue: () => Config.ClearImportKey,
                setValue: value => Config.ImportKey = value
            );

            import = Directory.EnumerateFiles(Path.Combine(Helper.DirectoryPath, "assets")).Where(import => Path.GetFileName(import).Equals("import.tmx") || Path.GetFileName(import).StartsWith("import") && Path.GetFileName(import).EndsWith(".tmx")).ToArray();
        }
    }
}
