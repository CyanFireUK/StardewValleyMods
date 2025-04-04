﻿using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System.Collections.Generic;

namespace Restauranteer
{
    public class ModConfig
    {
        public bool ModEnabled { get; set; } = true;
        public bool RequireEvent { get; set; } = true;
        public bool AutoFillFridge { get; set; } = true;
        public bool PatchSaloonMap { get; set; } = true;
        public bool AddFridgeObjects { get; set; } = false;
        public bool RevealGiftTaste { get; set; } = true;
        public bool SellCurrentRecipes { get; set; } = true;
        public SButton ModKey { get; set; } = SButton.LeftAlt;
        public KeybindList IngredientKey { get; set; } = KeybindList.Parse("LeftControl + I");
        public int MaxNPCOrdersPerNight { get; set; } = 2;
        public int LovedFriendshipChange { get; set; } = 40;
        public int LikedFriendshipChange { get; set; } = 20;
        public float PriceMarkup { get; set; } = 1f;
        public float OrderChance { get; set; } = 0.05f;
        public float LovedDishChance { get; set; } = 0.7f;
        public string EventKey { get; set; } = "980558/t 600 1130/w sunny/f Gus 1250";
        public string EventReplacePart { get; set; } = "/pause 500/end";
        public string EventReplaceWith { get; set; } = "/pause 200/speak Gus \"{0}\"/pause 500/end";
        public string EmilySaloonString { get; set; } = "Saloon 15 17 0 square_3_1_0";
        public List<string> RestaurantLocations { get; set; } = new List<string>()
        {
            "Saloon"
        };
        public List<string> IgnoredNPCs { get; set; } = new List<string>()
        {
            "Gus",
            "Emily"
        };
        public List<Point> KitchenTiles { get; set; } = new List<Point>()
        {
        };
    }
}
