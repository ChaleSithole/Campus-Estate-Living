using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using CampusEstateLiving.Services;
using CampusEstateLiving.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly OccupancyService _occupancy;
    private readonly EstateAccountService _accounts;
    private readonly NotificationService _notify;

    public AdminController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        OccupancyService occupancy,
        EstateAccountService accounts,
        NotificationService notify)
    {
        _db = db;
        _users = users;
        _occupancy = occupancy;
        _accounts = accounts;
        _notify = notify;
    }

    public IActionResult Index() => RedirectToAction("Dashboard", "Home");

    public async Task<IActionResult> Users(string? role = null, string? q = null)
    {
        var query = _db.Users
            .Include(u => u.Person)
            .Include(u => u.EstateAccount)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.UserRoles.Any(r => r.Role.Name == role));
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(u =>
                u.Email!.Contains(q) ||
                u.UserNumber.Contains(q) ||
                u.Person.FirstName.Contains(q) ||
                u.Person.LastName.Contains(q));
        }

        ViewBag.Role = role;
        ViewBag.Q = q;
        return View("Users", await query.OrderBy(u => u.Person.LastName).ThenBy(u => u.Person.FirstName).ToListAsync());
    }

    public Task<IActionResult> Students(string? q = null) => Users(AppRoles.Student, q);
    public Task<IActionResult> Staff(string? q = null) => Users(AppRoles.Staff, q);
    public IActionResult Officers(string? q = null) => RedirectToAction(nameof(Management));

    public async Task<IActionResult> Management()
    {
        ViewBag.Tariffs = await _db.Tariffs.Include(t => t.Category).OrderBy(t => t.Category.CategoryName).ThenBy(t => t.TariffName).ToListAsync();
        ViewBag.RoomTypes = await _db.RoomTypes.OrderBy(t => t.RoomTypeName).ToListAsync();
        ViewBag.Properties = await _db.Properties.OrderBy(p => p.BuildingName).ToListAsync();
        ViewBag.Services = await _db.PaymentCategories.OrderBy(c => c.CategoryName).ToListAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Tariffs()
    {
        ViewBag.Categories = new SelectList(await _db.PaymentCategories.OrderBy(c => c.CategoryName).ToListAsync(), "CategoryId", "CategoryName");
        ViewBag.Tariffs = await _db.Tariffs.Include(t => t.Category)
            .OrderBy(t => t.Category.CategoryName).ThenBy(t => t.TariffName).ToListAsync();
        return View(new Tariff());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tariffs(Tariff model)
    {
        if (!await _db.PaymentCategories.AnyAsync(c => c.CategoryId == model.CategoryId))
            ModelState.AddModelError(nameof(model.CategoryId), "Choose a valid service category.");
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(await _db.PaymentCategories.OrderBy(c => c.CategoryName).ToListAsync(), "CategoryId", "CategoryName", model.CategoryId);
            ViewBag.Tariffs = await _db.Tariffs.Include(t => t.Category).OrderBy(t => t.Category.CategoryName).ThenBy(t => t.TariffName).ToListAsync();
            return View(model);
        }

        _db.Tariffs.Add(new Tariff
        {
            CategoryId = model.CategoryId,
            TariffName = model.TariffName.Trim(),
            Amount = model.Amount,
            Unit = model.Unit.Trim(),
            IsActive = model.IsActive
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Tariff added.";
        return RedirectToAction(nameof(Tariffs));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTariff(int id, decimal amount, string unit, bool isActive)
    {
        var tariff = await _db.Tariffs.FindAsync(id);
        if (tariff == null) return NotFound();
        if (amount is < 0.01m or > 1_000_000m || string.IsNullOrWhiteSpace(unit) || unit.Length > 40)
        {
            TempData["Error"] = "Enter a valid amount and billing unit.";
            return RedirectToAction(nameof(Tariffs));
        }
        tariff.Amount = amount;
        tariff.Unit = unit.Trim();
        tariff.IsActive = isActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Tariff updated.";
        return RedirectToAction(nameof(Tariffs));
    }

    private static string UserNumberLabel(string role) => role switch
    {
        AppRoles.Student => "Student Number",
        AppRoles.Staff => "Staff Number",
        AppRoles.FinancialOfficer => "Officer Number",
        AppRoles.Administrator => "Admin Number",
        _ => "Identification Number"
    };

    public async Task<IActionResult> UserDetails(int id)
    {
        var user = await LoadUserGraph(id);
        if (user == null) return NotFound();
        ViewBag.Roles = await _users.GetRolesAsync(user);
        ViewBag.Linked = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.PersonId == user.PersonId && u.Id != user.Id)
            .ToListAsync();
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> CreateUser(string? role = null)
    {
        ViewBag.Persons = await PersonOptions();
        return View(new CreateUserViewModel
        {
            Role = role is not null && AppRoles.All.Contains(role) ? role : AppRoles.Student,
            CreateEstateAccount = role is null or AppRoles.Student or AppRoles.Staff
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        ViewBag.Persons = await PersonOptions();
        if (model.ExistingPersonId.HasValue)
        {
            var existing = await _db.Persons.FindAsync(model.ExistingPersonId.Value);
            if (existing != null && (model.DateOfBirth.HasValue || !string.IsNullOrWhiteSpace(model.Gender) || model.HasDisability || !string.IsNullOrWhiteSpace(model.DisabilityDetails)))
                ModelState.AddModelError(string.Empty, "Resident profile fields belong to the linked person. Edit that person's profile rather than duplicating their details on this account.");
        }
        if (!AppRoles.All.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Invalid role.");
        var expectedDigits = model.Role switch
        {
            AppRoles.Student => 10,
            AppRoles.Staff or AppRoles.FinancialOfficer or AppRoles.Administrator => 7,
            _ => 0
        };
        if (expectedDigits == 0 || model.UserNumber.Length != expectedDigits || model.UserNumber.Any(c => !char.IsAsciiDigit(c)))
            ModelState.AddModelError(nameof(model.UserNumber), $"{UserNumberLabel(model.Role)} must contain exactly {expectedDigits} digits.");
        if (!ModelState.IsValid) return View(model);

        if (await _users.FindByEmailAsync(model.Email) != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email already in use.");
            return View(model);
        }

        Person person;
        if (model.ExistingPersonId.HasValue)
        {
            person = await _db.Persons.FindAsync(model.ExistingPersonId.Value)
                ?? throw new InvalidOperationException("Person not found.");
        }
        else
        {
            person = new Person
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                HasDisability = model.HasDisability,
                DisabilityDetails = model.HasDisability ? model.DisabilityDetails?.Trim() : null,
                CreatedAt = DateTime.UtcNow
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            PersonId = person.PersonId,
            UserNumber = model.UserNumber.Trim(),
            PhoneNumber = model.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
        var created = await _users.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var e in created.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        await _users.AddToRoleAsync(user, model.Role);
        if (model.CreateEstateAccount && AppRoles.Resident.Contains(model.Role))
            await _accounts.EnsureForResidentAsync(user);

        await _notify.NotifyAsync(user.Id, "Account created",
            $"An administrator created your {model.Role} account.", "Account");

        TempData["Success"] = $"{person.FullName} was added as {model.Role}.";
        return RedirectToAction(nameof(UserDetails), new { id = user.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        var user = await LoadUserGraph(id);
        if (user == null) return NotFound();
        var roles = await _users.GetRolesAsync(user);
        return View(new EditUserViewModel
        {
            UserId = user.Id,
            PersonId = user.PersonId,
            FirstName = user.Person.FirstName,
            LastName = user.Person.LastName,
            UserNumber = user.UserNumber,
            PhoneNumber = user.Person.PhoneNumber,
            Email = user.Email ?? "",
            IsActive = user.IsActive,
            AccountStatus = user.EstateAccount?.AccountStatus,
            Role = roles.FirstOrDefault() ?? ""
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await LoadUserGraph(model.UserId);
        if (user == null) return NotFound();

        user.Person.FirstName = model.FirstName.Trim();
        user.Person.LastName = model.LastName.Trim();
        user.Person.PhoneNumber = model.PhoneNumber;
        user.UserNumber = model.UserNumber.Trim();
        user.PhoneNumber = model.PhoneNumber;
        user.IsActive = model.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        if (user.EstateAccount != null && !string.IsNullOrWhiteSpace(model.AccountStatus))
        {
            user.EstateAccount.AccountStatus = model.AccountStatus;
            user.EstateAccount.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "User updated.";
        return RedirectToAction(nameof(UserDetails), new { id = user.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var user = await _db.Users.Include(u => u.EstateAccount).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        if (user.EstateAccount != null)
            user.EstateAccount.AccountStatus = AccountStatuses.Suspended;
        await _db.SaveChangesAsync();
        TempData["Success"] = "User deactivated.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> Properties()
        => View(await _db.Properties.Include(p => p.Rooms).OrderBy(p => p.BuildingName).ToListAsync());

    [HttpGet]
    public IActionResult CreateProperty() => View("PropertyForm", new PropertyFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProperty(PropertyFormViewModel model)
    {
        if (await _db.Properties.CountAsync() >= 4)
            ModelState.AddModelError(string.Empty, "The estate is configured for four buildings.");
        if (!ModelState.IsValid) return View("PropertyForm", model);
        _db.Properties.Add(new Property
        {
            BuildingCode = model.BuildingCode.Trim(),
            BuildingName = model.BuildingName.Trim(),
            Description = model.Description,
            Address = model.Address.Trim(),
            PropertyStatus = model.PropertyStatus,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Property added.";
        return RedirectToAction(nameof(Properties));
    }

    [HttpGet]
    public async Task<IActionResult> Rooms()
    {
        var rooms = await _db.Rooms
            .Include(r => r.Property)
            .Include(r => r.RoomType)
            .Include(r => r.Assignments)
            .OrderBy(r => r.Property.BuildingName)
            .ThenBy(r => r.RoomNumber)
            .ToListAsync();
        return View(rooms);
    }

    [HttpGet]
    public async Task<IActionResult> CreateRoom()
    {
        await FillRoomLookups();
        return View("RoomForm", new RoomFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom(RoomFormViewModel model)
    {
        await FillRoomLookups();
        var property = await _db.Properties.FirstOrDefaultAsync(p => p.PropertyId == model.PropertyId);
        var buildingLimits = property?.BuildingCode switch
        {
            "QWA-A" => (Total: 48, Single: 48, Sharing: 0),
            "QWA-B" or "QWA-C" or "QWA-D" => (Total: 72, Single: 54, Sharing: 18),
            _ => (Total: 0, Single: 0, Sharing: 0)
        };
        if (property == null || buildingLimits.Total == 0)
            ModelState.AddModelError(nameof(model.PropertyId), "Choose one of the four configured estate buildings.");
        else
        {
            var selectedRoomType = await _db.RoomTypes.FirstOrDefaultAsync(t => t.RoomTypeId == model.RoomTypeId);
            var existingRooms = await _db.Rooms.Where(r => r.PropertyId == model.PropertyId).ToListAsync();
            var singleTypeId = await _db.RoomTypes.Where(t => t.RoomTypeName == RoomTypeNames.Single).Select(t => t.RoomTypeId).FirstOrDefaultAsync();
            var sharingTypeId = await _db.RoomTypes.Where(t => t.RoomTypeName == RoomTypeNames.Sharing).Select(t => t.RoomTypeId).FirstOrDefaultAsync();
            var singleCount = existingRooms.Count(r => r.RoomTypeId == singleTypeId);
            var sharingCount = existingRooms.Count(r => r.RoomTypeId == sharingTypeId);
            if (existingRooms.Count >= buildingLimits.Total)
                ModelState.AddModelError(string.Empty, $"{property.BuildingName} already has its full {buildingLimits.Total}-room allocation.");
            else if (selectedRoomType == null)
                ModelState.AddModelError(nameof(model.RoomTypeId), "Choose a valid room type.");
            else if (selectedRoomType.RoomTypeName is not (RoomTypeNames.Single or RoomTypeNames.Sharing))
                ModelState.AddModelError(nameof(model.RoomTypeId), "Choose Single or Sharing room type.");
            else if (selectedRoomType.RoomTypeName == RoomTypeNames.Single && singleCount >= buildingLimits.Single)
                ModelState.AddModelError(nameof(model.RoomTypeId), "This building already has its full allocation of single rooms.");
            else if (selectedRoomType.RoomTypeName == RoomTypeNames.Sharing && sharingCount >= buildingLimits.Sharing)
                ModelState.AddModelError(nameof(model.RoomTypeId), "This building already has its full allocation of sharing rooms.");
        }
        var duplicate = await _db.Rooms.AnyAsync(r => r.PropertyId == model.PropertyId && r.RoomNumber == model.RoomNumber);
        if (duplicate)
            ModelState.AddModelError(nameof(model.RoomNumber), "That room number already exists in this building.");
        if (!ModelState.IsValid) return View("RoomForm", model);

        _db.Rooms.Add(new Room
        {
            PropertyId = model.PropertyId,
            RoomTypeId = model.RoomTypeId,
            RoomNumber = model.RoomNumber.Trim(),
            Notes = model.Notes
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Room added.";
        return RedirectToAction(nameof(Rooms));
    }

    [HttpGet]
    public async Task<IActionResult> Assignments()
    {
        var assignments = await _db.RoomAssignments
            .Include(a => a.User)
                .ThenInclude(u => u.Person)
            .Include(a => a.Room)
                .ThenInclude(r => r.Property)
            .Include(a => a.Room)
                .ThenInclude(r => r.RoomType)
            .OrderByDescending(a => a.MoveOutDate == null)
            .ThenBy(a => a.Room.Property.BuildingName)
            .ThenBy(a => a.Room.RoomNumber)
            .ThenBy(a => a.MoveInDate)
            .ToListAsync();

        var rooms = await _db.Rooms
            .Include(r => r.Property)
            .Include(r => r.RoomType)
            .Include(r => r.Assignments)
            .Where(r =>
                r.Property.PropertyStatus == PropertyStatuses.Active &&
                r.RoomType.Capacity > 0)
            .OrderBy(r => r.Property.BuildingName)
            .ThenBy(r => r.RoomNumber)
            .ToListAsync();

        ViewBag.Rooms = rooms;

        return View(assignments);
    }

    [HttpGet]
    public async Task<IActionResult> AssignRoom(int? userId = null)
    {
        await FillAssignmentLookups();
        var model = new AssignRoomViewModel { MoveInDate = DateTime.UtcNow.Date };
        if (userId.HasValue)
        {
            var user = await _db.Users.Include(u => u.Person).FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user != null)
            {
                model.UserId = user.Id;
                model.ResidentName = user.Person.FullName;
            }
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRoom(AssignRoomViewModel model)
    {
        await FillAssignmentLookups();

        var user = await _db.Users
            .Include(u => u.Person)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == model.UserId);

        if (user == null)
        {
            ModelState.AddModelError(
                nameof(model.UserId),
                "The selected resident was not found.");
        }
        else
        {
            var isResident = user.UserRoles.Any(ur =>
                ur.Role.Name == AppRoles.Student ||
                ur.Role.Name == AppRoles.Staff);

            if (!isResident)
            {
                ModelState.AddModelError(
                    nameof(model.UserId),
                    "Only students and staff members can be assigned accommodation.");
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    nameof(model.UserId),
                    "This user account is inactive.");
            }
        }

        var check = await _occupancy.CanAssignAsync(
            model.UserId,
            model.RoomId,
            model.MoveInDate.Date,
            null);

        if (!check.Ok)
        {
            ModelState.AddModelError(
                string.Empty,
                check.Error!);
        }

        if (!ModelState.IsValid)
            return View(model);

        if (user != null && !await _occupancy.EstablishSharingGenderAsync(model.RoomId, user.Person.Gender))
        {
            ModelState.AddModelError(string.Empty, "This room already has a resident of a different gender and cannot be shared.");
            return View(model);
        }

        _db.RoomAssignments.Add(new RoomAssignment
        {
            UserId = model.UserId,
            RoomId = model.RoomId,
            MoveInDate = model.MoveInDate.Date
        });

        await _db.SaveChangesAsync();

        await _notify.NotifyAsync(
            model.UserId,
            "Room assigned",
            "You have been assigned campus estate accommodation. Open Accommodation to view the details.",
            "Accommodation");

        TempData["Success"] =
            $"{user!.Person.FullName} was assigned accommodation successfully.";

        return RedirectToAction(nameof(Assignments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EndAssignment(int id)
    {
        var assignment = await _db.RoomAssignments
            .Include(a => a.User)
                .ThenInclude(u => u.Person)
            .Include(a => a.Room)
                .ThenInclude(r => r.Property)
            .FirstOrDefaultAsync(a => a.AssignmentId == id);

        if (assignment == null)
            return NotFound();

        if (assignment.MoveOutDate != null)
        {
            TempData["Error"] = "This assignment has already been closed.";
            return RedirectToAction(nameof(Assignments));
        }

        var moveOutDate = DateTime.UtcNow.Date;

        if (moveOutDate < assignment.MoveInDate.Date)
        {
            TempData["Error"] =
                "The assignment cannot be closed because the move-out date would be earlier than the move-in date.";

            return RedirectToAction(nameof(Assignments));
        }

        assignment.MoveOutDate = moveOutDate;

        await _db.SaveChangesAsync();

        await _notify.NotifyAsync(
            assignment.UserId,
            "Accommodation assignment ended",
            $"Your accommodation assignment for " +
            $"{assignment.Room.Property.BuildingName} " +
            $"{assignment.Room.RoomNumber} has been closed.",
            "Accommodation");

        TempData["Success"] =
            $"{assignment.User.Person.FullName}'s room assignment has been closed.";

        return RedirectToAction(nameof(Assignments));
    }

    public async Task<IActionResult> Payments(string? status = null, string? q = null)
    {
        var query = _db.Payments
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.PaymentStatus.StatusName == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p =>
                p.ReferenceNumber.Contains(q) ||
                p.Account.AccountNumber.Contains(q) ||
                p.Account.User.Person.FirstName.Contains(q) ||
                p.Account.User.Person.LastName.Contains(q) ||
                p.Account.User.Email!.Contains(q));
        }
        ViewBag.Status = status;
        ViewBag.Q = q;
        return View(await query.OrderByDescending(p => p.PaymentDate).ToListAsync());
    }

    public async Task<IActionResult> Reports(DateTime? from = null, DateTime? to = null, int? categoryId = null, string? status = null)
    {
        var model = await BuildReportAsync(from, to, categoryId, status);
        ViewBag.Categories = new SelectList(await _db.PaymentCategories.ToListAsync(), "CategoryId", "CategoryName", categoryId);
        return View(model);
    }

    public async Task<IActionResult> Export(DateTime? from = null, DateTime? to = null, int? categoryId = null, string? status = null)
    {
        var model = await BuildReportAsync(from, to, categoryId, status);
        var bytes = ReportCsv.Build(model.Payments);
        return File(bytes, "text/csv", $"campus-estate-report-{model.From:yyyyMMdd}-{model.To:yyyyMMdd}.csv");
    }

    private async Task<ReportViewModel> BuildReportAsync(DateTime? from, DateTime? to, int? categoryId, string? status)
    {
        var start = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = to ?? DateTime.UtcNow.Date.AddDays(1);
        var query = _db.Payments
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person)
            .Where(p => p.PaymentDate >= start && p.PaymentDate < end.AddDays(1));
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.PaymentStatus.StatusName == status);

        var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
        return new ReportViewModel
        {
            From = start,
            To = end,
            CategoryId = categoryId,
            Status = status,
            Payments = payments,
            Total = payments.Where(p => p.PaymentStatus.StatusName == PaymentStatusNames.Confirmed).Sum(p => p.Amount),
            ByCategory = payments.Where(p => p.PaymentStatus.StatusName == PaymentStatusNames.Confirmed)
                .GroupBy(p => p.Category.CategoryName)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount)),
            ByStatus = payments.GroupBy(p => p.PaymentStatus.StatusName)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<IActionResult> Activity()
    {
        ViewBag.Notifications = await _db.Notifications
            .Include(n => n.User).ThenInclude(u => u.Person)
            .OrderByDescending(n => n.CreatedAt)
            .Take(40)
            .ToListAsync();
        ViewBag.Assignments = await _db.RoomAssignments
            .Include(a => a.User).ThenInclude(u => u.Person)
            .Include(a => a.Room).ThenInclude(r => r.Property)
            .OrderByDescending(a => a.MoveInDate)
            .Take(15)
            .ToListAsync();
        var payments = await _db.Payments
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person)
            .OrderByDescending(p => p.PaymentDate)
            .Take(20)
            .ToListAsync();
        return View(payments);
    }

    private async Task<ApplicationUser?> LoadUserGraph(int id)
        => await _db.Users
            .Include(u => u.Person)
            .Include(u => u.EstateAccount)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.RoomAssignments).ThenInclude(a => a.Room).ThenInclude(r => r.Property)
            .Include(u => u.RoomAssignments).ThenInclude(a => a.Room).ThenInclude(r => r.RoomType)
            .FirstOrDefaultAsync(u => u.Id == id);

    private async Task<List<SelectListItem>> PersonOptions()
        => await _db.Persons
            .OrderBy(p => p.LastName)
            .Select(p => new SelectListItem
            {
                Value = p.PersonId.ToString(),
                Text = p.LastName + ", " + p.FirstName + " (#" + p.PersonId + ")"
            })
            .ToListAsync();

    private async Task FillRoomLookups()
    {
        ViewBag.Properties = new SelectList(await _db.Properties.OrderBy(p => p.BuildingName).ToListAsync(), "PropertyId", "BuildingName");
        ViewBag.RoomTypes = new SelectList(await _db.RoomTypes.ToListAsync(), "RoomTypeId", "RoomTypeName");
    }

    private async Task FillAssignmentLookups()
    {
        var residents = await _db.Users
            .Include(u => u.Person)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u =>
                u.IsActive &&
                u.UserRoles.Any(ur =>
                    ur.Role.Name == AppRoles.Student ||
                    ur.Role.Name == AppRoles.Staff) &&
                !u.RoomAssignments.Any(a => a.MoveOutDate == null))
            .OrderBy(u => u.Person.LastName)
            .ThenBy(u => u.Person.FirstName)
            .ToListAsync();

        ViewBag.Users = new SelectList(
            residents.Select(u => new
            {
                u.Id,
                Name = $"{u.Person.FullName} ({u.Email})"
            }),
            "Id",
            "Name");

        var rooms = await _db.Rooms
            .Include(r => r.Property)
            .Include(r => r.RoomType)
            .Include(r => r.Assignments)
            .Where(r =>
                r.Property.PropertyStatus == PropertyStatuses.Active &&
                r.RoomType.Capacity > 0)
            .OrderBy(r => r.Property.BuildingName)
            .ThenBy(r => r.RoomNumber)
            .ToListAsync();

        var availableRooms = rooms
            .Select(r =>
            {
                var occupied = r.Assignments.Count(a => a.MoveOutDate == null);

                return new
                {
                    r.RoomId,
                    r.Property.BuildingName,
                    r.RoomNumber,
                    r.RoomType.RoomTypeName,
                    Capacity = r.RoomType.Capacity,
                    Occupied = occupied
                };
            })
            .Where(r => r.Occupied < r.Capacity)
            .Select(r => new
            {
                r.RoomId,
                Label =
                    $"{r.BuildingName} {r.RoomNumber} · " +
                    $"{r.RoomTypeName} " +
                    $"({r.Occupied}/{r.Capacity} occupied · " +
                    $"{r.Capacity - r.Occupied} available)"
            })
            .ToList();

        ViewBag.Rooms = new SelectList(
            availableRooms,
            "RoomId",
            "Label");
    }
}
