using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Customer.Cart
{
    // Protected by the "CustomerOnly" folder convention in Program.cs
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class CartDisplayLine
        {
            public int MenuItemID { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public bool IsAvailable { get; set; }
            public decimal Subtotal => UnitPrice * Quantity;
        }

        public List<CartDisplayLine> Lines { get; private set; } = new();

        public decimal Total => Lines.Sum(l => l.Subtotal);

        public bool HasUnavailable => Lines.Any(l => !l.IsAvailable);

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            await LoadLinesAsync(customerId.Value);
            return Page();
        }

        // Called by the "Add to cart" button on the Menu page
        public async Task<IActionResult> OnPostAddAsync(int menuItemId, int quantity = 1)
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            var item = await _context.MenuItems
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MenuItemID == menuItemId);

            if (item == null)
            {
                return NotFound();
            }

            if (item.Availability != true)
            {
                TempData["CartError"] = $"{item.Name} is currently unavailable and cannot be added.";
                return RedirectToPage("/Customer/Menu/Index");
            }

            CartSession.Add(HttpContext.Session, customerId.Value, menuItemId, quantity);

            TempData["CartMessage"] = $"{item.Name} added to your cart.";
            return RedirectToPage("/Customer/Menu/Index");
        }

        // Quantity of 0 removes the item
        public IActionResult OnPostUpdate(int menuItemId, int quantity)
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            CartSession.SetQuantity(HttpContext.Session, customerId.Value, menuItemId, quantity);
            return RedirectToPage();
        }

        public IActionResult OnPostRemove(int menuItemId)
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            CartSession.Remove(HttpContext.Session, customerId.Value, menuItemId);
            StatusMessage = "Item removed from your cart.";
            return RedirectToPage();
        }

        public IActionResult OnPostClear()
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            CartSession.Clear(HttpContext.Session, customerId.Value);
            StatusMessage = "Your cart has been cleared.";
            return RedirectToPage();
        }

        private async Task LoadLinesAsync(int customerId)
        {
            var cart = CartSession.Get(HttpContext.Session, customerId);
            var ids = cart.Select(l => l.MenuItemID).ToList();

            var items = await _context.MenuItems
                .AsNoTracking()
                .Where(m => ids.Contains(m.MenuItemID))
                .ToListAsync();

            var result = new List<CartDisplayLine>();

            foreach (var line in cart)
            {
                var item = items.FirstOrDefault(m => m.MenuItemID == line.MenuItemID);

                // Skip items that no longer exist in the menu
                if (item == null)
                {
                    continue;
                }

                result.Add(new CartDisplayLine
                {
                    MenuItemID = item.MenuItemID,
                    Name = item.Name,
                    UnitPrice = item.Price,
                    Quantity = line.Quantity,
                    IsAvailable = item.Availability == true
                });
            }

            Lines = result;
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idValue, out int id) && id > 0 ? id : null;
        }
    }
}