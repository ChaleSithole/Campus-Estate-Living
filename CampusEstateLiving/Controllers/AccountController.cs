using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using CampusEstateLiving.Services;
using CampusEstateLiving.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly EstateAccountService _accounts;
    private readonly NotificationService _notify;

    public AccountController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        ApplicationDbContext db,
        EstateAccountService accounts,
        NotificationService notify)
    {
        _signIn = signIn;
        _users = users;
        _db = db;
        _accounts = accounts;
        _notify = notify;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _users.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Dashboard", "Home");
        }

        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "This account is temporarily locked. Try again later.");
        else
            ModelState.AddModelError(string.Empty, "Invalid email or password.");

        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard", "Home");

        return View(new RegisterViewModel());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.AccountType is not (AppRoles.Student or AppRoles.Staff))
        {
            ModelState.AddModelError(
                nameof(model.AccountType),
                "Public registration is only available for students and staff.");
        }

        var expectedDigits = model.AccountType == AppRoles.Student ? 10 : 7;
        var userNumberPattern = expectedDigits == 7 ? @"^[0-9]{7}$" : @"^[0-9]{10}$";
        var userNumber = model.UserNumber?.Trim() ?? "";

        if (model.AccountType is AppRoles.Student or AppRoles.Staff
            && !System.Text.RegularExpressions.Regex.IsMatch(userNumber, userNumberPattern))
        {
            ModelState.AddModelError(
                nameof(model.UserNumber),
                $"{(model.AccountType == AppRoles.Student ? "Student" : "Staff")} number must be exactly {expectedDigits} digits (0-9 only).");
        }

        if (model.DateOfBirth > DateTime.Today || model.DateOfBirth < DateTime.Today.AddYears(-120))
            ModelState.AddModelError(nameof(model.DateOfBirth), "Enter a valid date of birth.");

        if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
        {
            var cleanedPhone = System.Text.RegularExpressions.Regex.Replace(
                model.PhoneNumber.Trim(),
                @"[\s\-()]",
                "");

            if (!System.Text.RegularExpressions.Regex.IsMatch(cleanedPhone, @"^\+?[0-9]{10,15}$"))
            {
                ModelState.AddModelError(
                    nameof(model.PhoneNumber),
                    "Phone must be 10–15 digits only (optional + at the start). No letters or spaces.");
            }
        }

        var email = model.Email.Trim().ToLowerInvariant();
        var emailOk = model.AccountType switch
        {
            AppRoles.Student => System.Text.RegularExpressions.Regex.IsMatch(
                email, @"^[a-z0-9._%+\-]+@ufs4life\.ac\.za$"),
            AppRoles.Staff => System.Text.RegularExpressions.Regex.IsMatch(
                email, @"^[a-z0-9._%+\-]+@ufs\.ac\.za$"),
            _ => false
        };

        if (!emailOk)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                model.AccountType == AppRoles.Student
                    ? "Student email must match: name@ufs4life.ac.za"
                    : "Staff email must match: name@ufs.ac.za");
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var pwd = model.Password;
            var hasUpper = System.Text.RegularExpressions.Regex.IsMatch(pwd, @"[A-Z]");
            var hasLower = System.Text.RegularExpressions.Regex.IsMatch(pwd, @"[a-z]");
            var hasDigit = System.Text.RegularExpressions.Regex.IsMatch(pwd, @"\d");
            var hasSymbol = System.Text.RegularExpressions.Regex.IsMatch(pwd, @"[^A-Za-z0-9]");

            if (!hasUpper || !hasLower || !hasDigit || !hasSymbol)
            {
                ModelState.AddModelError(
                    nameof(model.Password),
                    "Password must be at least 8 characters with uppercase, lowercase, a digit and a symbol.");
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        if (await _users.FindByEmailAsync(email) != null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "An account with this email already exists.");

            return View(model);
        }

        // Explicitly specify IActionResult so the correct overload is used
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                var person = new Person
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

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    PersonId = person.PersonId,
                    UserNumber = userNumber,
                    PhoneNumber = model.PhoneNumber,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var created = await _users.CreateAsync(user, model.Password);

                if (!created.Succeeded)
                {
                    foreach (var error in created.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    await transaction.RollbackAsync();
                    ViewBag.AccountCreated = false;
                    return View(model);
                }

                var roleResult = await _users.AddToRoleAsync(user, model.AccountType);

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    await transaction.RollbackAsync();
                    ViewBag.AccountCreated = false;
                    return View(model);
                }

                await _accounts.EnsureForResidentAsync(user);

                await _notify.NotifyAsync(
                    user.Id,
                    "Welcome to Campus Estate Living",
                    "Your resident account is ready. Add a card or bank account, then pay rent, water, electricity, parking or refuse from the Payments menu.",
                    "Welcome");

                await transaction.CommitAsync();

                await _signIn.SignInAsync(user, isPersistent: false);

                TempData["Success"] = "Account created. Welcome to Campus Estate Living.";

                return RedirectToAction("Dashboard", "Home");
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "We could not create your account. Please try again.");

                return View(model);
            }
        });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        TempData["Success"] = "You have been signed out.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        await _signIn.RefreshSignInAsync(user);

        TempData["Success"] = "Password updated.";
        return RedirectToAction("Index", "Profile");
    }

    public IActionResult AccessDenied() => View();
}