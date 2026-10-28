using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace RROMS.Pages.Customer.Cart
{
    public class CartLine
    {
        public int MenuItemID { get; set; }
        public int Quantity { get; set; }
    }

    // Keeps one cart per logged-in customer in the session.
    // Only IDs and quantities are stored. Prices are always read from the database.
    public static class CartSession
    {
        public const int MaxQuantityPerItem = 20;

        private static string KeyFor(int customerId) => $"Dineflow.Cart.{customerId}";

        public static List<CartLine> Get(ISession session, int customerId)
        {
            string? json = session.GetString(KeyFor(customerId));

            if (string.IsNullOrEmpty(json))
            {
                return new List<CartLine>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CartLine>>(json) ?? new List<CartLine>();
            }
            catch (JsonException)
            {
                return new List<CartLine>();
            }
        }

        public static void Save(ISession session, int customerId, List<CartLine> cart)
        {
            session.SetString(KeyFor(customerId), JsonSerializer.Serialize(cart));
        }

        public static void Clear(ISession session, int customerId)
        {
            session.Remove(KeyFor(customerId));
        }

        public static void Add(ISession session, int customerId, int menuItemId, int quantity)
        {
            var cart = Get(session, customerId);
            var line = cart.FirstOrDefault(l => l.MenuItemID == menuItemId);

            if (line == null)
            {
                cart.Add(new CartLine
                {
                    MenuItemID = menuItemId,
                    Quantity = Math.Clamp(quantity, 1, MaxQuantityPerItem)
                });
            }
            else
            {
                line.Quantity = Math.Clamp(line.Quantity + quantity, 1, MaxQuantityPerItem);
            }

            Save(session, customerId, cart);
        }

        // Quantity of 0 or less removes the item
        public static void SetQuantity(ISession session, int customerId, int menuItemId, int quantity)
        {
            var cart = Get(session, customerId);
            var line = cart.FirstOrDefault(l => l.MenuItemID == menuItemId);

            if (line == null)
            {
                return;
            }

            if (quantity <= 0)
            {
                cart.Remove(line);
            }
            else
            {
                line.Quantity = Math.Clamp(quantity, 1, MaxQuantityPerItem);
            }

            Save(session, customerId, cart);
        }

        public static void Remove(ISession session, int customerId, int menuItemId)
        {
            var cart = Get(session, customerId);
            cart.RemoveAll(l => l.MenuItemID == menuItemId);
            Save(session, customerId, cart);
        }
    }
}