

namespace Restauranteer
{
    internal class OrderData
    {
        public string dish;
        public string dishName;
        public string dishDisplayName;
        public int dishPrice;
        public string loved;

        public OrderData(string dish, string dishName, string dishDisplayName, int dishPrice, string loved)
        {
            this.dish = dish;
            this.dishName = dishName;
            this.dishDisplayName = dishDisplayName;
            this.dishPrice = dishPrice;
            this.loved = loved;
        }
    }
}