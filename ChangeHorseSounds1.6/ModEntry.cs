using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using GenericModConfigMenu;
using HarmonyLib;
using StardewValley;
using Microsoft.Xna.Framework.Audio;
using StardewValley.Extensions;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Linq;
using StardewModdingAPI.Utilities;




namespace ChangeHorseSounds
{

    public sealed class ChangeHorseSoundsModConfig
    {
     public bool ReplaceSounds { get; set; } = true;
     public List<string> SoundsToPlayOnce { get; set; } = new List<string> { };
    }
    public class ModEntry : Mod
    {
        public static IModHelper SHelper { get; private set; }
        public static IManifest SModManifest { get; private set; }
        private static ChangeHorseSoundsModConfig config;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;

            SHelper = helper;
            SModManifest = ModManifest;

            config = SHelper.ReadConfig<ChangeHorseSoundsModConfig>();


            var harmony = new Harmony(ModManifest.UniqueID);

            harmony.Patch(
                              original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.localSound)),
                              prefix: new HarmonyMethod(typeof(SoundPatches), nameof(SoundPatches.localSound_prefix))
                                );

            harmony.Patch(
                              original: AccessTools.Method(typeof(FarmerSprite), "checkForFootstep"),
                              postfix: new HarmonyMethod(typeof(FarmerSpritePatches), nameof(FarmerSpritePatches.checkForFootstep_postfix))
                                );
        }


        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {

                var configMenu = SHelper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                if (configMenu is null)
                    return;

                configMenu.Register(
                   mod: ModManifest,
                   reset: () => config = new ChangeHorseSoundsModConfig(),
                   save: () => SHelper.WriteConfig(config)
                   );

                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("config.ReplaceSounds.name"),
                    tooltip: () => Helper.Translation.Get("config.ReplaceSounds.description"),
                    getValue: () => config.ReplaceSounds,
                    setValue: value => config.ReplaceSounds = value
                    );

                configMenu.AddParagraph(
                    mod: ModManifest,
                    text: () => Helper.Translation.Get("config.PlayOnce.text")
                    );
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
                var thudStep = Directory.GetFiles(Path.Combine(Helper.DirectoryPath, "assets"), "*_thudstep.wav");
                var stoneStep = Directory.GetFiles(Path.Combine(Helper.DirectoryPath, "assets"), "*_stonestep.wav");
                var woodyStep = Directory.GetFiles(Path.Combine(Helper.DirectoryPath, "assets"), "*_woodystep.wav");


            if (config.ReplaceSounds == true)
            {
                for (var i = 0; i < thudStep.Length; i++)
                {
                    CueDefinition namedthudStepCueDefinition = new CueDefinition();

                    namedthudStepCueDefinition.name = $"{ModManifest.UniqueID}_{FileName(thudStep[i])}";

                    SoundEffect audio;
                    string filePathCombined = Path.Combine(Helper.DirectoryPath, "assets", $"{FileName(thudStep[i])}.wav");
                    using (var stream = new FileStream(filePathCombined, FileMode.Open))
                        if (stream != null)
                        {
                            audio = SoundEffect.FromStream(stream, false);

                            namedthudStepCueDefinition.SetSound(audio, Game1.audioEngine.GetCategoryIndex("Sound"), false);

                            Game1.soundBank.AddCue(namedthudStepCueDefinition);
                        }
                }

                for (var i = 0; i < stoneStep.Length; i++)
                {
                    CueDefinition namedstoneStepCueDefinition = new CueDefinition();

                    namedstoneStepCueDefinition.name = $"{ModManifest.UniqueID}_{FileName(stoneStep[i])}";

                    SoundEffect audio;
                    string filePathCombined = Path.Combine(Helper.DirectoryPath, "assets", $"{FileName(stoneStep[i])}.wav");
                    using (var stream = new FileStream(filePathCombined, FileMode.Open))
                        if (stream != null)
                        {
                            audio = SoundEffect.FromStream(stream, false);

                            namedstoneStepCueDefinition.SetSound(audio, Game1.audioEngine.GetCategoryIndex("Sound"), false);

                            Game1.soundBank.AddCue(namedstoneStepCueDefinition);
                        }
                }

                for (var i = 0; i < woodyStep.Length; i++)
                {
                    CueDefinition namedwoodyStepCueDefinition = new CueDefinition();

                    namedwoodyStepCueDefinition.name = $"{ModManifest.UniqueID}_{FileName(woodyStep[i])}";

                    SoundEffect audio;
                    string filePathCombined = Path.Combine(Helper.DirectoryPath, "assets", $"{FileName(woodyStep[i])}.wav");
                    using (var stream = new FileStream(filePathCombined, FileMode.Open))
                        if (stream != null)
                        {
                            audio = SoundEffect.FromStream(stream, false);

                            namedwoodyStepCueDefinition.SetSound(audio, Game1.audioEngine.GetCategoryIndex("Sound"), false);

                            Game1.soundBank.AddCue(namedwoodyStepCueDefinition);
                        }
                }

                if (File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "thudstep.wav")))
                {
                    CueDefinition thudStepCueDefinition = new CueDefinition();

                    thudStepCueDefinition.name = $"{ModManifest.UniqueID}_{FileName(Path.Combine(Helper.DirectoryPath, "assets", "thudstep.wav"))}";

                    SoundEffect audio;
                    string filePathCombined = Path.Combine(Helper.DirectoryPath, "assets", "thudstep.wav");
                    using (var stream = new FileStream(filePathCombined, FileMode.Open))
                        if (stream != null)
                        {
                            audio = SoundEffect.FromStream(stream, false);

                            thudStepCueDefinition.SetSound(audio, Game1.audioEngine.GetCategoryIndex("Sound"), false);

                            Game1.soundBank.AddCue(thudStepCueDefinition);
                        }
                }

                if (File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "stonestep.wav")))
                {
                    CueDefinition stoneStepCueDefinition = new CueDefinition();

                    stoneStepCueDefinition.name = $"{ModManifest.UniqueID}_{FileName(Path.Combine(Helper.DirectoryPath, "assets", "stonestep.wav"))}";

                    SoundEffect audio;
                    string filePathCombined = Path.Combine(Helper.DirectoryPath, "assets", "stonestep.wav");
                    using (var stream = new FileStream(filePathCombined, FileMode.Open))
                        if (stream != null)
                        {
                            audio = SoundEffect.FromStream(stream, false);

                            stoneStepCueDefinition.SetSound(audio, Game1.audioEngine.GetCategoryIndex("Sound"), false);

                            Game1.soundBank.AddCue(stoneStepCueDefinition);
                        }
                }

                if (File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "woodystep.wav")))
                {
                    CueDefinition woodyStepCueDefinition = new CueDefinition();

                    woodyStepCueDefinition.name = $"{ModManifest.UniqueID}_{FileName(Path.Combine(Helper.DirectoryPath, "assets", "woodystep.wav"))}";

                    SoundEffect audio;
                    string filePathCombined = Path.Combine(Helper.DirectoryPath, "assets", "woodystep.wav");
                    using (var stream = new FileStream(filePathCombined, FileMode.Open))
                        if (stream != null)
                        {
                            audio = SoundEffect.FromStream(stream, false);

                            woodyStepCueDefinition.SetSound(audio, Game1.audioEngine.GetCategoryIndex("Sound"), false);

                            Game1.soundBank.AddCue(woodyStepCueDefinition);
                        }
                }


                foreach (string sounds in config.SoundsToPlayOnce)
                    {
                    for (var i = 0; i < thudStep.Length; i++)
                        if (string.Equals(sounds, FileName(thudStep[i]), StringComparison.InvariantCultureIgnoreCase))
                        {
                            Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(thudStep[i])}").instanceLimit = 1;
                            Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(thudStep[i])}").limitBehavior = CueDefinition.LimitBehavior.FailToPlay;
                        }
                    for (var i = 0; i < stoneStep.Length; i++)
                        if (string.Equals(sounds, FileName(stoneStep[i]), StringComparison.InvariantCultureIgnoreCase))
                        {
                            Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(stoneStep[i])}").instanceLimit = 1;
                            Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(stoneStep[i])}").limitBehavior = CueDefinition.LimitBehavior.FailToPlay;
                        }
                    for (var i = 0; i < woodyStep.Length; i++)
                        if (string.Equals(sounds, FileName(woodyStep[i]), StringComparison.InvariantCultureIgnoreCase))
                        {
                            Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(woodyStep[i])}").instanceLimit = 1;
                            Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(woodyStep[i])}").limitBehavior = CueDefinition.LimitBehavior.FailToPlay;
                        }



                    if (File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "thudstep.wav")) && string.Equals(sounds, FileName(Path.Combine(Helper.DirectoryPath, "assets", "thudstep.wav")), StringComparison.InvariantCultureIgnoreCase))
                    {
                        Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(Path.Combine(Helper.DirectoryPath, "assets", "thudstep.wav"))}").instanceLimit = 1;
                        Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(Path.Combine(Helper.DirectoryPath, "assets", "thudstep.wav"))}").limitBehavior = CueDefinition.LimitBehavior.FailToPlay;
                    }
                    if (File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "stonestep.wav")) && string.Equals(sounds, FileName(Path.Combine(Helper.DirectoryPath, "assets", "stonestep.wav")), StringComparison.InvariantCultureIgnoreCase))
                    {
                        Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(Path.Combine(Helper.DirectoryPath, "assets", "stonestep.wav"))}").instanceLimit = 1;
                        Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(Path.Combine(Helper.DirectoryPath, "assets", "stonestep.wav"))}").limitBehavior = CueDefinition.LimitBehavior.FailToPlay;
                    }
                    if (File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "woodystep.wav")) && string.Equals(sounds, FileName(Path.Combine(Helper.DirectoryPath, "assets", "woodystep.wav")), StringComparison.InvariantCultureIgnoreCase))
                    {
                        Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(Path.Combine(Helper.DirectoryPath, "assets", "woodystep.wav"))}").instanceLimit = 1;
                        Game1.soundBank.GetCueDefinition($"{ModManifest.UniqueID}_{FileName(Path.Combine(Helper.DirectoryPath, "assets", "woodystep.wav"))}").limitBehavior = CueDefinition.LimitBehavior.FailToPlay;
                    }
                }
            }
        }

        private static string FileName(string filePath)
        {
            return PathUtilities.GetSegments(filePath).Last().Split(".")[0];
        }

        [HarmonyPatch(typeof(GameLocation), "localSound")]
        public class SoundPatches
        {

            public static void localSound_prefix(GameLocation __instance, ref string audioName, Vector2? position)
            {
                var thudStep = Directory.GetFiles(Path.Combine(SHelper.DirectoryPath, "assets"), "*_thudstep.wav");
                var stoneStep = Directory.GetFiles(Path.Combine(SHelper.DirectoryPath, "assets"), "*_stonestep.wav");
                var woodyStep = Directory.GetFiles(Path.Combine(SHelper.DirectoryPath, "assets"), "*_woodystep.wav");


                foreach (Farmer farmer in __instance.farmers)
                {
                    for (var i = 0; i < thudStep.Length; i++)
                        if (config.ReplaceSounds == true && Game1.soundBank.Exists($"{SModManifest.UniqueID}_{FileName(thudStep[i])}") && audioName.Equals("thudStep", StringComparison.InvariantCultureIgnoreCase) && farmer.mount != null && farmer.mount.rider != null && position != null && farmer.mount.Tile == position && string.Equals(farmer.mount.Name, FileName(thudStep[i]).Split("_")[0], StringComparison.InvariantCultureIgnoreCase))
                        {
                            audioName = $"{SModManifest.UniqueID}_{FileName(thudStep[i])}";
                        }
                    for (var i = 0; i < stoneStep.Length; i++)
                        if (config.ReplaceSounds == true && Game1.soundBank.Exists($"{SModManifest.UniqueID}_{FileName(stoneStep[i])}") && audioName.Equals("stoneStep", StringComparison.InvariantCultureIgnoreCase) && farmer.mount != null && farmer.mount.rider != null && position != null && farmer.mount.Tile == position && string.Equals(farmer.mount.Name, FileName(stoneStep[i]).Split("_")[0], StringComparison.InvariantCultureIgnoreCase))
                        {
                            audioName = $"{SModManifest.UniqueID}_{FileName(stoneStep[i])}";
                        }
                    for (var i = 0; i < woodyStep.Length; i++)
                        if (config.ReplaceSounds == true && Game1.soundBank.Exists($"{SModManifest.UniqueID}_{FileName(woodyStep[i])}") && audioName.Equals("woodyStep", StringComparison.InvariantCultureIgnoreCase) && farmer.mount != null && farmer.mount.rider != null && position != null && farmer.mount.Tile == position && string.Equals(farmer.mount.Name, FileName(woodyStep[i]).Split("_")[0], StringComparison.InvariantCultureIgnoreCase))
                        {
                            audioName = $"{SModManifest.UniqueID}_{FileName(woodyStep[i])}";
                        }
                }

                foreach (Farmer farmer in __instance.farmers)
                {
                    if (config.ReplaceSounds == true && Game1.soundBank.Exists($"{SModManifest.UniqueID}_{FileName(Path.Combine(SHelper.DirectoryPath, "assets", "thudstep.wav"))}") && audioName.Equals("thudStep", StringComparison.InvariantCultureIgnoreCase) && farmer.mount != null && farmer.mount.rider != null && position != null && farmer.mount.Tile == position)
                    {
                        audioName = $"{SModManifest.UniqueID}_{FileName(Path.Combine(SHelper.DirectoryPath, "assets", "thudstep.wav"))}";
                    }
                    if (config.ReplaceSounds == true && Game1.soundBank.Exists($"{SModManifest.UniqueID}_{FileName(Path.Combine(SHelper.DirectoryPath, "assets", "stonestep.wav"))}") && audioName.Equals("stoneStep", StringComparison.InvariantCultureIgnoreCase) && farmer.mount != null && farmer.mount.rider != null && position != null && farmer.mount.Tile == position)
                    {
                        audioName = $"{SModManifest.UniqueID}_{FileName(Path.Combine(SHelper.DirectoryPath, "assets", "stonestep.wav"))}";
                    }
                    if (config.ReplaceSounds == true && Game1.soundBank.Exists($"{SModManifest.UniqueID}_{FileName(Path.Combine(SHelper.DirectoryPath, "assets", "woodystep.wav"))}") && audioName.Equals("woodyStep", StringComparison.InvariantCultureIgnoreCase) && farmer.mount != null && farmer.mount.rider != null && position != null && farmer.mount.Tile == position)
                    {
                        audioName = $"{SModManifest.UniqueID}_{FileName(Path.Combine(SHelper.DirectoryPath, "assets", "woodystep.wav"))}";
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FarmerSprite), "checkForFootstep")]
        public class FarmerSpritePatches
        {

            public static void checkForFootstep_postfix(FarmerSprite __instance)
            {

                if (Context.IsMultiplayer)
                if (__instance.Owner is Farmer farmer && farmer.isRidingHorse() || __instance.Owner == null || __instance.Owner.currentLocation != Game1.currentLocation)
                    return;
                Farmer owner = __instance.Owner as Farmer;
                Vector2 key = owner != null ? owner.Tile : Game1.player.Tile;
                if (Game1.currentLocation.IsOutdoors || Game1.currentLocation.Name.ToLower().Contains("mine") || Game1.currentLocation.Name.ToLower().Contains("cave") || Game1.currentLocation.IsGreenhouse)
                {
                    string str = Game1.currentLocation.doesTileHaveProperty((int)key.X, (int)key.Y, "Type", "Buildings");
                    if (str == null || str.Length < 1)
                        str = Game1.currentLocation.doesTileHaveProperty((int)key.X, (int)key.Y, "Type", "Back");
                    if (str != null)
                    {
                        if (!(str == "Dirt"))
                        {
                            if (!(str == "Stone"))
                            {
                                if (!(str == "Grass"))
                                {
                                    if (str == "Wood")
                                        __instance.currentStep = "woodyStep";
                                }
                                else
                                    __instance.currentStep = Game1.currentLocation.GetSeason() == Season.Winter ? "snowyStep" : "grassyStep";
                            }
                            else
                                __instance.currentStep = "stoneStep";
                        }
                        else
                            __instance.currentStep = "sandyStep";
                    }
                }
                else
                    __instance.currentStep = "thudStep";
                if ((__instance.currentSingleAnimation >= 32 && __instance.currentSingleAnimation <= 56 || __instance.currentSingleAnimation >= 128 && __instance.currentSingleAnimation <= 152) && __instance.currentAnimationIndex % 4 == 0)
                {
                    string cueName = owner.currentLocation.getFootstepSoundReplacement(__instance.currentStep);
                    if (owner.onBridge.Value)
                    {
                        if (owner.currentLocation == Game1.currentLocation && Utility.isOnScreen(owner.Position, 384))
                            cueName = "thudStep";
                        owner.bridge?.OnFootstep(owner.Position);
                    }
                    if (Game1.currentLocation.terrainFeatures.TryGetValue(key, out TerrainFeature terrainFeature) && terrainFeature is Flooring flooring)
                        cueName = flooring.getFootstepSound();
                    Vector2 position = owner.Position;
                    if (owner.shouldShadowBeOffset)
                        position += owner.drawOffset;
                    if (!(cueName == "sandyStep"))
                    {
                        if (cueName == "snowyStep")
                            Game1.currentLocation.temporarySprites.Add(TemporaryAnimatedSprite.GetTemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(247, 407, 6, 6), 2000f, 1, 10000, new Vector2(position.X + 24f + (float)(Game1.random.Next(-4, 4) * 4), position.Y + 8f + (float)(Game1.random.Next(-4, 4) * 4)), false, false, position.Y / 1E+07f, 0.01f, Color.White, (float)(3.0 + Game1.random.NextDouble()), 0.0f, owner.FacingDirection == 1 || owner.FacingDirection == 3 ? -0.7853982f : 0.0f, 0.0f));
                    }
                    else
                    {
                        Game1.currentLocation.temporarySprites.Add(TemporaryAnimatedSprite.GetTemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(128, 2948, 64, 64), 80f, 8, 0, new Vector2(position.X + 16f + (float)Game1.random.Next(-8, 8), position.Y + (float)(Game1.random.Next(-3, -1) * 4)), false, Game1.random.NextBool(), position.Y / 10000f, 0.03f, Color.Khaki * 0.45f, (float)(0.75 + (double)Game1.random.Next(-3, 4) * 0.0500000007450581), 0.0f, 0.0f, 0.0f));
                        TemporaryAnimatedSprite temporaryAnimatedSprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(128, 2948, 64, 64), 80f, 8, 0, new Vector2(position.X + 16f + (float)Game1.random.Next(-4, 4), position.Y + (float)(Game1.random.Next(-3, -1) * 4)), false, Game1.random.NextBool(), position.Y / 10000f, 0.03f, Color.Khaki * 0.45f, (float)(0.550000011920929 + (double)Game1.random.Next(-3, 4) * 0.0500000007450581), 0.0f, 0.0f, 0.0f);
                        temporaryAnimatedSprite.delayBeforeAnimationStart = 20;
                        Game1.currentLocation.temporarySprites.Add(temporaryAnimatedSprite);
                    }
                    if (cueName != null && owner.currentLocation == Game1.currentLocation && Utility.isOnScreen(owner.Position, 384) && (owner == Game1.player || !LocalMultiplayer.IsLocalMultiplayer(true)))
                    {
                        Game1.playSound(cueName);
                        if (owner.boots.Value != null && owner.boots.Value.ItemId == "853")
                            Game1.playSound("jingleBell");
                    }
                    foreach (Trinket trinketItem in owner.trinketItems)
                        trinketItem?.OnFootstep(owner);
                    if (owner.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
                        return;
                    Game1.stats.takeStep();
                }
                else
                {
                    if ((__instance.currentSingleAnimation < 0 || __instance.currentSingleAnimation > 24) && (__instance.currentSingleAnimation < 96 || __instance.currentSingleAnimation > 120))
                        return;
                    if (owner.onBridge.Value && __instance.currentAnimationIndex % 2 == 0)
                    {
                        if (owner.currentLocation == Game1.currentLocation && Utility.isOnScreen(owner.Position, 384) && (owner == Game1.player || !LocalMultiplayer.IsLocalMultiplayer(true)))
                            Game1.playSound("thudStep");
                        owner.bridge?.OnFootstep(owner.Position);
                        foreach (Trinket trinketItem in owner.trinketItems)
                            trinketItem?.OnFootstep(owner);
                    }
                    if (__instance.currentAnimationIndex != 0 || owner.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
                        return;
                    Game1.stats.takeStep();
                }
            }
        }
    }
}
