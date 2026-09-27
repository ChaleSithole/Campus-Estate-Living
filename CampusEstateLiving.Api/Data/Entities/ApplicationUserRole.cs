using Microsoft.AspNetCore.Identity;

namespace CampusEstateLiving.Api.Data.Entities;

public class ApplicationUserRole : IdentityUserRole<int>
{
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;

    public ApplicationRole Role { get; set; } = null!;
}
