namespace CampusEstateLiving.Shared.DTOs.Payments;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string ResidentName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? CardLast4 { get; set; }
    public string? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class CreatePaymentRequest
{
    public int TariffId { get; set; }
    public string PaymentMethod { get; set; } = "EFT";
    public int? SavedCardId { get; set; }
    public string? Notes { get; set; }
}

public class RecordPaymentRequest
{
    public int AccountId { get; set; }
    public int? TariffId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "EFT";
    public string? Notes { get; set; }
}

public class TariffDto
{
    public int TariffId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string TariffName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class PaymentCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
