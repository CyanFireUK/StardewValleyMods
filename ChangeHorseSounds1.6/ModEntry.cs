using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using GenericModConfigMenu;
using HarmonyLib;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData;
using Microsoft.Xna.Framework.Audio;



namespace ChangeHorseSounds
{

    public sealed class ChangeHorseSoundsModConfig
    {
     public bool Enabled { get; set; } = true;
     public bool PlayOnce { get; set; } = false;

    }
    public class ModEntry : Mod
    {
        public static IModHelper SHelper { get; private set; }
        private static ChangeHorseSoundsModConfig config;


        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.Content.AssetRequested += Content_AssetRequested;

            SHelper = helper;

            config = SHelper.ReadConfig<ChangeHorseSoundsModConfig>();


            var harmony = new Harmony(ModManifest.UniqueID);

            harmony.Patch(
                              original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.localSound)),
                              prefix: new HarmonyMethod(typeof(SoundPatches), nameof(SoundPatches.localSound_prefix))
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
                    name: () => "Enabled",
                    tooltip: () => "Select whether this mod is enabled or not",
                    getValue: () => config.Enabled,
                    setValue: value => config.Enabled = value
                    );

                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => "Play Sounds Once",
                    tooltip: () => "Plays the sound once and then loops when it reaches the end of the sound file",
                    getValue: () => config.PlayOnce,
                    setValue: value => config.PlayOnce = value
                    );

        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (config.PlayOnce == true)
            {
               Game1.soundBank.GetCueDefinition(ModManifest.UniqueID + "_customStone").instanceLimit = 1;
               Game1.soundBank.GetCueDefinition(ModManifest.UniqueID + "_customStone").limitBehavior = CueDefinition.LimitBehavior.FailToPlay;

               Game1.soundBank.GetCueDefinition(ModManifest.UniqueID + "_customWoody").instanceLimit = 1;
               Game1.soundBank.GetCueDefinition(ModManifest.UniqueID + "_customWoody").limitBehavior = CueDefinition.LimitBehavior.FailToPlay;

               Game1.soundBank.GetCueDefinition(ModManifest.UniqueID + "_customThud").instanceLimit = 1;
               Game1.soundBank.GetCueDefinition(ModManifest.UniqueID + "_customThud").limitBehavior = CueDefinition.LimitBehavior.FailToPlay;
            }

        }


        public void Content_AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, AudioCueData> data = editor.AsDictionary<string, AudioCueData>().Data;

                    if (config.Enabled == true)
                    {
                        if (File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "horsecustomstone.wav")))
                        {
                            AddCueData(data, ModManifest.UniqueID + "_customStone", "horsecustomstone.wav", loop: false);
                        }
                        if (File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "horsecustomwoody.wav")))
                        {
                            AddCueData(data, ModManifest.UniqueID + "_customWoody", "horsecustomwoody.wav", loop: false);
                        }
                        if (File.Exists(Path.Combine(Helper.DirectoryPath, "assets", "horsecustomthud.wav")))
                        {
                            AddCueData(data, ModManifest.UniqueID + "_customThud", "horsecustomthud.wav", loop: false);
                        }
                    }
                });
            }
        }

        private void AddCueData(IDictionary<string, AudioCueData> data, string id, string filename, bool loop)
        {
            string path = Path.Combine(Helper.DirectoryPath, "assets", filename);

            data[id] = new AudioCueData
            {
                Id = id,
                FilePaths = new(1) { path },
                Category = "Sound",
                Looped = loop,
                StreamedVorbis = false
            };
        }



        [HarmonyPatch(typeof(GameLocation), "localSound")]
        public class SoundPatches
        {
            public static IEnumerable<Horse> GetHorsesIn(GameLocation location)
            {
                if (!Context.IsMultiplayer)
                {
                    return from h in location.characters.OfType<Horse>()
                           select h;
                }
                return (from h in location.characters.OfType<Horse>()
                        select h).Concat(from player in (IEnumerable<Farmer>)location.farmers
                                         where player.mount != null
                                         select player.mount).Distinct();
            }


            public static void localSound_prefix(GameLocation __instance, ref string audioName, Vector2? position)
            {    
                if (!Context.IsMultiplayer && config.Enabled == true && Game1.soundBank.Exists("CF.ChangeHorseSounds_customStone") && audioName.Equals("stoneStep", StringComparison.InvariantCultureIgnoreCase) && Game1.player.mount.Tile == position && Game1.player.mount.rider != null)
                {
                    audioName = "CF.ChangeHorseSounds_customStone";
                }
                if (!Context.IsMultiplayer && config.Enabled == true && Game1.soundBank.Exists("CF.ChangeHorseSounds_customWoody") && audioName.Equals("woodyStep", StringComparison.InvariantCultureIgnoreCase) && Game1.player.mount.Tile == position && Game1.player.mount.rider != null)
                {
                    audioName = "CF.ChangeHorseSounds_customWoody";
                }
                if (!Context.IsMultiplayer && config.Enabled == true && Game1.soundBank.Exists("CF.ChangeHorseSounds_customThud") && audioName.Equals("thudStep", StringComparison.InvariantCultureIgnoreCase) && Game1.player.mount.Tile == position && Game1.player.mount.rider != null)
                {
                    audioName = "CF.ChangeHorseSounds_customThud";
                }
                foreach (Horse horse1 in GetHorsesIn(__instance))
                {
                    if (Context.IsMultiplayer && config.Enabled == true && Game1.soundBank.Exists("CF.ChangeHorseSounds_customStone") && audioName.Equals("stoneStep", StringComparison.InvariantCultureIgnoreCase) && horse1.Tile == position && horse1.rider != null & !horse1.Name.Equals(""))
                    {
                        audioName = "CF.ChangeHorseSounds_customStone";
                    }
                    if (Context.IsMultiplayer && config.Enabled == true && Game1.soundBank.Exists("CF.ChangeHorseSounds_customWoody") && audioName.Equals("woodyStep", StringComparison.InvariantCultureIgnoreCase) && horse1.Tile == position && horse1.rider != null & !horse1.Name.Equals(""))
                    {
                        audioName = "CF.ChangeHorseSounds_customWoody";
                    }
                    if (Context.IsMultiplayer && config.Enabled == true && Game1.soundBank.Exists("CF.ChangeHorseSounds_customThud") && audioName.Equals("thudStep", StringComparison.InvariantCultureIgnoreCase) && horse1.Tile == position && horse1.rider != null & !horse1.Name.Equals(""))
                    {
                        audioName = "CF.ChangeHorseSounds_customThud";
                    }
                }
            }
        }

    }
}
