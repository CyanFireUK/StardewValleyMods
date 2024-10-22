using Microsoft.Xna.Framework.Graphics;
using System.ComponentModel;

namespace Restauranteer
{
    internal class OrderData
    {
        public string dish;
        public string dishName;
        public string displayName;
        public int dishPrice;
        public string texture;
        public int spriteIndex;
        public string loved;

        public OrderData(string dish, string dishName, string displayName, int dishPrice, string texture, int spriteIndex, string loved)
        {
            this.dish = dish;
            this.dishName = dishName;
            this.displayName = displayName;
            this.dishPrice = dishPrice;
            this.texture = texture;
            this.spriteIndex = spriteIndex;
            this.loved = loved;
        }
    }
}