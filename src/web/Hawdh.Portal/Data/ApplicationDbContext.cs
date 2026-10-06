using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hawdh.Portal.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<WaterTariffSetting> WaterTariffSettings => Set<WaterTariffSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.MeterNumber).HasMaxLength(80);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Location).HasMaxLength(180);
            entity.Property(x => x.Notes).HasMaxLength(2000);
        });

        builder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Description).HasMaxLength(400).IsRequired();
            entity.HasOne(x => x.Customer).WithMany(x => x.Invoices).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Reference).HasMaxLength(80);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasOne(x => x.Invoice).WithMany(x => x.Payments).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MeterReading>(entity =>
        {
            entity.HasIndex(x => new { x.CustomerId, x.Period }).IsUnique();
            entity.Property(x => x.PreviousReading).HasPrecision(18, 3);
            entity.Property(x => x.CurrentReading).HasPrecision(18, 3);
            entity.Property(x => x.Rate).HasPrecision(18, 4);
            entity.HasOne(x => x.Customer).WithMany(x => x.MeterReadings).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WaterTariffSetting>(entity =>
        {
            entity.ToTable("WaterTariffSettings", table => table.HasCheckConstraint("CK_WaterTariffSettings_Singleton", "\"Id\" = 1"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.RatePerCubicMetre).HasPrecision(18, 4);
            entity.Property(x => x.UpdatedBy).HasMaxLength(256).IsRequired();
        });

        builder.Entity<AuditEntry>(entity =>
        {
            entity.Property(x => x.Actor).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(2000);
            entity.HasIndex(x => x.OccurredAtUtc);
        });
    }
}
