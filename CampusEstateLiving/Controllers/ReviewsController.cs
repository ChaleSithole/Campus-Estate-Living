using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using CampusEstateLiving.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

[Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ReviewsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpGet]
    public IActionResult Create() => View(new ReviewCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var userId = int.Parse(_users.GetUserId(User)!);
        _db.Reviews.Add(new Review
        {
            UserId = userId,
            Rating = model.Rating,
            Comment = model.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Thank you. Your review has been recorded.";
        return RedirectToAction("Dashboard", "Home");
    }
}
