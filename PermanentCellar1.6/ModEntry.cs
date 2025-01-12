using System;
using System.Collections.Generic;
using System.Linq;
using static System.StringComparer;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using xTile;
using xTile.ObjectModel;




namespace PermanentCellar
{
    public interface ISaveAnywhereApi
    {
        event EventHandler AfterLoad;
    }

    public static class MessageId
    {
        public const string RemoveCabinCasks = nameof(RemoveCabinCasks);
        public const string AddCabinCasks = nameof(AddCabinCasks);
    }


    public class ModEntry : Mod
    {
        private ModConfigHost hostConfig_;
        private ModConfigClient clientConfig_;
        private static IMonitor SMonitor;
        private string saveGameName_;
        private Map cellarStairsMap0;
        private Map cellarStairsMap1;
        private PropertyValue Cellar0Exit;
        private PropertyValue Cellar1Exit;
        private float CE0XPosition1;
        private float CE0YPosition1;
        private float CE0XPosition2;
        private float CE0YPosition2;
        private float CE1XPosition1;
        private float CE1YPosition1;
        private float CE1XPosition2;
        private float CE1YPosition2;

        internal class ModConfigHost
        {
            public IDictionary<string, ConfigEntryHost> SaveGame { get; set; }
                 = new Dictionary<string, ConfigEntryHost>();
        }

        internal class ModConfigClient
        {
            public IDictionary<string, ConfigEntryClient> SaveGame { get; set; }
                 = new Dictionary<string, ConfigEntryClient>();
        }

        internal class ConfigEntryHost
        {
            public bool ShowCommunityUpgrade { get; set; } = false;
            public bool RemoveCellarCasks { get; set; } = false;
            public bool AddCellarCasks { get; set; } = false;
        }

        internal class ConfigEntryClient
        {
            public bool RemoveCellarCasks { get; set; } = false;
            public bool AddCellarCasks { get; set; } = false;
        }

        public override void Entry(IModHelper helper)
        {
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            Helper.Events.GameLoop.Saving += OnSaving;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.GameLoop.DayStarted += OnDayStarted2;
            Helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            Helper.Events.Player.Warped += OnWarped;
            Helper.Events.Player.Warped += OnWarped2;
            Helper.Events.Content.AssetRequested += OnAssetRequested;
            Helper.Events.Content.AssetReady += OnAssetReady;
            Helper.Events.Display.MenuChanged += OnMenuChanged;
            Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;


            SMonitor = Monitor;
        }

        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID)
                return;

            SMonitor.Log($"[{(Context.IsMainPlayer ? "host" : "farmhand")}] Received {e.Type} from {e.FromPlayerID}.", LogLevel.Trace);

            switch (e.Type)
            {
                case MessageId.RemoveCabinCasks:
                    if (Context.IsMainPlayer)
                    {
                        foreach (Cabin cabin in GetLocations().OfType<Cabin>())
                            if (cabin.OwnerId == e.FromPlayerID)
                            {
                                var cellar = cabin.GetCellar();

                                cabin.GetCellar().Objects
                                  .Pairs
                                  .Where(item => item.Value is Cask)
                                  .Select(item => item.Key)
                                  .ToList()
                                  .ForEach(key => cellar.Objects.Remove(key));
                            }
                    }
                    break;

                case MessageId.AddCabinCasks:
                    if (Context.IsMainPlayer)
                    {
                        foreach (Cabin cabin in GetLocations().OfType<Cabin>())
                            if (cabin.OwnerId == e.FromPlayerID)
                            {
                                cabin.GetCellar().setUpAgingBoards();
                            }
                    }
                    break;
            }
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var saveAnywhereApi = Helper.ModRegistry.GetApi<ISaveAnywhereApi>("Omegasis.SaveAnywhere");


            if (saveAnywhereApi != null)
            {
                saveAnywhereApi.AfterLoad += OnAfterLoad;
                saveAnywhereApi.AfterLoad += OnAfterLoad2;
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (Game1.IsMasterGame && Context.ScreenId == 0)
            {
                hostConfig_ = Helper.ReadConfig<ModConfigHost>();

                saveGameName_ = $"{Constants.SaveFolderName}";

                if (!hostConfig_.SaveGame.ContainsKey(saveGameName_))
                {
                    hostConfig_.SaveGame.Add(saveGameName_, new ConfigEntryHost());
                    Helper.WriteConfig(hostConfig_);
                }

                if (hostConfig_.SaveGame[saveGameName_].RemoveCellarCasks)
                {
                    FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.MasterPlayer);
                    GameLocation cellar = farmHouse.GetCellar();

                    cellar.Objects
                          .Pairs
                          .Where(item => item.Value is Cask)
                          .Select(item => item.Key)
                          .ToList()
                          .ForEach(key => cellar.Objects.Remove(key));
                }
                if (hostConfig_.SaveGame[saveGameName_].AddCellarCasks)
                {
                    FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.MasterPlayer);

                    farmHouse.GetCellar().setUpAgingBoards();
                }
            }

            if (!Game1.IsMasterGame)
            {
                clientConfig_ = Helper.ReadConfig<ModConfigClient>();

                saveGameName_ = $"{Game1.player.farmName}_{Game1.uniqueIDForThisGame}";

                if (!clientConfig_.SaveGame.ContainsKey(saveGameName_))
                {
                    clientConfig_.SaveGame.Add(saveGameName_, new ConfigEntryClient());
                    Helper.WriteConfig(clientConfig_);
                }

                if (clientConfig_.SaveGame[saveGameName_].RemoveCellarCasks)
                {
                    Helper.Multiplayer.SendMessage("RemoveCabinCasks", MessageId.RemoveCabinCasks, modIDs: new[] { ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });

                    clientConfig_.SaveGame[saveGameName_].RemoveCellarCasks = false;
                    Helper.WriteConfig(clientConfig_);
                }
                if (clientConfig_.SaveGame[saveGameName_].AddCellarCasks)
                {
                    Helper.Multiplayer.SendMessage("AddCabinCasks", MessageId.AddCabinCasks, modIDs: new[] { ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });

                    clientConfig_.SaveGame[saveGameName_].AddCellarCasks = false;
                    Helper.WriteConfig(clientConfig_);
                }

            }

            if (Context.IsSplitScreen && Context.ScreenId > 0)
            {
                hostConfig_ = Helper.ReadConfig<ModConfigHost>();

                saveGameName_ = $"{Constants.SaveFolderName}";

                if (!hostConfig_.SaveGame.ContainsKey(saveGameName_))
                {
                    hostConfig_.SaveGame.Add(saveGameName_, new ConfigEntryHost());
                    Helper.WriteConfig(hostConfig_);
                }

                if (hostConfig_.SaveGame[saveGameName_].RemoveCellarCasks)
                {
                    Helper.Multiplayer.SendMessage("RemoveCabinCasks", MessageId.RemoveCabinCasks, modIDs: new[] { ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });

                }
                if (hostConfig_.SaveGame[saveGameName_].AddCellarCasks)
                {
                    Helper.Multiplayer.SendMessage("AddCabinCasks", MessageId.AddCabinCasks, modIDs: new[] { ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });

                }

            }

            cellarStairsMap0 = Game1.content.Load<Map>("Maps/FarmHouse_Cellar0");    
            cellarStairsMap1 = Game1.content.Load<Map>("Maps/FarmHouse_Cellar1");
            

            Helper.GameContent.InvalidateCache("Maps\\FarmHouse");
            Helper.GameContent.InvalidateCache("Maps\\FarmHouse1");
            Helper.GameContent.InvalidateCache("Maps\\FarmHouse1_marriage");
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            if (Game1.IsMasterGame)
            {
                if (hostConfig_.SaveGame[saveGameName_].RemoveCellarCasks)
                {
                    hostConfig_.SaveGame[saveGameName_].RemoveCellarCasks = false;
                    Helper.WriteConfig(hostConfig_);
                }

                if (hostConfig_.SaveGame[saveGameName_].AddCellarCasks)
                {
                    hostConfig_.SaveGame[saveGameName_].AddCellarCasks = false;
                    Helper.WriteConfig(hostConfig_);
                }
            }

            if (!Game1.IsMasterGame)
            {

                if (clientConfig_.SaveGame[saveGameName_].RemoveCellarCasks)
                {
                    clientConfig_.SaveGame[saveGameName_].RemoveCellarCasks = false;
                    Helper.WriteConfig(clientConfig_);
                }

                if (clientConfig_.SaveGame[saveGameName_].AddCellarCasks)
                {
                    clientConfig_.SaveGame[saveGameName_].AddCellarCasks = false;
                    Helper.WriteConfig(clientConfig_);
                }
            }


            Helper.GameContent.InvalidateCache("Maps\\FarmHouse");
            Helper.GameContent.InvalidateCache("Maps\\FarmHouse1");
            Helper.GameContent.InvalidateCache("Maps\\FarmHouse1_marriage");
        }


        [EventPriority((EventPriority)int.MinValue)]
        private void OnAfterLoad(object sender, EventArgs e)
        {
            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.MasterPlayer);


            if (Context.IsWorldReady && !Game1.newDay && Game1.player.currentLocation == farmHouse && farmHouse.upgradeLevel < 3)
            {
                CreateCellarEntrance(farmHouse);
            }

            foreach (Cabin cabin in GetLocations().OfType<Cabin>())
                if (Context.IsWorldReady && !Game1.newDay && Game1.player.currentLocation == cabin && cabin.upgradeLevel < 3)
                {
                    CreateCellarEntrance(cabin);
                }

            Helper.GameContent.InvalidateCache("Maps\\FarmHouse");
            Helper.GameContent.InvalidateCache("Maps\\FarmHouse1");
            Helper.GameContent.InvalidateCache("Maps\\FarmHouse1_marriage");
        }


        [EventPriority(EventPriority.Normal)]
        private void OnAfterLoad2(object sender, EventArgs e)
        {
            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.MasterPlayer);


            if (Context.IsWorldReady && !Game1.newDay && Game1.player.currentLocation == farmHouse.GetCellar() && farmHouse.upgradeLevel < 3)
            {
                CreateCellarExitWarps(farmHouse);
            }

            foreach (Cabin cabin in GetLocations().OfType<Cabin>())
                if (Context.IsWorldReady && !Game1.newDay && Game1.player.currentLocation == cabin.GetCellar() && cabin.upgradeLevel < 3)
                {
                    CreateCellarExitWarps(cabin);
                }
        }


        [EventPriority((EventPriority)int.MinValue)]
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.MasterPlayer);

            if (Game1.year == 1 && Game1.dayOfMonth == 1 && Game1.IsSpring)
            {
                Game1.updateCellarAssignments();
            }

            if (!Game1.player.craftingRecipes.ContainsKey("Cask"))
            {
                Game1.player.craftingRecipes.Add("Cask", 0);
            }

            if (Game1.player.currentLocation == farmHouse && farmHouse.upgradeLevel < 3)
            {
                CreateCellarEntrance(farmHouse);
            }

            foreach (Cabin cabin in GetLocations().OfType<Cabin>())
            if (Game1.player.currentLocation == cabin && cabin.upgradeLevel < 3)
            {
                CreateCellarEntrance(cabin);
            }
        }


        [EventPriority(EventPriority.Normal)]
        private void OnDayStarted2(object sender, DayStartedEventArgs e)
        {
            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.MasterPlayer);

            if (Game1.player.currentLocation == farmHouse.GetCellar() && farmHouse.upgradeLevel < 3)
            {
                CreateCellarExitWarps(farmHouse);
            }

            foreach (Cabin cabin in GetLocations().OfType<Cabin>())
                if (Game1.player.currentLocation == cabin.GetCellar() && cabin.upgradeLevel < 3)
                {
                    CreateCellarExitWarps(cabin);
                }

        }


        [EventPriority((EventPriority)int.MinValue)]
        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.MasterPlayer);

            if (Game1.player.currentLocation == farmHouse && Game1.timeOfDay != 600 && farmHouse.upgradeLevel < 3)
            {
                CreateCellarEntrance(farmHouse);
            }

            foreach (Cabin cabin in GetLocations().OfType<Cabin>())
            if (Game1.player.currentLocation == cabin && Game1.timeOfDay != 600 && cabin.upgradeLevel < 3)
            {
                CreateCellarEntrance(cabin);
            }

        }


        [EventPriority((EventPriority)int.MinValue)]
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.MasterPlayer);

            if (e.NewLocation == farmHouse && farmHouse.upgradeLevel < 3)
            {
                CreateCellarEntrance(farmHouse);
            }


            foreach (Cabin cabin in GetLocations().OfType<Cabin>())
                if (e.NewLocation == cabin && cabin.upgradeLevel < 3)
                {
                    CreateCellarEntrance(cabin);
                }
        }


        [EventPriority(EventPriority.Normal)]
        private void OnWarped2(object sender, WarpedEventArgs e)
        {
            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.MasterPlayer);

            if (e.NewLocation == farmHouse.GetCellar() && farmHouse.upgradeLevel < 3)
            {
                CreateCellarExitWarps(farmHouse);
            }


            foreach (Cabin cabin in GetLocations().OfType<Cabin>())
                if (e.NewLocation == cabin.GetCellar() && cabin.upgradeLevel < 3)
                {
                    CreateCellarExitWarps(cabin);
                }
        }


        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {

            if (!Game1.IsMasterGame || hostConfig_ == null || !hostConfig_.SaveGame[saveGameName_].ShowCommunityUpgrade)
                return;


            bool ccIsComplete = Game1.MasterPlayer.mailReceived.Contains("ccIsComplete") ||
                                Game1.MasterPlayer.hasCompletedCommunityCenter();
            bool jojaMember = Game1.MasterPlayer.mailReceived.Contains("JojaMember");

            bool communityUpgradeInProgress = (Game1.getLocationFromName("Town") as Town).daysUntilCommunityUpgrade.Value > 0;
            bool pamHouseUpgrade = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade");
            bool communityUpgradeShortcuts = Game1.MasterPlayer.mailReceived.Contains("communityUpgradeShortcuts");

            if ((!ccIsComplete && !jojaMember) || communityUpgradeInProgress || (pamHouseUpgrade && communityUpgradeShortcuts))
            {
                return;
            }

            if (e.NewMenu is DialogueBox dialogue)
            {
                string text = dialogue.dialogues.FirstOrDefault();
                string menuText = Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu");
                if (text == menuText)
                {
                    Response upgrade = dialogue.responses.FirstOrDefault(r => r.responseKey == "Upgrade");
                    Response communityUpgrade = dialogue.responses.FirstOrDefault(r => r.responseKey == "CommunityUpgrade");
                    if (upgrade == null || communityUpgrade != null)
                    {
                        return;
                    }

                    upgrade.responseKey = "CommunityUpgrade";
                    upgrade.responseText = Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_CommunityUpgrade");
                }
            }
        }


        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Maps/FarmHouse_Cellar0"))
            {
                e.LoadFromModFile<Map>("assets/FarmHouse_Cellar0.tmx", AssetLoadPriority.Medium);

            }
            if (e.Name.IsEquivalentTo("Maps/FarmHouse_Cellar1"))
            {
                e.LoadFromModFile<Map>("assets/FarmHouse_Cellar1.tmx", AssetLoadPriority.Medium);
            }
            if (e.Name.IsEquivalentTo("Maps/FarmHouse"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();

                    editor.ExtendMap(minHeight: 13);

                }, AssetEditPriority.Early + -1000);
            }
            if (e.Name.IsEquivalentTo("Maps/FarmHouse1") || e.Name.IsEquivalentTo("Maps/FarmHouse1_marriage"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();

                    editor.ExtendMap(minHeight: 13);
                }, AssetEditPriority.Early + -1000);
            }
        }

        private void OnAssetReady(object sender, AssetReadyEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Maps/FarmHouse_Cellar0"))
            {
                cellarStairsMap0 = Game1.content.Load<Map>("Maps/FarmHouse_Cellar0");
            }

            if (e.Name.IsEquivalentTo("Maps/FarmHouse_Cellar1"))
            {
                cellarStairsMap1 = Game1.content.Load<Map>("Maps/FarmHouse_Cellar1");
            }
        }


        public static IEnumerable<GameLocation> GetLocations()
        {
            return Game1.locations
                .Concat(
                    from location in Game1.locations
                    from building in location.buildings
                    where building.indoors.Value != null
                    select building.indoors.Value
                );
        }


        [EventPriority((EventPriority)int.MinValue)]
        private void CreateCellarEntrance(FarmHouse farmHouse)
        {
            GetCellarExitProperty();


            if (farmHouse.upgradeLevel >= 3)
            {
                return;
            }

            if (farmHouse.upgradeLevel == 0)
            {
                if (Helper.Reflection.GetField<HashSet<string>>(farmHouse, "_appliedMapOverrides").GetValue().Contains("cellar"))
                    Helper.Reflection.GetField<HashSet<string>>(farmHouse, "_appliedMapOverrides").GetValue().Remove("cellar");

                farmHouse.ApplyMapOverride("FarmHouse_Cellar0", "cellar");
                farmHouse.createCellarWarps();
                farmHouse.ReadWallpaperAndFloorTileData();
                farmHouse.setFloors();
            }
            else if (farmHouse.upgradeLevel == 1)
            {
                if (Helper.Reflection.GetField<HashSet<string>>(farmHouse, "_appliedMapOverrides").GetValue().Contains("cellar"))
                    Helper.Reflection.GetField<HashSet<string>>(farmHouse, "_appliedMapOverrides").GetValue().Remove("cellar");

                farmHouse.ApplyMapOverride("FarmHouse_Cellar1", "cellar");
                farmHouse.createCellarWarps();
                farmHouse.ReadWallpaperAndFloorTileData();
                farmHouse.setFloors();
            }
            else if (farmHouse.upgradeLevel == 2)
            {
                farmHouse.upgradeLevel = 3;
                farmHouse.updateFarmLayout();

            }
        }


        [EventPriority((EventPriority)int.MinValue)]
        private void CreateCellarExitWarps(FarmHouse farmHouse)
        {
            if (GetDefaultCellarExitWarps(farmHouse) != null)
            {
                Tuple<Warp, Warp> warps = GetDefaultCellarExitWarps(farmHouse);

                Helper.ModContent.GetPatchHelper(cellarStairsMap0).AsMap().Data.Properties.TryGetValue("CellarExit", out Cellar0Exit);
                Helper.ModContent.GetPatchHelper(cellarStairsMap1).AsMap().Data.Properties.TryGetValue("CellarExit", out Cellar1Exit);

                GetCellarExitProperty();


                if (farmHouse.upgradeLevel == 0)
                {
                    if (CE0XPosition1 != 0 && CE0YPosition1 != 0)
                    {
                        warps.Item1.TargetX = (int)CE0XPosition1;
                        warps.Item1.TargetY = (int)CE0YPosition1;
                    }

                    if (CE0XPosition2 == 0 && CE0YPosition2 == 0 && CE0XPosition1 != 0 && CE0YPosition1 != 0)
                    {
                        warps.Item2.TargetX = (int)CE0XPosition1;
                        warps.Item2.TargetY = (int)CE0YPosition1;
                    }
                    else if (CE0XPosition2 != 0 && CE0YPosition2 != 0)
                    {
                        warps.Item2.TargetX = (int)CE0XPosition2;
                        warps.Item2.TargetY = (int)CE0YPosition2;
                    }
                }
                else if (farmHouse.upgradeLevel == 1)
                {
                    if (CE1XPosition1 != 0 && CE1YPosition1 != 0)
                    {
                        warps.Item1.TargetX = (int)CE1XPosition1;
                        warps.Item1.TargetY = (int)CE1YPosition1;
                    }

                    if (CE1XPosition2 == 0 && CE1YPosition2 == 0 && CE1XPosition1 != 0 && CE1YPosition1 != 0)
                    {
                        warps.Item2.TargetX = (int)CE1XPosition1;
                        warps.Item2.TargetY = (int)CE1YPosition1;
                    }
                    else if (CE1XPosition2 != 0 && CE1YPosition2 != 0)
                    {
                        warps.Item2.TargetX = (int)CE1XPosition2;
                        warps.Item2.TargetY = (int)CE1YPosition2;
                    }
                }
            }
        }

        private void GetCellarExitProperty()
        {
            if (Cellar0Exit != null && Cellar0Exit != "" && Cellar0Exit != " ")
            {
                string[] CE0xyVals = Cellar0Exit.ToString().Split();
                CE0XPosition1 = float.Parse(CE0xyVals[0]);
                CE0YPosition1 = float.Parse(CE0xyVals[1]);
                try
                {
                    CE0XPosition2 = float.Parse(CE0xyVals[2]);
                    CE0YPosition2 = float.Parse(CE0xyVals[3]);
                }
                catch { }
            }

            if (Cellar1Exit != null && Cellar1Exit != "" && Cellar1Exit != " ")
            {
                string[] CE1xyVals = Cellar1Exit.ToString().Split();
                CE1XPosition1 = float.Parse(CE1xyVals[0]);
                CE1YPosition1 = float.Parse(CE1xyVals[1]);
                try
                {
                    CE1XPosition2 = float.Parse(CE1xyVals[2]);
                    CE1YPosition2 = float.Parse(CE1xyVals[3]);
                }
                catch { }

            }
        }

        private static Tuple<Warp, Warp> GetDefaultCellarExitWarps(FarmHouse farmHouse)
        {
                GameLocation cellar = farmHouse.GetCellar();

                try
                {
                    Warp warp1 = cellar.warps.First(warp =>
                    {
                        return OrdinalIgnoreCase.Equals(warp.TargetName, farmHouse.NameOrUniqueName);
                    });

                    Warp warp2 = cellar.warps.Skip(1).First(warp =>
                    {
                        return OrdinalIgnoreCase.Equals(warp.TargetName, farmHouse.NameOrUniqueName);
                    });

                    return Tuple.Create(warp1, warp2);
                }
                catch
                {
                    SMonitor.Log("The cellar map doesn't have the required warp points. Unable to alter target co-ordinates", LogLevel.Warn);
                    return null;
                }
        }

    }

}

