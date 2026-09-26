namespace RROMS.Models
{
    public class OrderItem
    {
        public int OrderItemID { get; set; }

        public int OrderID { get; set; }

        public int MenuItemID { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Subtotal { get; set; }

        public CustomerOrder? Order { get; set; }

        public MenuItem? MenuItem { get; set; }
    }
}