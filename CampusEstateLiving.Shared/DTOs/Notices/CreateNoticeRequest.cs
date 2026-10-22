using System;
using System.ComponentModel.DataAnnotations;

namespace CampusEstateLiving.Shared.DTOs.Notices;

public class CreateNoticeRequest
{
    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000)]
    public string Content { get; set; } = string.Empty;

    [StringLength(50)]
    public string Category { get; set; } = "General";

    public DateTime? ExpiryDate { get; set; }
}