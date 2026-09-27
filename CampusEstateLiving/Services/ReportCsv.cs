using System.Text;
using CampusEstateLiving.Models;

namespace CampusEstateLiving.Services;

public static class ReportCsv
{
    public static byte[] Build(IEnumerable<Payment> payments)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Date,Resident,Account,Service,Amount,Method,Status,Reference");

        foreach (var payment in payments)
        {
            builder.AppendLine(string.Join(',',
                Cell(SaTime.Format(payment.PaymentDate)),
                Cell(payment.Account.User.Person.FullName),
                Cell(payment.Account.AccountNumber),
                Cell(payment.Category.CategoryName),
                Cell(payment.Amount.ToString("0.00")),
                Cell(payment.PaymentMethod),
                Cell(payment.PaymentStatus.StatusName),
                Cell(payment.ReferenceNumber)));
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return utf8.GetBytes(builder.ToString());
    }

    private static string Cell(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n'))
            return $"\"{text.Replace("\"", "\"\"")}\"";
        return text;
    }
}
