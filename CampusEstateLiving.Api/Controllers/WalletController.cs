using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
public class WalletController : ControllerBase
{
    private readonly CampusEstateDbContext _db;

    public WalletController(CampusEstateDbContext db)
    {
        _db = db;
    }

    [HttpGet("cards")]
    public async Task<ActionResult<IEnumerable<SavedCardDto>>> Cards()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var cards = await _db.SavedCards
            .AsNoTracking()
            .Where(c => c.UserId == userId.Value)
            .OrderByDescending(c => c.IsDefault)
            .Select(c => new SavedCardDto
            {
                CardId = c.CardId,
                CardholderName = c.CardholderName,
                Brand = c.Brand,
                Last4 = c.Last4,
                ExpiryMonth = c.ExpiryMonth,
                ExpiryYear = c.ExpiryYear,
                IsDefault = c.IsDefault
            })
            .ToListAsync();

        return Ok(cards);
    }

    [HttpPost("cards")]
    public async Task<ActionResult<SavedCardDto>> AddCard(AddCardRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        if (!CardHelper.LuhnValid(request.CardNumber))
        {
            return BadRequest(new { message = "Enter a valid card number." });
        }

        var hasDefault = await _db.SavedCards.AnyAsync(c =>
            c.UserId == userId.Value && c.IsDefault);

        var card = new SavedCard
        {
            UserId = userId.Value,
            CardholderName = request.CardholderName.Trim(),
            Last4 = CardHelper.Last4(request.CardNumber),
            Brand = CardHelper.DetectBrand(request.CardNumber),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Token = CardHelper.Tokenise(request.CardNumber),
            IsDefault = !hasDefault,
            CreatedAt = DateTime.UtcNow
        };

        _db.SavedCards.Add(card);
        await _db.SaveChangesAsync();

        return Ok(new SavedCardDto
        {
            CardId = card.CardId,
            CardholderName = card.CardholderName,
            Brand = card.Brand,
            Last4 = card.Last4,
            ExpiryMonth = card.ExpiryMonth,
            ExpiryYear = card.ExpiryYear,
            IsDefault = card.IsDefault
        });
    }

    [HttpGet("banks")]
    public async Task<ActionResult<IEnumerable<BankAccountDto>>> Banks()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var banks = await _db.BankAccounts
            .AsNoTracking()
            .Where(b => b.UserId == userId.Value)
            .OrderByDescending(b => b.IsDefault)
            .Select(b => new BankAccountDto
            {
                BankAccountId = b.BankAccountId,
                BankName = b.BankName,
                AccountHolder = b.AccountHolder,
                AccountNumberLast4 = b.AccountNumberLast4,
                BranchCode = b.BranchCode,
                AccountType = b.AccountType,
                IsDefault = b.IsDefault
            })
            .ToListAsync();

        return Ok(banks);
    }

    [HttpPost("banks")]
    public async Task<ActionResult<BankAccountDto>> AddBank(AddBankAccountRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var digits = CardHelper.DigitsOnly(request.AccountNumber);
        if (digits.Length < 6)
        {
            return BadRequest(new { message = "Enter a valid bank account number." });
        }

        var hasDefault = await _db.BankAccounts.AnyAsync(b =>
            b.UserId == userId.Value && b.IsDefault);

        var bank = new BankAccount
        {
            UserId = userId.Value,
            BankName = request.BankName.Trim(),
            AccountHolder = request.AccountHolder.Trim(),
            AccountNumberLast4 = digits.Length >= 4 ? digits[^4..] : digits,
            BranchCode = request.BranchCode.Trim(),
            AccountType = string.IsNullOrWhiteSpace(request.AccountType)
                ? "Cheque"
                : request.AccountType.Trim(),
            IsDefault = !hasDefault,
            CreatedAt = DateTime.UtcNow
        };

        _db.BankAccounts.Add(bank);
        await _db.SaveChangesAsync();

        return Ok(new BankAccountDto
        {
            BankAccountId = bank.BankAccountId,
            BankName = bank.BankName,
            AccountHolder = bank.AccountHolder,
            AccountNumberLast4 = bank.AccountNumberLast4,
            BranchCode = bank.BranchCode,
            AccountType = bank.AccountType,
            IsDefault = bank.IsDefault
        });
    }

    [HttpDelete("cards/{id:int}")]
    public async Task<IActionResult> RemoveCard(int id)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var card = await _db.SavedCards.FirstOrDefaultAsync(c => c.CardId == id && c.UserId == userId.Value);
        if (card == null) return NotFound(new { message = "Card not found." });

        var wasDefault = card.IsDefault;
        _db.SavedCards.Remove(card);
        if (wasDefault)
        {
            var replacement = await _db.SavedCards
                .Where(c => c.UserId == userId.Value && c.CardId != id)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
            if (replacement != null) replacement.IsDefault = true;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Card removed." });
    }

    [HttpDelete("banks/{id:int}")]
    public async Task<IActionResult> RemoveBank(int id)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var bank = await _db.BankAccounts.FirstOrDefaultAsync(b => b.BankAccountId == id && b.UserId == userId.Value);
        if (bank == null) return NotFound(new { message = "Bank account not found." });

        var wasDefault = bank.IsDefault;
        _db.BankAccounts.Remove(bank);
        if (wasDefault)
        {
            var replacement = await _db.BankAccounts
                .Where(b => b.UserId == userId.Value && b.BankAccountId != id)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();
            if (replacement != null) replacement.IsDefault = true;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Bank account removed." });
    }
}
