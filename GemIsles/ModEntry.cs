using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Locations;
using StardewValley.GameData.Machines;
using StardewValley.Locations;
using System.Collections.Generic;
using System.Linq;
using xTile;

namespace GemIsles
{

    public static class MessageId
    {
        public const string AddLocation = nameof(MessageId.AddLocation);
    }

    public class ModEntry : Mod
    {

        public static ModEntry context;

        internal static ModConfig Config;
        private static IMonitor SMonitor;

        private static int mapX;
        private static int mapY;

        public static string mapAssetKey;
        private string locationPrefix;

        private static ModEntry mod;
        private static Multiplayer multiplayer;



        public static void Log(string message, LogLevel level = LogLevel.Trace)
        {
            ModEntry.mod?.Monitor?.Log(message, level);
        }

        public static void SendMessage<T>(T message, string messageType, long playerID)
        {
            ModEntry.mod.Helper.Multiplayer.SendMessage(message, messageType, modIDs: new[] { ModEntry.mod.ModManifest.UniqueID }, playerIDs: new[] { playerID });
        }

        public static void SendMessage(string messageType, long playerID)
        {
            ModEntry.SendMessage(true, messageType, playerID);
        }

        public static IModHelper GetHelper()
        {
            return ModEntry.mod?.Helper;
        }

        public static Multiplayer GetMultiplayer()
        {
            return ModEntry.multiplayer;
        }

        public override void Entry(IModHelper helper)
        {
            context = this;
            ModEntry.mod = this;
            Config = Helper.ReadConfig<ModConfig>();
            SMonitor = Monitor;

            Utils.Initialize(Config, Monitor, Helper);

            locationPrefix = $"{ModManifest.UniqueID}_GemIsles_";

            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.GameLoop.DayEnding += GameLoop_DayEnding;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            helper.Events.Content.AssetRequested += Content_AssetRequested;
            helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
        }


        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs args)
        {
            ModEntry.multiplayer = Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
        }

        private void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID)
                return;

            ModEntry.Log($"[{(Context.IsMainPlayer ? "host" : "farmhand")}] Received {e.Type} from {e.FromPlayerID}.", LogLevel.Trace);

            switch (e.Type)
            {
                case MessageId.AddLocation:
                    if (Context.IsMainPlayer)
                    {
                        string name = e.ReadAs<string>();
                        GameLocation location = new GameLocation(mapAssetKey, name) { IsOutdoors = true, IsFarm = false };
                        Game1.locations.Add(location);
                        Helper.GameContent.InvalidateCache("Data/Locations");
                        Utils.CreateIslesMap(location);
                    }
                    break;
            }
        }

            private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)
        {
            for (int i = Game1.locations.Count - 1; i >= 0; i--)
            {
                if (Game1.locations[i].Name.StartsWith(locationPrefix))
                {
                    Game1.locations[i].characters.Clear();
                    Game1.locations.RemoveAt(i);
                }
            }
        }

        private void Content_AssetRequested(object sender, AssetRequestedEventArgs e)
        {

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Locations"))
            {
                e.Edit(asset =>
                {
                    if (!Config.EnableMod)
                        return;

                    var editor = asset.AsDictionary<string, LocationData>().Data;

                    string name = $"{locationPrefix}{mapX}_{mapY}";

                    foreach (GameLocation location in Game1.locations.ToList())
                        if (editor.TryGetValue("Beach", out var beachData) && !editor.ContainsKey(name) && location.NameOrUniqueName == name && location != null)
                        {
                            editor.Add(name, beachData);
                            editor[name].DisplayName = "Gem Isles";
                        }
                }, AssetEditPriority.Late);
            }       
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            mapAssetKey = Helper.ModContent.GetInternalAssetName("assets/isles.tbin").Name;
        }


        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsPlayerFree || !Game1.player.swimming.Value || (!(Game1.player.currentLocation is Beach) && !(Game1.player.currentLocation is Forest) && !Game1.currentLocation.Name.StartsWith(locationPrefix)))
                return;

            Point pos = Game1.player.TilePoint;
            if (Game1.player.position.Y > Game1.viewport.Y + Game1.viewport.Height - 20)
            {
                Game1.player.position.Value = new Vector2(Game1.player.position.X, Game1.viewport.Y + Game1.viewport.Height - 21);
                Monitor.Log("warping south");
                mapY++;
                if (Game1.player.currentLocation.Name == "Beach")
                {
                    mapY = 1;
                    mapX = 1;
                    pos.X = pos.X * 128 / 104;
                }
                else if (Game1.player.currentLocation.Name == "Forest")
                {
                    mapY = 1;
                    mapX = 0;
                    pos.X = pos.X * 128 / 120;
                }
                WarpToGemIsles(pos.X, 0);
                return;
            }
            if (!Game1.currentLocation.Name.StartsWith(locationPrefix))
                return;
            if (Game1.player.position.Y < Game1.viewport.Y - 12)
            {
                Game1.player.position.Value = new Vector2(Game1.player.position.X, Game1.viewport.Y - 11);
                mapY--;
                Monitor.Log("warping north");
                if (mapY > 0)
                {
                    WarpToGemIsles(pos.X, 71);
                }
                else
                {
                    if (true || mapX > 0)
                    {
                        pos.X = pos.X * 104 / 128;
                        Game1.warpFarmer("Beach", pos.X, Game1.getLocationFromName("Beach").map.DisplaySize.Height / Game1.tileSize - 2, false);
                    }
                    else
                    {
                        pos.X = pos.X * 120 / 128;
                        Game1.warpFarmer("Forest", pos.X, Game1.getLocationFromName("Forest").map.DisplaySize.Height - 2, false);
                    }
                }
            }
            else if (Game1.player.position.X > Game1.viewport.X + Game1.viewport.Width - 36)
            {
                Game1.player.position.Value = new Vector2(Game1.viewport.X + Game1.viewport.Width - 37, Game1.player.position.Y);
                mapX++;
                Monitor.Log("warping east");
                WarpToGemIsles(0, pos.Y);
            }
            else if (Game1.player.position.X < Game1.viewport.X - 28)
            {
                Game1.player.position.Value = new Vector2(Game1.viewport.X - 27, Game1.player.position.Y);
                mapX--;
                Monitor.Log("warping west");
                WarpToGemIsles(127, pos.Y);
            }
        }
        private void WarpToGemIsles(int x, int y)
        {
            if (Game1.eventUp)
                return;
            string name = $"{locationPrefix}{mapX}_{mapY}";
            if (Game1.IsMasterGame && Game1.getLocationFromName(name) == null)
            {
                GameLocation location = new GameLocation(mapAssetKey, name) { IsOutdoors = true, IsFarm = false };
                Game1.locations.Add(location);
                Helper.GameContent.InvalidateCache("Data/Locations");
                Utils.CreateIslesMap(location);
            }
            if (!Game1.IsMasterGame && Game1.getLocationFromName(name) == null)
            {
                ModEntry.SendMessage(name, MessageId.AddLocation, Game1.MasterPlayer.UniqueMultiplayerID);

            }
            Game1.warpFarmer(name, x, y, false);
        }
    }
}
 