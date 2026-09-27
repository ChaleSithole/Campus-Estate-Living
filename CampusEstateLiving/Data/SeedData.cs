using CampusEstateLiving.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Data;

public static class SeedData
{
    public const string AdministratorEmail = "admin@ufs.ac.za";
    public const string AdministratorPassword = "CampusAdmin@2026";
    public const string AdministratorNumber = "ADM0001";

    public static async Task InitialiseAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = services.GetRequiredService<RoleManager<ApplicationRole>>();

        await SeedRolesAsync(roles);
        await SeedLookupsAsync(db);
        await EnsureEstateInventoryAsync(db);
        await SeedAdministratorAsync(db, users);
        await SeedWelcomeNoticeAsync(db);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roles)
    {
        var definitions = new (string Name, string Description)[]
        {
            (AppRoles.Administrator, "Agent head. Manages users, officers, reports and estate activity."),
            (AppRoles.FinancialOfficer, "Records, verifies and reports on estate payments."),
            (AppRoles.Student, "Resident student. Pays estate services and views own account."),
            (AppRoles.Staff, "Resident staff member. Pays estate services and views own account.")
        };

        foreach (var (name, description) in definitions)
        {
            if (await roles.RoleExistsAsync(name)) continue;
            var result = await roles.CreateAsync(new ApplicationRole
            {
                Name = name,
                NormalizedName = name.ToUpperInvariant(),
                Description = description
            });
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to seed role {name}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    private static async Task SeedLookupsAsync(ApplicationDbContext db)
    {
        var roomTypes = new[]
        {
            new RoomType { RoomTypeName = RoomTypeNames.Single, Capacity = 1, Description = "Private room for one resident." },
            new RoomType { RoomTypeName = RoomTypeNames.Sharing, Capacity = 2, Description = "Shared room for two residents." }
        };

        foreach (var roomType in roomTypes)
        {
            if (!await db.RoomTypes.AnyAsync(r => r.RoomTypeName == roomType.RoomTypeName))
                db.RoomTypes.Add(roomType);
        }

        var categories = new[]
        {
            new PaymentCategory { CategoryName = PaymentCategoryNames.Rent, Description = "Monthly accommodation rental." },
            new PaymentCategory { CategoryName = PaymentCategoryNames.Water, Description = "Municipal water usage." },
            new PaymentCategory { CategoryName = PaymentCategoryNames.Electricity, Description = "Prepaid electricity units." },
            new PaymentCategory { CategoryName = PaymentCategoryNames.Parking, Description = "Additional vehicle parking." },
            new PaymentCategory { CategoryName = PaymentCategoryNames.RefuseCollection, Description = "Refuse collection levy." }
        };

        foreach (var category in categories)
        {
            if (!await db.PaymentCategories.AnyAsync(c => c.CategoryName == category.CategoryName))
                db.PaymentCategories.Add(category);
        }

        var statuses = new[]
        {
            new PaymentStatus { StatusName = PaymentStatusNames.Pending, Description = "Submitted and awaiting verification." },
            new PaymentStatus { StatusName = PaymentStatusNames.Confirmed, Description = "Verified and accepted." },
            new PaymentStatus { StatusName = PaymentStatusNames.Failed, Description = "Declined or unsuccessful." },
            new PaymentStatus { StatusName = PaymentStatusNames.Cancelled, Description = "Cancelled by the resident or officer." }
        };

        foreach (var status in statuses)
        {
            if (!await db.PaymentStatuses.AnyAsync(s => s.StatusName == status.StatusName))
                db.PaymentStatuses.Add(status);
        }

        await db.SaveChangesAsync();

        var rent = await db.PaymentCategories.SingleAsync(c => c.CategoryName == PaymentCategoryNames.Rent);
        var water = await db.PaymentCategories.SingleAsync(c => c.CategoryName == PaymentCategoryNames.Water);
        var power = await db.PaymentCategories.SingleAsync(c => c.CategoryName == PaymentCategoryNames.Electricity);
        var parking = await db.PaymentCategories.SingleAsync(c => c.CategoryName == PaymentCategoryNames.Parking);
        var refuse = await db.PaymentCategories.SingleAsync(c => c.CategoryName == PaymentCategoryNames.RefuseCollection);

        var tariffs = new[]
        {
            new Tariff { CategoryId = rent.CategoryId, TariffName = "Single room — monthly", Amount = 3850.00m, Unit = "Monthly" },
            new Tariff { CategoryId = rent.CategoryId, TariffName = "Sharing room — monthly", Amount = 2450.00m, Unit = "Monthly" },
            new Tariff { CategoryId = water.CategoryId, TariffName = "Water levy — monthly", Amount = 185.00m, Unit = "Monthly" },
            new Tariff { CategoryId = power.CategoryId, TariffName = "Electricity 50 kWh", Amount = 220.00m, Unit = "50 kWh" },
            new Tariff { CategoryId = power.CategoryId, TariffName = "Electricity 100 kWh", Amount = 410.00m, Unit = "100 kWh" },
            new Tariff { CategoryId = power.CategoryId, TariffName = "Electricity 200 kWh", Amount = 780.00m, Unit = "200 kWh" },
            new Tariff { CategoryId = parking.CategoryId, TariffName = "Additional vehicle — monthly", Amount = 250.00m, Unit = "Monthly" },
            new Tariff { CategoryId = refuse.CategoryId, TariffName = "Refuse collection — monthly", Amount = 95.00m, Unit = "Monthly" }
        };

        foreach (var tariff in tariffs)
        {
            if (!await db.Tariffs.AnyAsync(t => t.CategoryId == tariff.CategoryId && t.TariffName == tariff.TariffName))
                db.Tariffs.Add(tariff);
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureEstateInventoryAsync(ApplicationDbContext db)
    {
        var single = await db.RoomTypes.SingleAsync(t => t.RoomTypeName == RoomTypeNames.Single);
        var sharing = await db.RoomTypes.SingleAsync(t => t.RoomTypeName == RoomTypeNames.Sharing);
        var address = "Campus Estate, University of the Free State, QwaQwa Campus, Phuthaditjhaba";
        var buildings = new[]
        {
            new { Code = "QWA-A", Name = "Block A", Total = 48, Single = 48, Sharing = 0 },
            new { Code = "QWA-B", Name = "Block B", Total = 72, Single = 54, Sharing = 18 },
            new { Code = "QWA-C", Name = "Block C", Total = 72, Single = 54, Sharing = 18 },
            new { Code = "QWA-D", Name = "Block D", Total = 72, Single = 54, Sharing = 18 }
        };

        foreach (var building in buildings)
        {
            if (await db.Properties.AnyAsync(p => p.BuildingCode == building.Code)) continue;
            db.Properties.Add(new Property
            {
                BuildingCode = building.Code,
                BuildingName = building.Name,
                Description = "Campus estate residence building.",
                Address = address,
                PropertyStatus = PropertyStatuses.Active
            });
        }
        await db.SaveChangesAsync();

        foreach (var building in buildings)
        {
            var property = await db.Properties.SingleAsync(p => p.BuildingCode == building.Code);
            property.BuildingName = building.Name;
            property.Address = address;
            property.PropertyStatus = PropertyStatuses.Active;

            var desiredTypes = Enumerable.Repeat(single.RoomTypeId, building.Single)
                .Concat(Enumerable.Repeat(sharing.RoomTypeId, building.Sharing))
                .ToList();
            var existing = await db.Rooms.Include(r => r.Assignments)
                .Where(r => r.PropertyId == property.PropertyId)
                .OrderBy(r => r.RoomId)
                .ToListAsync();
            var assigned = existing.Where(r => r.Assignments.Count > 0).ToList();
            if (assigned.Count > building.Total ||
                assigned.GroupBy(r => r.RoomTypeId).Any(g => g.Count() > desiredTypes.Count(typeId => typeId == g.Key)))
                throw new InvalidOperationException($"{building.Name} has assigned rooms that conflict with the required {building.Total}-room layout. Existing assignments were preserved.");

            foreach (var room in assigned)
                desiredTypes.Remove(room.RoomTypeId);

            var reusable = existing.Where(r => r.Assignments.Count == 0).ToList();
            foreach (var room in reusable.Take(desiredTypes.Count).Select((room, index) => new { room, index }))
            {
                room.room.RoomTypeId = desiredTypes[room.index];
            }

            var retainedReusable = reusable.Take(desiredTypes.Count).ToList();
            var excessRooms = reusable.Skip(desiredTypes.Count).ToList();
            if (excessRooms.Count > 0) db.Rooms.RemoveRange(excessRooms);

            var usedNumbers = assigned.Select(r => r.RoomNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var nextRoomNumber = 1;
            string NextRoomNumber()
            {
                while (usedNumbers.Contains(nextRoomNumber.ToString("D3"))) nextRoomNumber++;
                var roomNumber = nextRoomNumber++.ToString("D3");
                usedNumbers.Add(roomNumber);
                return roomNumber;
            }

            foreach (var room in retainedReusable)
                room.RoomNumber = NextRoomNumber();

            var additions = desiredTypes.Skip(retainedReusable.Count).ToList();
            foreach (var typeId in additions)
            {
                db.Rooms.Add(new Room
                {
                    PropertyId = property.PropertyId,
                    RoomTypeId = typeId,
                    RoomNumber = NextRoomNumber()
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedAdministratorAsync(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        var existing = await users.GetUsersInRoleAsync(AppRoles.Administrator);
        if (existing.Count > 0) return;

        var person = await db.Persons.FirstOrDefaultAsync(p =>
            p.FirstName == "Puleng" && p.LastName == "Mofokeng");
        if (person == null)
        {
            person = new Person
            {
                FirstName = "Puleng",
                LastName = "Mofokeng",
                PhoneNumber = "058 718 5000",
                CreatedAt = DateTime.UtcNow
            };
            db.Persons.Add(person);
            await db.SaveChangesAsync();
        }

        var user = await users.FindByEmailAsync(AdministratorEmail);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = AdministratorEmail,
                Email = AdministratorEmail,
                EmailConfirmed = true,
                PersonId = person.PersonId,
                UserNumber = AdministratorNumber,
                PhoneNumber = person.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };
            var created = await users.CreateAsync(user, AdministratorPassword);
            if (!created.Succeeded)
                throw new InvalidOperationException("Failed to seed administrator: " +
                    string.Join(", ", created.Errors.Select(e => e.Description)));
        }

        if (!await users.IsInRoleAsync(user, AppRoles.Administrator))
            await users.AddToRoleAsync(user, AppRoles.Administrator);
    }

    private static async Task SeedWelcomeNoticeAsync(ApplicationDbContext db)
    {
        if (await db.Announcements.AnyAsync()) return;

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == AdministratorEmail);
        if (admin == null) return;

        db.Announcements.Add(new Announcement
        {
            Title = "Estate office is open",
            Body = "Welcome to Campus Estate Living. Pay rent, water, electricity, parking and refuse from your estate account. After you pay, a financial officer verifies the amount and a receipt is issued. Maintenance requests can be logged from the Maintenance menu. For after-hours emergencies call campus security on 058 718 5111.",
            Audience = AnnouncementAudiences.All,
            IsPinned = true,
            PublishedAt = DateTime.UtcNow,
            CreatedByUserId = admin.Id
        });
        await db.SaveChangesAsync();
    }
}
