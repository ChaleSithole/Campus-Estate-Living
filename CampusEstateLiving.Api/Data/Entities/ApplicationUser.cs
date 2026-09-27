using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CampusEstateLiving.Api.Data.Entities;

public class ApplicationUser : IdentityUser<int>
{
    [Required]
    public int PersonId { get; set; }

    [ForeignKey(nameof(PersonId))]
    public Person Person { get; set; } = null!;

    [Required, StringLength(40)]
    public string UserNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public ICollection<ResidentVehicle> Vehicles { get; set; } = new List<ResidentVehicle>();

    public ICollection<ApplicationUserRole> UserRoles { get; set; } =
        new List<ApplicationUserRole>();

    public ICollection<RoomAssignment> RoomAssignments { get; set; } =
        new List<RoomAssignment>();

    public EstateAccount? EstateAccount { get; set; }

    public ICollection<Notification> Notifications { get; set; } =
        new List<Notification>();

    public ICollection<SavedCard> SavedCards { get; set; } =
        new List<SavedCard>();

    public ICollection<BankAccount> BankAccounts { get; set; } =
        new List<BankAccount>();

    public ICollection<Review> Reviews { get; set; } =
        new List<Review>();

    public ICollection<Payment> ProcessedPayments { get; set; } =
        new List<Payment>();
}
