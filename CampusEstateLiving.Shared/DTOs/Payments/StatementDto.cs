namespace CampusEstateLiving.Shared.DTOs.Payments;

public class StatementDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserNumber { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string RoomLabel { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal ConfirmedTotal { get; set; }
    public decimal PendingTotal { get; set; }
    public List<PaymentDto> Payments { get; set; } = [];
}

public class UpdateTariffRequest
{
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdateCategoryRequest
{
    public string? Description { get; set; }
}

public class ChargesDto
{
    public bool CanEdit { get; set; }
    public List<TariffDto> Tariffs { get; set; } = [];
    public List<PaymentCategoryDto> Categories { get; set; } = [];
}
