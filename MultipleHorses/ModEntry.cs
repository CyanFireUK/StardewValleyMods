using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.GameData.Buildings;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;





namespace MultipleHorses
{
    public interface ISaveAnywhereApi
    {
        event EventHandler AfterLoad;
    }

    class ModConfig
    {
        public SButton HorseWhistleKey { get; set; } = SButton.R;
        public SButton CorralKey { get; set; } = SButton.G;
        public SButton DefaultHorseKey { get; set; } = SButton.H;
        public bool HorseWhistleAnywhere { get; set; } = false;
        public bool AutoMountHorse { get; set; } = false;
    }

        public class ModEntry : Mod
    {
        private Horse horseToBeNamed;
        private Stable stableToBeAssigned;
        internal static IModHelper SHelper;
        internal static IMonitor SMonitor;
        internal static IManifest SModManifest;
        internal static ModConfig Config;



        public override void Entry(IModHelper helper)
        {
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.Input.ButtonReleased += OnButtonReleased;
            Helper.Events.Content.AssetRequested += OnAssetRequested;


            helper.ConsoleCommands.Add("corral_horses", Helper.Translation.Get("command.corral_horses.description"), OnCommandReceived);
            helper.ConsoleCommands.Add("horse_whistle", Helper.Translation.Get("command.horse_whistle.description"), OnCommandReceived);
            helper.ConsoleCommands.Add("default_horse", Helper.Translation.Get("command.default_horse.description"), OnCommandReceived);

            Config = Helper.ReadConfig<ModConfig>();

            SHelper = Helper;
            SMonitor = Monitor;
            SModManifest = ModManifest;


            var harmony = new Harmony(ModManifest.UniqueID);

            harmony.Patch(
                              original: AccessTools.Method(typeof(NPC), "findPlayer", Array.Empty<Type>()),
                              prefix: new HarmonyMethod(typeof(NPCPatches), nameof(NPCPatches.findPlayer_prefix))
                                );


        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var saveAnywhereApi = Helper.ModRegistry.GetApi<ISaveAnywhereApi>("Omegasis.SaveAnywhere");

            if (saveAnywhereApi != null)
            {
                saveAnywhereApi.AfterLoad += OnAfterLoad;
            }

            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
               mod: ModManifest,
               reset: () => Config = new ModConfig(),
               save: () => Helper.WriteConfig(Config)
               );

            configMenu.AddKeybind(
               mod: ModManifest,
               name: () => Helper.Translation.Get("config.HorseWhistleKey.name"),
               tooltip: () => Helper.Translation.Get("config.HorseWhistleKey.description"),
               getValue: () => Config.HorseWhistleKey,
               setValue: value => Config.HorseWhistleKey = value
               );

            configMenu.AddKeybind(
               mod: ModManifest,
               name: () => Helper.Translation.Get("config.CorralKey.name"),
               tooltip: () => Helper.Translation.Get("config.CorralKey.description"),
               getValue: () => Config.CorralKey,
               setValue: value => Config.CorralKey = value
               );

            configMenu.AddKeybind(
               mod: ModManifest,
               name: () => Helper.Translation.Get("config.DefaultHorseKey.name"),
               tooltip: () => Helper.Translation.Get("config.DefaultHorseKey.description"),
               getValue: () => Config.DefaultHorseKey,
               setValue: value => Config.DefaultHorseKey = value
               );

            configMenu.AddBoolOption(
               mod: ModManifest,
               name: () => Helper.Translation.Get("config.HorseWhistleAnywhere.name"),
               tooltip: () => Helper.Translation.Get("config.HorseWhistleAnywhere.description"),
               getValue: () => Config.HorseWhistleAnywhere,
               setValue: value => Config.HorseWhistleAnywhere = value
               );

            configMenu.AddBoolOption(
               mod: ModManifest,
               name: () => Helper.Translation.Get("config.AutoMountHorse.name"),
               tooltip: () => Helper.Translation.Get("config.AutoMountHorse.description"),
               getValue: () => Config.AutoMountHorse,
               setValue: value => Config.AutoMountHorse = value
               );
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
            {
                e.Edit(editor =>
                {

                    var data = editor.AsDictionary<string, BuildingData>().Data;

                    foreach ((string buildingID, BuildingData buildingData) in data)
                        if (buildingID == "Stable")
                        {
                            buildingData.BuildCondition = null;
                        }

                });
            }
        }


        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            var saveAnywhereApi = Helper.ModRegistry.GetApi<ISaveAnywhereApi>("Omegasis.SaveAnywhere");

            foreach (GameLocation location in Game1.locations)
                foreach (Horse horse in location.characters.OfType<Horse>())
                    foreach (Stable stable in location.buildings.OfType<Stable>())
                        if (stable == horse.TryFindStable() && !string.IsNullOrEmpty(horse.Name) && saveAnywhereApi == null)
                        {
                            stable.modData.TryGetValue($"{ModManifest.UniqueID}.owner", out string Owner);

                            if (stable.owner.Value == 0 && !string.IsNullOrEmpty(Owner))
                            {
                                stable.owner.Value = long.Parse(Owner);
                            }
                            if (horse.ownerId.Value == 0 && (!horse.Name.StartsWith("tractor") || !horse.Name.StartsWith("Motorcycle")))
                            {
                                horse.ownerId.Value = stable.owner.Value;
                            }
                        }
        }

        private void OnAfterLoad(object sender, EventArgs e)
        {
            foreach (GameLocation location in Game1.locations)
                foreach (Horse horse in location.characters.OfType<Horse>())
                    foreach (Stable stable in location.buildings.OfType<Stable>())
                        if (stable == horse.TryFindStable() && !string.IsNullOrEmpty(horse.Name))
                        {
                            stable.modData.TryGetValue($"{ModManifest.UniqueID}.owner", out string Owner);

                            if (stable.owner.Value == 0 && !string.IsNullOrEmpty(Owner))
                            {
                                stable.owner.Value = long.Parse(Owner);
                            }
                            if (horse.ownerId.Value == 0 && (!horse.Name.StartsWith("tractor") || !horse.Name.StartsWith("Motorcycle")))
                            {
                                horse.ownerId.Value = stable.owner.Value;
                            }

                        }

        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {

            foreach (GameLocation location in Game1.locations)
                foreach (Horse horse in location.characters.OfType<Horse>())
                    foreach (Stable stable in location.buildings.OfType<Stable>())
                    {
                        if (Context.IsMainPlayer)
                        {
                            if ((e.Button.Equals(SButton.MouseRight) || (e.Button.Equals(SButton.ControllerA)) || e.Button.IsActionButton()) &&
                                horse.withinPlayerThreshold(2) && stable == horse.TryFindStable() && string.IsNullOrEmpty(horse.Name))
                            {
                                horseToBeNamed = horse;
                                stableToBeAssigned = stable;

                                Game1.activeClickableMenu = new NamingMenu(HorseNamer, Game1.content.LoadString("Strings\\Characters:NameYourHorse"), Game1.content.LoadString("Strings\\Characters:DefaultHorseName"));

                            }
                        }
                        else
                        {
                            if ((e.Button.Equals(SButton.MouseRight) || (e.Button.Equals(SButton.ControllerA)) || e.Button.IsActionButton()) &&
                                horse.withinPlayerThreshold(2) && stable == horse.TryFindStable() && horse.ownerId.Value == 0 && string.IsNullOrEmpty(horse.Name))
                            {
                                horseToBeNamed = horse;
                                stableToBeAssigned = stable;

                                Game1.activeClickableMenu = new NamingMenu(HorseNamer, Game1.content.LoadString("Strings\\Characters:NameYourHorse"), Game1.content.LoadString("Strings\\Characters:DefaultHorseName"));

                            }
                        }
                    }
        }

        internal static void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;


                    if (e.Button == Config.HorseWhistleKey && !Game1.IsChatting)
                    {
                        if (CallHorse())
                           return;

                        if (Game1.player.mount != null)
                           return;

                        Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HorseFlute_NoHorse"));
                        SMonitor.Log(SHelper.Translation.Get("command.horse_whistle.nohorse"), LogLevel.Error);
                    }

                    if (e.Button == Config.DefaultHorseKey && !Game1.IsChatting)
                    {
                        SetDefaultHorse();
                    }

            if (e.Button == Config.CorralKey && !Game1.IsChatting)
            {
                CorralHorses();
            }
        }




        internal void HorseNamer(string horseName)
        {
            if (horseName.Length <= 0)
            {
                return;
            }

            foreach (GameLocation location in Game1.locations)
                foreach (Horse horse in location.characters.OfType<Horse>())
            if (string.Equals(horseName, horse.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }
            Game1.Multiplayer.globalChatInfoMessage("HorseNamed", Game1.player.Name, horseName);
            horseToBeNamed.Name = horseName;
            horseToBeNamed.displayName = horseName;
            horseToBeNamed.ownerId.Value = Game1.player.UniqueMultiplayerID;
            stableToBeAssigned.owner.Value = Game1.player.UniqueMultiplayerID;
            stableToBeAssigned.modData[$"{ModManifest.UniqueID}.owner"] = Game1.player.UniqueMultiplayerID.ToString();


            if (Game1.player.horseName.Value == null)
            {
                Game1.player.horseName.Value = horseName;
            }

            Game1.exitActiveMenu();
            Game1.playSound("newArtifact");
            if (horseToBeNamed.mutex.IsLockHeld())
            {
                horseToBeNamed.mutex.ReleaseLock();
            }

        }


        [HarmonyPatch(typeof(NPC), "findPlayer")]
        public class NPCPatches
        {
            public static bool findPlayer_prefix(NPC __instance, ref Farmer __result)
            {
                try
                {
                    if (Context.IsMultiplayer && __instance is Horse)
                    {
                        __result = Game1.player;
                        return false;
                    }
                    else
                        return true;
                }
                catch
                {
                    return true;
                }
            }
        }

        internal static bool CallHorse(string name = null)
        {

            foreach (GameLocation location in Game1.locations)
            {
                List<Horse> taxis = location.characters.OfType<Horse>().ToList();
                taxis.Reverse();
                foreach (Horse taxi in taxis)
                {
                    if (Config.HorseWhistleAnywhere == false && !Game1.player.currentLocation.IsOutdoors && (taxi.ownerId.Value == Game1.player.UniqueMultiplayerID || taxi.ownerId.Value == 0) && !string.IsNullOrEmpty(taxi.Name) && Game1.player.mount == null)
                    {
                        Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HorseFlute_InvalidLocation"));
                        return true;
                    }

                    if (name != null && string.Equals(taxi.Name, name, StringComparison.InvariantCultureIgnoreCase) && Game1.player.mount == null)
                    {
                        if (taxi.ownerId.Value != Game1.player.UniqueMultiplayerID)
                        {
                            SMonitor.Log(SHelper.Translation.Get("command.error.doesnotbelong", new { mountName = taxi.Name, ownerName = taxi.getOwner().Name }), LogLevel.Error);
                            return true;
                        }

                        Game1.player.faceDirection(2);
                        Game1.MusicDuckTimer = 2000f;
                        Game1.playSound("horse_flute");
                        Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[6]
                        {
                              new (98, 400, true, false),
                              new (99, 200, true, false),
                              new (100, 200, true, false),
                              new (99, 200, true, false),
                              new (98, 400, true, false),
                              new (99, 200, true, false)
                        });
                        Game1.player.freezePause = 1500;
                        DelayedAction.functionAfterDelay(() =>
                        {
                            GameLocation currentLocation1 = taxi.currentLocation;
                            Vector2 tile1 = taxi.Tile;
                            for (int index = 0; index < 8; ++index)
                                Game1.Multiplayer.broadcastSprites(currentLocation1, new TemporaryAnimatedSprite(10, new Vector2(tile1.X + Utility.RandomFloat(-1f, 1f), tile1.Y + Utility.RandomFloat(-1f, 0.0f)) * 64f, Color.White, animationInterval: 50f)
                                {
                                    layerDepth = 1f,
                                    motion = new Vector2(Utility.RandomFloat(-0.5f, 0.5f), Utility.RandomFloat(-0.5f, 0.5f))
                                });
                            currentLocation1.playSound("wand", new Vector2?(taxi.Tile));
                            GameLocation currentLocation2 = Game1.player.currentLocation;
                            Vector2 tile2 = Game1.player.Tile;
                            currentLocation2.playSound("wand", new Vector2?(tile2));
                            for (int index = 0; index < 8; ++index)
                                Game1.Multiplayer.broadcastSprites(currentLocation2, new TemporaryAnimatedSprite(10, new Vector2(tile2.X + Utility.RandomFloat(-1f, 1f), tile2.Y + Utility.RandomFloat(-1f, 0.0f)) * 64f, Color.White, animationInterval: 50f)
                                {
                                    layerDepth = 1f,
                                    motion = new Vector2(Utility.RandomFloat(-0.5f, 0.5f), Utility.RandomFloat(-0.5f, 0.5f))
                                });
                            Game1.warpCharacter(taxi, Game1.player.currentLocation, tile2);
                            int num = 0;
                            for (int x = (int)tile2.X + 3; x >= (int)tile2.X - 3; --x)
                            {
                                Game1.Multiplayer.broadcastSprites(currentLocation2, new TemporaryAnimatedSprite(6, new Vector2(x, tile2.Y) * 64f, Color.White, animationInterval: 50f)
                                {
                                    layerDepth = 1f,
                                    delayBeforeAnimationStart = num * 25,
                                    motion = new Vector2(-0.25f, 0.0f)
                                });
                                ++num;
                            }
                        }, 1500);
                        if (Config.AutoMountHorse == true)
                            DelayedAction.functionAfterDelay(() => tryMountHorse(Game1.player, taxi, Game1.player.currentLocation), 2000);
                        return true;
                    }
                    else if (name == null && (taxi.ownerId.Value == Game1.player.UniqueMultiplayerID || taxi.ownerId.Value == 0) && Game1.player.modData.TryGetValue($"{SModManifest.UniqueID}.default_horse", out string defaultHorse) && string.Equals(taxi.Name, defaultHorse, StringComparison.InvariantCultureIgnoreCase) && Game1.player.mount == null)
                    {
                        Game1.player.faceDirection(2);
                        Game1.MusicDuckTimer = 2000f;
                        Game1.playSound("horse_flute");
                        Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[6]
{
                              new (98, 400, true, false),
                              new (99, 200, true, false),
                              new (100, 200, true, false),
                              new (99, 200, true, false),
                              new (98, 400, true, false),
                              new (99, 200, true, false)
});
                        Game1.player.freezePause = 1500;
                        DelayedAction.functionAfterDelay(() =>
                        {
                            GameLocation currentLocation1 = taxi.currentLocation;
                            Vector2 tile1 = taxi.Tile;
                            for (int index = 0; index < 8; ++index)
                                Game1.Multiplayer.broadcastSprites(currentLocation1, new TemporaryAnimatedSprite(10, new Vector2(tile1.X + Utility.RandomFloat(-1f, 1f), tile1.Y + Utility.RandomFloat(-1f, 0.0f)) * 64f, Color.White, animationInterval: 50f)
                                {
                                    layerDepth = 1f,
                                    motion = new Vector2(Utility.RandomFloat(-0.5f, 0.5f), Utility.RandomFloat(-0.5f, 0.5f))
                                });
                            currentLocation1.playSound("wand", new Vector2?(taxi.Tile));
                            GameLocation currentLocation2 = Game1.player.currentLocation;
                            Vector2 tile2 = Game1.player.Tile;
                            currentLocation2.playSound("wand", new Vector2?(tile2));
                            for (int index = 0; index < 8; ++index)
                                Game1.Multiplayer.broadcastSprites(currentLocation2, new TemporaryAnimatedSprite(10, new Vector2(tile2.X + Utility.RandomFloat(-1f, 1f), tile2.Y + Utility.RandomFloat(-1f, 0.0f)) * 64f, Color.White, animationInterval: 50f)
                                {
                                    layerDepth = 1f,
                                    motion = new Vector2(Utility.RandomFloat(-0.5f, 0.5f), Utility.RandomFloat(-0.5f, 0.5f))
                                });
                            Game1.warpCharacter(taxi, Game1.player.currentLocation, tile2);
                            int num = 0;
                            for (int x = (int)tile2.X + 3; x >= (int)tile2.X - 3; --x)
                            {
                                Game1.Multiplayer.broadcastSprites(currentLocation2, new TemporaryAnimatedSprite(6, new Vector2(x, tile2.Y) * 64f, Color.White, animationInterval: 50f)
                                {
                                    layerDepth = 1f,
                                    delayBeforeAnimationStart = num * 25,
                                    motion = new Vector2(-0.25f, 0.0f)
                                });
                                ++num;
                            }
                        }, 1500);
                        if (Config.AutoMountHorse == true)
                            DelayedAction.functionAfterDelay(() => tryMountHorse(Game1.player, taxi, Game1.player.currentLocation), 2000);
                        return true;
                    }
                    else if (name == null && (taxi.ownerId.Value == Game1.player.UniqueMultiplayerID || taxi.ownerId.Value == 0) && !string.IsNullOrEmpty(taxi.Name) && !Game1.player.modData.TryGetValue($"{SModManifest.UniqueID}.default_horse", out string noDefaultHorse) && Game1.player.mount == null)
                    {
                        Game1.player.faceDirection(2);
                        Game1.MusicDuckTimer = 2000f;
                        Game1.playSound("horse_flute");
                        Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[6]
                        {
                              new (98, 400, true, false),
                              new (99, 200, true, false),
                              new (100, 200, true, false),
                              new (99, 200, true, false),
                              new (98, 400, true, false),
                              new (99, 200, true, false)
                        });
                        Game1.player.freezePause = 1500;
                        DelayedAction.functionAfterDelay(() =>
                        {
                            GameLocation currentLocation1 = taxi.currentLocation;
                            Vector2 tile1 = taxi.Tile;
                            for (int index = 0; index < 8; ++index)
                                Game1.Multiplayer.broadcastSprites(currentLocation1, new TemporaryAnimatedSprite(10, new Vector2(tile1.X + Utility.RandomFloat(-1f, 1f), tile1.Y + Utility.RandomFloat(-1f, 0.0f)) * 64f, Color.White, animationInterval: 50f)
                                {
                                    layerDepth = 1f,
                                    motion = new Vector2(Utility.RandomFloat(-0.5f, 0.5f), Utility.RandomFloat(-0.5f, 0.5f))
                                });
                            currentLocation1.playSound("wand", new Vector2?(taxi.Tile));
                            GameLocation currentLocation2 = Game1.player.currentLocation;
                            Vector2 tile2 = Game1.player.Tile;
                            currentLocation2.playSound("wand", new Vector2?(tile2));
                            for (int index = 0; index < 8; ++index)
                                Game1.Multiplayer.broadcastSprites(currentLocation2, new TemporaryAnimatedSprite(10, new Vector2(tile2.X + Utility.RandomFloat(-1f, 1f), tile2.Y + Utility.RandomFloat(-1f, 0.0f)) * 64f, Color.White, animationInterval: 50f)
                                {
                                    layerDepth = 1f,
                                    motion = new Vector2(Utility.RandomFloat(-0.5f, 0.5f), Utility.RandomFloat(-0.5f, 0.5f))
                                });
                            Game1.warpCharacter(taxi, Game1.player.currentLocation, tile2);
                            int num = 0;
                            for (int x = (int)tile2.X + 3; x >= (int)tile2.X - 3; --x)
                            {
                                Game1.Multiplayer.broadcastSprites(currentLocation2, new TemporaryAnimatedSprite(6, new Vector2(x, tile2.Y) * 64f, Color.White, animationInterval: 50f)
                                {
                                    layerDepth = 1f,
                                    delayBeforeAnimationStart = num * 25,
                                    motion = new Vector2(-0.25f, 0.0f)
                                });
                                ++num;
                            }
                        }, 1500);
                        if (Config.AutoMountHorse == true)
                            DelayedAction.functionAfterDelay(() => tryMountHorse(Game1.player, taxi, Game1.player.currentLocation), 2000);
                        return true;
                    }
                    if (Game1.player.mount != null)
                        return true;
                }
            }
            return false;
        }

        internal static bool SetDefaultHorse(string name = null)
        {

            foreach (GameLocation location in Game1.locations)
            {
                List<Horse> taxis = location.characters.OfType<Horse>().ToList();
                taxis.Reverse();
                foreach (Horse taxi in taxis)
                {

                    if (name != null && string.Equals(taxi.Name, name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (taxi.ownerId.Value != Game1.player.UniqueMultiplayerID && taxi.ownerId.Value != 0)
                        {
                            SMonitor.Log(SHelper.Translation.Get("command.error.doesnotbelong", new { mountName = taxi.Name, ownerName = taxi.getOwner().Name }), LogLevel.Error);
                            return true;
                        }

                        Game1.player.modData[$"{SModManifest.UniqueID}.default_horse"] = taxi.Name;
                        SMonitor.Log(SHelper.Translation.Get("command.default_horse.set", new { mountName = taxi.Name }), LogLevel.Info);
                        return true;
                    }
                }
            }

            if (name != null && name.Equals("Remove", StringComparison.InvariantCultureIgnoreCase))
            {
                 Game1.player.modData.TryGetValue($"{SModManifest.UniqueID}.default_horse", out string defaultHorse);
                 Game1.player.modData.Remove($"{SModManifest.UniqueID}.default_horse");
                 SMonitor.Log(SHelper.Translation.Get("command.default_horse.removed", new { defaultMount = defaultHorse}), LogLevel.Info);
                 return true;
            }    
            
            if (Game1.player.mount != null && !string.IsNullOrEmpty(Game1.player.mount.Name) && name == null)
            {
                if (Game1.player.mount.ownerId.Value != Game1.player.UniqueMultiplayerID && Game1.player.mount.ownerId.Value != 0)
                {
                    SMonitor.Log(SHelper.Translation.Get("command.error.doesnotbelong", new { mountName = Game1.player.mount.Name, ownerName = Game1.player.mount.getOwner().Name }), LogLevel.Error);
                    return true;
                }

                Game1.player.modData[$"{SModManifest.UniqueID}.default_horse"] = Game1.player.mount.Name;
                SMonitor.Log(SHelper.Translation.Get("command.default_horse.set", new { mountName = Game1.player.mount.Name }), LogLevel.Info);
                return true;

            }
            return false;
        }


        internal static void CorralHorses(string name = null)
        {
            if (name == null)
            {
                List<Horse> horses = new();
                foreach (GameLocation location in Game1.locations)
                    foreach (Horse horse in location.characters.OfType<Horse>())
                        if (horse.ownerId.Value == Game1.player.UniqueMultiplayerID && !string.IsNullOrEmpty(horse.Name))
                            horses.Add(horse);

                if (horses.Count == 0)
                {
                    Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HorseFlute_NoHorse"));
                    SMonitor.Log(SHelper.Translation.Get("command.horse_whistle.nohorse"), LogLevel.Error);
                    return;
                }

                List<Stable> horsehuts = new();
                foreach (GameLocation location in Game1.locations)
                    foreach (Building building in location.buildings)
                        if (building is Stable stable)
                            foreach (Horse horse in horses)
                                if (stable.HorseId == horse.HorseId && stable.owner.Value == Game1.player.UniqueMultiplayerID)
                                {
                                    horsehuts.Add(building as Stable);
                                    break;
                                }

                foreach (Horse horse in horses)
                    foreach (Stable stable in horsehuts)
                        if (horse.HorseId == stable.HorseId && horse.Tile != new Vector2(stable.tileX.Value + 1, stable.tileY.Value + 1))
                        {
                            DelayedAction.functionAfterDelay(() =>
                            {
                                GameLocation currentLocation1 = horse.currentLocation;
                                Vector2 tile1 = horse.Tile;
                                for (int index = 0; index < 8; ++index)
                                    Game1.Multiplayer.broadcastSprites(currentLocation1, new TemporaryAnimatedSprite(10, new Vector2(tile1.X + Utility.RandomFloat(-1f, 1f), tile1.Y + Utility.RandomFloat(-1f, 0.0f)) * 64f, Color.White, animationInterval: 50f)
                                    {
                                        layerDepth = 1f,
                                        motion = new Vector2(Utility.RandomFloat(-0.5f, 0.5f), Utility.RandomFloat(-0.5f, 0.5f))
                                    });
                                currentLocation1.playSound("wand", new Vector2?(horse.Tile));
                                int num = 0;
                                for (int x = (int)tile1.X + 3; x >= (int)tile1.X - 3; --x)
                                {
                                    Game1.Multiplayer.broadcastSprites(currentLocation1, new TemporaryAnimatedSprite(6, new Vector2(x, tile1.Y) * 64f, Color.White, animationInterval: 50f)
                                    {
                                        layerDepth = 1f,
                                        delayBeforeAnimationStart = num * 25,
                                        motion = new Vector2(-0.25f, 0.0f)
                                    });
                                    ++num;
                                }
                            }, 100);
                            Game1.warpCharacter(horse, "farm", Vector2.Zero);
                            stable?.grabHorse();

                            SMonitor.Log(SHelper.Translation.Get("command.corral_horses.warped"), LogLevel.Info);
                            return;
                        }
            }

            foreach (Farmer farmHand in Game1.getOfflineFarmhands())
                if (Game1.IsMasterGame && string.Equals(name, farmHand.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    List<Horse> horses = new();
                    foreach (GameLocation location in Game1.locations)
                        foreach (Horse horse in location.characters.OfType<Horse>())
                            if (horse.ownerId.Value == farmHand.UniqueMultiplayerID && !string.IsNullOrEmpty(horse.Name))
                                horses.Add(horse);

                    if (horses.Count == 0)
                    {
                        Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HorseFlute_NoHorse"));
                        SMonitor.Log(SHelper.Translation.Get("command.horse_whistle.nohorseoffline"), LogLevel.Error);
                        return;
                    }

                    List<Stable> horsehuts = new();
                    foreach (GameLocation location in Game1.locations)
                        foreach (Building building in location.buildings)
                            if (building is Stable stable)
                                foreach (Horse horse in horses)
                                    if (stable.HorseId == horse.HorseId && stable.owner.Value == farmHand.UniqueMultiplayerID)
                                    {
                                        horsehuts.Add(building as Stable);
                                        break;
                                    }

                    foreach (Horse horse in horses)
                        foreach (Stable stable in horsehuts)
                            if (horse.HorseId == stable.HorseId && horse.Tile != new Vector2(stable.tileX.Value + 1, stable.tileY.Value + 1))
                            {
                                DelayedAction.functionAfterDelay(() =>
                                {
                                    GameLocation currentLocation1 = horse.currentLocation;
                                    Vector2 tile1 = horse.Tile;
                                    for (int index = 0; index < 8; ++index)
                                        Game1.Multiplayer.broadcastSprites(currentLocation1, new TemporaryAnimatedSprite(10, new Vector2(tile1.X + Utility.RandomFloat(-1f, 1f), tile1.Y + Utility.RandomFloat(-1f, 0.0f)) * 64f, Color.White, animationInterval: 50f)
                                        {
                                            layerDepth = 1f,
                                            motion = new Vector2(Utility.RandomFloat(-0.5f, 0.5f), Utility.RandomFloat(-0.5f, 0.5f))
                                        });
                                    currentLocation1.playSound("wand", new Vector2?(horse.Tile));
                                    int num = 0;
                                    for (int x = (int)tile1.X + 3; x >= (int)tile1.X - 3; --x)
                                    {
                                        Game1.Multiplayer.broadcastSprites(currentLocation1, new TemporaryAnimatedSprite(6, new Vector2(x, tile1.Y) * 64f, Color.White, animationInterval: 50f)
                                        {
                                            layerDepth = 1f,
                                            delayBeforeAnimationStart = num * 25,
                                            motion = new Vector2(-0.25f, 0.0f)
                                        });
                                        ++num;
                                    }
                                }, 100);
                                Game1.warpCharacter(horse, "farm", Vector2.Zero);
                                stable?.grabHorse();

                                SMonitor.Log(SHelper.Translation.Get("command.corral_horses.warped"), LogLevel.Info);
                                return;
                            }

                }

            SMonitor.Log(SHelper.Translation.Get("command.corral_horses.error"), LogLevel.Warn);
        }


        private static void tryMountHorse(Farmer who, Horse horse, GameLocation location, int retryCount = 0)
        {
            if (retryCount < 0) 
                return;

            if (who.mount != null)
            {
                return;
            }

            horse.rider = who;
            horse.rider.freezePause = 5000;
            horse.rider.synchronizedJump(6f);
            horse.rider.Halt();
            if (horse.rider.Position.X < horse.Position.X)
                horse.rider.faceDirection(1);
            location.playSound("dwop");
            horse.mounting.Value = true;
            horse.rider.isAnimatingMount = true;
            horse.rider.completelyStopAnimatingOrDoingAction();
            horse.rider.faceGeneralDirection(Utility.PointToVector2(horse.StandingPixel), 0, false, false);


            DelayedAction.functionAfterDelay(() => tryMountHorse(who, horse, location, retryCount - 1), 200);
        }

        internal void OnCommandReceived(string command, string[] args)
        {
            switch (command)
            {

                case "horse_whistle":
                    if (args.ToList().Count == 1)
                    {
                        string callName = args[0];
                        if (CallHorse(callName))
                            return;

                        SMonitor.Log(Helper.Translation.Get("command.horse_whistle.noname", new { mountName = callName }), LogLevel.Error);
                    }
                    else
                    {

                        if (!EnforceArgCount(args, 0))
                            return;


                        if (CallHorse())
                            return;

                        Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HorseFlute_NoHorse"));
                        SMonitor.Log(Helper.Translation.Get("command.horse_whistle.nohorse"), LogLevel.Error);

                    }
                    return;

                case "corral_horses":
                    if (args.ToList().Count >= 1)
                    {
                        string corralName = args[0];

                        CorralHorses(corralName);
                        return;
                    }
                    else
                    {
                        if (!EnforceArgCount(args, 0))
                            return;

                        CorralHorses();
                          return;
                    }

                case "default_horse":
                    if (args.ToList().Count == 1)
                    {
                        string defaultName = args[0];
                        if (SetDefaultHorse(defaultName))
                            return;

                        SMonitor.Log(Helper.Translation.Get("command.default_horse.unable"), LogLevel.Error);
                    }
                    else
                    {

                        if (!EnforceArgCount(args, 0))
                            return;


                        if (SetDefaultHorse())
                            return;

                        SMonitor.Log(Helper.Translation.Get("command.default_horse.unable"), LogLevel.Error);
                    }
                    return;
            }

        }

        internal static bool EnforceArgCount(string[] args, int number)
        {
            if (args.Length == number)
            {
                return true;
            }
            SMonitor.Log(SHelper.Translation.Get("command.error.enforceargcount", new { requiredCount = number, argsCount = args.Length }), LogLevel.Error);
            return false;
        }
    }
}
