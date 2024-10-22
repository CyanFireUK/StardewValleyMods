using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Shops;
using StardewValley.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;
using xTile.ObjectModel;
using xTile.Tiles;
using static StardewValley.Minigames.CraneGame;
using static System.Environment;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Restauranteer
{
    public partial class ModEntry
    {

        [HarmonyPatch(typeof(NPC), nameof(NPC.draw))]
        public class NPC_draw_Patch
        {
            private static int emoteBaseIndex = 424242;

            public static void Prefix(NPC __instance, ref bool __state)
            {
                if (!Config.ModEnabled || !__instance.IsEmoting || __instance.CurrentEmote != emoteBaseIndex)
                    return;
                __state = true;
                __instance.IsEmoting = false;
            }
            public static void Postfix(NPC __instance, SpriteBatch b, float alpha, ref bool __state)
            {
                if (!Config.ModEnabled || !__state)
                    return;
                __instance.IsEmoting = true;
                if (!__instance.modData.TryGetValue(orderKey, out string data))
                    return;
                if (!Config.RestaurantLocations.Contains(__instance.currentLocation.Name))
                {
                    __instance.modData.Remove(orderKey);
                    return;
                }
                OrderData orderData = JsonConvert.DeserializeObject<OrderData>(data);
                int emoteIndex = __instance.CurrentEmoteIndex >= emoteBaseIndex ? __instance.CurrentEmoteIndex - emoteBaseIndex : __instance.CurrentEmoteIndex;
                if (__instance.CurrentEmoteIndex >= emoteBaseIndex + 3)
                {
                    AccessTools.Field(typeof(Character), "currentEmoteFrame").SetValue(__instance, emoteBaseIndex);
                }
                Vector2 emotePosition = __instance.getLocalPosition(Game1.viewport);
                emotePosition.Y -= 32 + __instance.Sprite.SpriteHeight * 4;
                if (SHelper.Input.IsDown(Config.ModKey))
                {
                    Point standingPixel = __instance.StandingPixel;
                    Vector2 local = Game1.GlobalToLocal(new Vector2(standingPixel.X, standingPixel.Y - __instance.Sprite.SpriteHeight * 4 - 64 + __instance.yJumpOffset));
                    Point tile = __instance.TilePoint;
                    if (orderData.texture == null)
                        SpriteText.drawStringWithScrollCenteredAt(b, orderData.dishName, (int)local.X, (int)local.Y, "", 1, null, 1);
                    else
                        SpriteText.drawStringWithScrollCenteredAt(b, orderData.displayName, (int)local.X, (int)local.Y, "", 1, null, 1);
                }
                else
                {
                    b.Draw(emoteSprite, emotePosition, new Rectangle?(new Rectangle(emoteIndex * 16 % Game1.emoteSpriteSheet.Width, emoteIndex * 16 / emoteSprite.Width * 16, 16, 16)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, __instance.getStandingPosition().Y / 10000f);
                    if (orderData.texture == null)
                        b.Draw(Game1.objectSpriteSheet, emotePosition + new Vector2(16, 8), GameLocation.getSourceRectForObject(int.Parse(orderData.dish)), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, (__instance.getStandingPosition().Y + 1) / 10000f);
                    else
                        b.Draw(GetTexture(orderData.texture), emotePosition + new Vector2(16, 8), Game1.getSquareSourceRectForNonStandardTileSheet(GetTexture(orderData.texture), 16, 16, orderData.spriteIndex), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, (__instance.getStandingPosition().Y + 1) / 10000f);

                }

            }
        }
        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.checkAction))]
        public class GameLocation_checkAction_Patch
        {
            public static bool Prefix(GameLocation __instance, Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who, ref bool __result)
            {
                if (!Config.ModEnabled || !Config.RestaurantLocations.Contains(__instance.Name))
                    return true;
                Tile tile = __instance.map.GetLayer("Buildings").PickTile(new Location(tileLocation.X * 64, tileLocation.Y * 64), viewport.Size);
                if (tile != null && tile.Properties.TryGetValue("Action", out PropertyValue property) && property == "DropBox GusFridge")
                {
                    if (__instance.performAction(property, who, tileLocation))
                    {

                        __result = true;
                        return false;
                    }
                    else if (Config.RequireEvent && !Game1.player.eventsSeen.Contains("980558"))
                    {
                        Game1.drawObjectDialogue(SHelper.Translation.Get("low-friendship"));
                        __result = true;
                        return false;
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(GameLocation), "performAction", new Type[] { typeof(string), typeof(Farmer), typeof(Location) })]

        public class GameLocation_performAction_Patch
        {
            public static bool Prefix(GameLocation __instance, string fullActionString, Farmer who, Location tileLocation, ref bool __result)
            {
                if (!Config.ModEnabled || !Config.RestaurantLocations.Contains(__instance.Name) || (fullActionString != "kitchen" && fullActionString != "restaurant"))
                    return true;
                if (Config.RequireEvent && !Game1.player.eventsSeen.Contains("980558"))
                {
                    Game1.drawObjectDialogue(SHelper.Translation.Get("low-friendship"));
                    __result = true;
                    return false;
                }
                var fridge = GetFridge(__instance);

                __instance.ActivateKitchen();
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.UpdateWhenCurrentLocation))]
        public class GameLocation_UpdateWhenCurrentLocation_Patch
        {
            public static void Postfix(GameLocation __instance, GameTime time)
            {
                if (!Config.ModEnabled || !Config.RestaurantLocations.Contains(__instance.Name))
                    return;
                var fridge = GetFridge(__instance);
                fridge.Value.updateWhenCurrentLocation(time);
            }
        }
        
        [HarmonyPatch(typeof(Utility), nameof(Utility.checkForCharacterInteractionAtTile))]
        public class Utility_checkForCharacterInteractionAtTile_Patch
        {
            public static bool Prefix(Vector2 tileLocation, Farmer who)
            {
                if (!Config.ModEnabled)
                    return true;
                NPC npc = Game1.currentLocation.isCharacterAtTile(tileLocation);
                if (npc is null || !npc.modData.TryGetValue(orderKey, out string data))
                    return true;
                if (!Config.RestaurantLocations.Contains(Game1.currentLocation.Name))
                {
                    npc.modData.Remove(orderKey);
                    return true;
                }
                OrderData orderData = JsonConvert.DeserializeObject<OrderData>(data);
                if(who.ActiveObject != null && who.ActiveObject.canBeGivenAsGift() && who.ActiveObject.Name == orderData.dishName)
                {
                    Game1.mouseCursor = 6;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.tryToReceiveActiveObject))]
        public class NPC_tryToReceiveActiveObject_Patch
        {
            public static bool Prefix(NPC __instance, Farmer who)
            {
                if (!Config.ModEnabled || !Config.RestaurantLocations.Contains(__instance.currentLocation.Name) || !__instance.modData.TryGetValue(orderKey, out string data))
                    return true;
                OrderData orderData = JsonConvert.DeserializeObject<OrderData>(data);
                if(who.ActiveObject?.ParentSheetIndex == int.Parse(orderData.dish))
                {
                    SMonitor.Log($"Fulfilling {__instance.Name}'s order of {orderData.dishName}");
                    if(!npcOrderNumbers.Value.ContainsKey(__instance.Name))
                    {
                        npcOrderNumbers.Value[__instance.Name] = 1;
                    }
                    else
                    {
                        npcOrderNumbers.Value[__instance.Name]++;
                    }
                    List<string> possibleReactions = new();
                    int count = 0;
                    string prefix = "RestauranteerMod-";
                    var dict = SHelper.GameContent.Load<Dictionary<string, string>>($"Characters/Dialogue/{__instance.Name}");
                    if (orderData.loved == "true")
                    {
                        if (dict is not null && dict.TryGetValue($"{prefix}Loved-{++count}", out string r))
                        {
                            possibleReactions.Add(r);
                            while(dict.TryGetValue($"{prefix}Loved-{++count}", out r))
                            {
                                possibleReactions.Add(r);
                            }
                        }
                        else
                        {
                            possibleReactions.Add(SHelper.Translation.Get("loved-order-reaction-1"));
                            possibleReactions.Add(SHelper.Translation.Get("loved-order-reaction-2"));
                            possibleReactions.Add(SHelper.Translation.Get("loved-order-reaction-3"));
                        }
                    }
                    else
                    {
                        if (dict is not null && dict.TryGetValue($"{prefix}Liked-{++count}", out string r))
                        {
                            possibleReactions.Add(r);
                            while(dict.TryGetValue($"{prefix}Liked-{++count}", out r))
                            {
                                possibleReactions.Add(r);
                            }
                        }
                        else
                        {
                            possibleReactions.Add(SHelper.Translation.Get("liked-order-reaction-1"));
                            possibleReactions.Add(SHelper.Translation.Get("liked-order-reaction-2"));
                            possibleReactions.Add(SHelper.Translation.Get("liked-order-reaction-3"));
                        }
                    }
                    string reaction = possibleReactions[Game1.random.Next(possibleReactions.Count)];

                    switch (who.FacingDirection)
                    {
                        case 0:
                            ((FarmerSprite)who.Sprite).animateBackwardsOnce(80, 50f);
                            break;
                        case 1:
                            ((FarmerSprite)who.Sprite).animateBackwardsOnce(72, 50f);
                            break;
                        case 2:
                            ((FarmerSprite)who.Sprite).animateBackwardsOnce(64, 50f);
                            break;
                        case 3:
                            ((FarmerSprite)who.Sprite).animateBackwardsOnce(88, 50f);
                            break;
                    }
                    int friendshipAmount = orderData.loved == "true" ? Config.LovedFriendshipChange : Config.LikedFriendshipChange;
                    who.changeFriendship(friendshipAmount, __instance);
                    SMonitor.Log($"Changed friendship with {__instance.Name} by {friendshipAmount}");
                    if (Config.RevealGiftTaste)
                    {
                        who.revealGiftTaste(__instance.Name, orderData.dish);
                    }
                    if(Config.PriceMarkup > 0)
                    {
                        int price = (int)Math.Round(who.ActiveObject.Price * Config.PriceMarkup);
                        who.Money += price;
                        SMonitor.Log($"Received {price} coins for order");
                    }
                    who.reduceActiveItemByOne();
                    who.completelyStopAnimatingOrDoingAction();
                    __instance.CurrentDialogue.Push(new Dialogue(__instance, null, reaction + "$h"));
                    Game1.drawDialogue(__instance);
                    __instance.faceTowardFarmerForPeriod(2000, 3, false, who);
                    __instance.modData.Remove(orderKey);
                    return false;
                }
                return true;   
            }
        }
    }
}