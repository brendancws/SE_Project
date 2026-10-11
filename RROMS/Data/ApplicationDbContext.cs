using Microsoft.EntityFrameworkCore;
using RROMS.Models;

namespace RROMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<MenuCategory> MenuCategories { get; set; }
        public DbSet<RestaurantTable> RestaurantTables { get; set; }
        public DbSet<OpeningHour> OpeningHours { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<StaffMember> Staff { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }
    }
}