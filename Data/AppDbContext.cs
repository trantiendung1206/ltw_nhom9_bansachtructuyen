using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using sc.Models;

namespace sc.Data;

public sealed class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public string Address { get; set; } = "";
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<CustomerFeedback> CustomerFeedbacks => Set<CustomerFeedback>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Book>().Property(book => book.Price).HasPrecision(18, 2);
        builder.Entity<Book>().Property(book => book.DiscountAmount).HasPrecision(18, 2);
        builder.Entity<Order>().Property(order => order.Total).HasPrecision(18, 2);
        builder.Entity<OrderItem>().Property(item => item.UnitPrice).HasPrecision(18, 2);
    }
}
