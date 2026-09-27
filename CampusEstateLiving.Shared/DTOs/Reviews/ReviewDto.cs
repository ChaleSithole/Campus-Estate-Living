namespace CampusEstateLiving.Shared.DTOs.Reviews;

public class ReviewDto
{
    public int ReviewId { get; set; }
    public int UserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewRequest
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
