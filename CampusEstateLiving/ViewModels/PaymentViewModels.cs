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

public class ReviewCreateViewModel
{
    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [Required, StringLength(800)]
    public string Comment { get; set; } = string.Empty;
}
