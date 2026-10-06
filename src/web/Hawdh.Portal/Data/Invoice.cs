namespace Hawdh.Portal.Data;

public sealed class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Description { get; set; } = "";
    public string CurrencyCode { get; set; } = "DZD";
    public decimal Amount { get; set; }
    public DateOnly IssuedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly DueOn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
