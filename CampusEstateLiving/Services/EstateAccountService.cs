using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Services;

public class EstateAccountService
{
    private readonly ApplicationDbContext _db;

    public EstateAccountService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<EstateAccount> EnsureForResidentAsync(ApplicationUser user)
    {
        var existing = await _db.EstateAccounts
            .FirstOrDefaultAsync(a => a.UserId == user.Id);

        if (existing != null)
            return existing;

        var account = new EstateAccount
        {
            UserId = user.Id,
            AccountNumber = await NextAccountNumberAsync(),
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.EstateAccounts.Add(account);

        await _db.SaveChangesAsync();

        return account;
    }

    public async Task<string> NextAccountNumberAsync()
    {
        var year = DateTime.UtcNow.Year;

        var last = await _db.EstateAccounts
            .Where(a => a.AccountNumber.StartsWith($"CEL{year}"))
            .OrderByDescending(a => a.AccountNumber)
            .Select(a => a.AccountNumber)
            .FirstOrDefaultAsync();

        var sequence = 1;

        if (last != null &&
            last.Length >= 11 &&
            int.TryParse(last[^4..], out var number))
        {
            sequence = number + 1;
        }

        return $"CEL{year}{sequence:D4}";
    }

    public static string NextPaymentReference()
    {
        var timestamp = DateTime.UtcNow
            .ToString("yyyyMMddHHmmssfff");

        var uniquePart = Guid.NewGuid()
            .ToString("N")[..8]
            .ToUpperInvariant();

        return $"PAY{timestamp}{uniquePart}";
    }
}