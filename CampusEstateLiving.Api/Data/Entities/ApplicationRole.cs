using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CampusEstateLiving.Api.Data.Entities;

public class ApplicationRole : IdentityRole<int>
{
    [StringLength(250)]
    public string? Description { get; set; }

    public ICollection<ApplicationUserRole> UserRoles { get; set; } =
        new List<ApplicationUserRole>();
}
