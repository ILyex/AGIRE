namespace Hawdh.Portal.Data;

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? MeterNumber { get; set; }
    public int? SubscriptionYear { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "";
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
}
