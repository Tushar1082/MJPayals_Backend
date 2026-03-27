using DotnetBoilerplate.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetBoilerplate.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Todo> Todos => Set<Todo>();
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<InvoiceItems> InvoiceItems { get; set; } = null!;
    public DbSet<CustomerInvoice> CustomerInvoices { get; set; } = null!;

    public DbSet<DiaryCustomer> DiaryCustomer { get; set; } = null!;
    public DbSet<DiaryItems> DiaryItems { get; set; } = null!;

    public DbSet<HisabDiaryCustomer> HisabDiaryCustomer { get; set; } = null!;
    public DbSet<HisabDiaryTransaction> HisabDiaryTransaction { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
