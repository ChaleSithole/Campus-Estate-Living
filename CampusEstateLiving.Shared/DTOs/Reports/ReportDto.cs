namespace CampusEstateLiving.Shared.DTOs.Reports;

public class ReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal ConfirmedTotal { get; set; }
    public int PaymentCount { get; set; }
    public Dictionary<string, decimal> ByCategory { get; set; } = [];
    public Dictionary<string, int> ByStatus { get; set; } = [];
}
