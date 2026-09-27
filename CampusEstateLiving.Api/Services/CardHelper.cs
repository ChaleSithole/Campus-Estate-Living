using System.Security.Cryptography;
using System.Text;

namespace CampusEstateLiving.Api.Services;

public static class CardHelper
{
    public static string DetectBrand(string number)
    {
        var n = DigitsOnly(number);
        if (n.StartsWith("4")) return "Visa";
        if (n.Length >= 2)
        {
            var two = int.Parse(n[..2]);
            if (two is >= 51 and <= 55) return "Mastercard";
            if (two is 34 or 37) return "American Express";
        }
        if (n.StartsWith("6011") || n.StartsWith("65")) return "Discover";
        return "Card";
    }

    public static string Last4(string number)
    {
        var n = DigitsOnly(number);
        return n.Length >= 4 ? n[^4..] : n.PadLeft(4, '0');
    }

    public static string Tokenise(string number)
    {
        var n = DigitsOnly(number);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("cel-card:" + n));
        return Convert.ToHexString(hash);
    }

    public static bool LuhnValid(string number)
    {
        var n = DigitsOnly(number);
        if (n.Length is < 13 or > 19) return false;
        var sum = 0;
        var alt = false;
        for (var i = n.Length - 1; i >= 0; i--)
        {
            var d = n[i] - '0';
            if (alt)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }
            sum += d;
            alt = !alt;
        }
        return sum % 10 == 0;
    }

    public static string DigitsOnly(string value)
        => new(value.Where(char.IsDigit).ToArray());
}
