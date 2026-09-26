using Microsoft.EntityFrameworkCore;
using RROMS.Models;

namespace RROMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Admin> Admins { get; set; }

        public DbSet<MenuCategory> MenuCategories { get; set; }
        public DbSet<RestaurantTable> RestaurantTables { get; set; }

        public DbSet<Reservation> Reservations { get; set; }

        public DbSet<MenuItem> MenuItems { get; set; }

        public DbSet<CustomerOrder> CustomerOrders { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>()
                .ToTable("Customer");

            modelBuilder.Entity<Staff>()
                .ToTable("Staff");

            modelBuilder.Entity<Admin>()
                .ToTable("Admin");

            modelBuilder.Entity<MenuCategory>()
                .ToTable("MenuCategory");

            modelBuilder.Entity<RestaurantTable>()
                .ToTable("RestaurantTable");

            modelBuilder.Entity<Reservation>()
                .ToTable("Reservation");

            modelBuilder.Entity<MenuItem>()
                .ToTable("MenuItem");

            modelBuilder.Entity<CustomerOrder>()
                .ToTable("CustomerOrder");

            modelBuilder.Entity<OrderItem>()
                .ToTable("OrderItem");

            modelBuilder.Entity<Payment>()
                .ToTable("Payment");

            modelBuilder.Entity<Notification>()
                .ToTable("Notification");
        }
    }
}