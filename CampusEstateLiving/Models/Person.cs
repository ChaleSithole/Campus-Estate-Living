using System.ComponentModel.DataAnnotations;

namespace CampusEstateLiving.Models;

public class Person
{
    [Key]
    public int PersonId { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(30)]
    public string? Gender { get; set; }

    public bool HasDisability { get; set; }

    [StringLength(500)]
    public string? DisabilityDetails { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

    public string FullName => $"{FirstName} {LastName}";
}
