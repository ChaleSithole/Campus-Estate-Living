using System.ComponentModel.DataAnnotations;
using CampusEstateLiving.Models;

namespace CampusEstateLiving.ViewModels;

public class CreateUserViewModel
{
    [Required, StringLength(80)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = AppRoles.Student;

    [Required, StringLength(40)]
    [Display(Name = "User number")]
    public string UserNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(30)]
    public string? Gender { get; set; }

    public bool HasDisability { get; set; }

    [StringLength(500)]
    public string? DisabilityDetails { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Link to existing person")]
    public int? ExistingPersonId { get; set; }

    [Display(Name = "Create estate account (residents only)")]
    public bool CreateEstateAccount { get; set; } = true;
}

public class EditUserViewModel
{
    public int UserId { get; set; }
    public int PersonId { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string UserNumber { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Display(Name = "Estate account status")]
    public string? AccountStatus { get; set; }

    public string Role { get; set; } = string.Empty;
}

public class AssignRoomViewModel
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [Display(Name = "Room")]
    public int RoomId { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Move-in date")]
    public DateTime MoveInDate { get; set; } = DateTime.UtcNow.Date;

    public string? ResidentName { get; set; }
}

public class PropertyFormViewModel
{
    public int? PropertyId { get; set; }

    [Required, StringLength(40)]
    [Display(Name = "Building code")]
    public string BuildingCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Building name")]
    public string BuildingName { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    [Required, StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string PropertyStatus { get; set; } = PropertyStatuses.Active;
}

public class RoomFormViewModel
{
    public int? RoomId { get; set; }

    [Required]
    [Display(Name = "Property")]
    public int PropertyId { get; set; }

    [Required]
    [Display(Name = "Room type")]
    public int RoomTypeId { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Room number")]
    public string RoomNumber { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class VehicleViewModel
{
    [Required, StringLength(12, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9 -]+$", ErrorMessage = "Enter a valid vehicle registration.")]
    [Display(Name = "Registration number")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [StringLength(60)]
    [Display(Name = "Make and model")]
    public string? MakeAndModel { get; set; }
}
