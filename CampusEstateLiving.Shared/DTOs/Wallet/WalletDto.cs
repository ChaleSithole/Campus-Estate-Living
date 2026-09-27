namespace CampusEstateLiving.Shared.DTOs.Wallet;

public class SavedCardDto
{
    public int CardId { get; set; }
    public string CardholderName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public bool IsDefault { get; set; }
}

public class AddCardRequest
{
    public string CardholderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
}

public class BankAccountDto
{
    public int BankAccountId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumberLast4 { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class AddBankAccountRequest
{
    public string BankName { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Cheque";
}
