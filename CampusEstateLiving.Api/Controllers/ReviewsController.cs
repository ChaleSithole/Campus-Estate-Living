using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly CampusEstateDbContext _db;

    public ReviewsController(CampusEstateDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.FinancialOfficer}")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetAll()
    {
        var reviews = await _db.Reviews
            .AsNoTracking()
            .Include(r => r.User).ThenInclude(u => u.Person)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto
            {
                ReviewId = r.ReviewId,
                UserId = r.UserId,
                AuthorName = r.User.Person.FirstName + " " + r.User.Person.LastName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpGet("mine")]
    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> Mine()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var reviews = await _db.Reviews
            .AsNoTracking()
            .Include(r => r.User).ThenInclude(u => u.Person)
            .Where(r => r.UserId == userId.Value)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto
            {
                ReviewId = r.ReviewId,
                UserId = r.UserId,
                AuthorName = r.User.Person.FirstName + " " + r.User.Person.LastName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    public async Task<ActionResult<ReviewDto>> Create(CreateReviewRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        if (request.Rating is < 1 or > 5)
        {
            return BadRequest(new { message = "Rating must be between 1 and 5." });
        }

        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            return BadRequest(new { message = "A comment is required." });
        }

        var review = new Review
        {
            UserId = userId.Value,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        var user = await _db.Users.Include(u => u.Person)
            .FirstAsync(u => u.Id == userId.Value);

        return Ok(new ReviewDto
        {
            ReviewId = review.ReviewId,
            UserId = review.UserId,
            AuthorName = user.Person.FullName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        });
    }
}
