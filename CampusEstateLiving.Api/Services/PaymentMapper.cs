using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Shared.DTOs.Payments;

namespace CampusEstateLiving.Api.Services;

public static class PaymentMapper
{
    public static PaymentDto ToDto(Payment payment)
    {
        var person = payment.Account.User.Person;
        return new PaymentDto
        {
            PaymentId = payment.PaymentId,
            AccountId = payment.AccountId,
            AccountNumber = payment.Account.AccountNumber,
            ResidentName = $"{person.FirstName} {person.LastName}",
            CategoryId = payment.CategoryId,
            CategoryName = payment.Category.CategoryName,
            Status = payment.PaymentStatus.StatusName,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod,
            ReferenceNumber = payment.ReferenceNumber,
            Notes = payment.Notes,
            CardLast4 = payment.SavedCard?.Last4,
            ProcessedBy = payment.ProcessedByUser == null
                ? null
                : $"{payment.ProcessedByUser.Person.FirstName} {payment.ProcessedByUser.Person.LastName}",
            ProcessedAt = payment.ProcessedAt
        };
    }
}
