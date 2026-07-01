namespace ELearningPlatform.Core;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public decimal Amount { get; set; }
    public CurrencyEnum Currency { get; set; } = CurrencyEnum.EGP;
    public string Status { get; set; } = "Generated";
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string? PdfUrl { get; set; }
}
