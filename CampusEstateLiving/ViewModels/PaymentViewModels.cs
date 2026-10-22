using System.ComponentModel.DataAnnotations;
using CampusEstateLiving.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampusEstateLiving.ViewModels;

public class PaymentCreateViewModel
{
    [Required]
    [Display(Name = "Service")]
    public int CategoryId { get; set; }

    [Display(Name = "Tariff")]
    public int? TariffId { get; set; }

    public List<Tariff> AvailableTariffs { get; set; } = [];

    [Required]
    [Range(0.01, 1_000_000)]
    public decimal Amount { get; set; }

    [Required]
    [Display(Name = "Payment method")]
    public string PaymentMethod { get; set; } = PaymentMethods.Card;

    [Display(Name = "Saved card")]
    public int? SavedCardId { get; set; }

    [StringLength(250)]
    public string? Notes { get; set; }

    public List<SelectListItem> Categories { get; set; } = [];
    public List<Tariff> Tariffs { get; set; } = [];
    public List<SavedCard> Cards { get; set; } = [];
    public bool HasCards { get; set; }
    public int VehicleCount { get; set; }
    public string? AccountNumber { get; set; }
    public string? RoomLabel { get; set; }
}

public class PayableServiceViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal StartingAmount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int OptionCount { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? AvailabilityMessage { get; set; }
}

public class PayAllTariffOption
{
    public int TariffId { get; set; }
    public string TariffName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Label => $"{TariffName} — {Amount:C} ({Unit})";
}

public class PayAllServiceViewModel
{
    public int CategoryId { get; set; }
    public int TariffId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string TariffName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsSelected { get; set; } = true;

    public bool AllowsTariffChoice { get; set; }
    public List<PayAllTariffOption> TariffOptions { get; set; } = [];
}

public class PayAllViewModel
{
    public string? AccountNumber { get; set; }
    public string? RoomLabel { get; set; }

    [Required]
    [Display(Name = "Payment method")]
    public string PaymentMethod { get; set; } = PaymentMethods.Card;

    [Display(Name = "Saved card")]
    public int? SavedCardId { get; set; }

    public List<SavedCard> Cards { get; set; } = [];
    public bool HasCards { get; set; }
    public List<PayAllServiceViewModel> Services { get; set; } = [];
    public decimal Total => Services.Where(s => s.IsSelected).Sum(s => s.Amount);
}

public class ReviewCreateViewModel
{
    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [Required, StringLength(800)]
    public string Comment { get; set; } = string.Empty;
}
