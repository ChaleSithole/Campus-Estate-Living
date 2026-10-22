using CampusEstateLiving.Models;

namespace CampusEstateLiving.ViewModels;

public class StaffMessagesIndexViewModel
{
    public List<StaffMessageContact> Contacts { get; set; } = new();
    public StaffConversationSummary? Selected { get; set; }
}

public class StaffMessageContact
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? LatestMessage { get; set; }
    public DateTime? LatestAt { get; set; }
    public int UnreadCount { get; set; }
}

public class StaffConversationSummary : StaffMessageContact
{
    public List<StaffMessage> Messages { get; set; } = new();
    public string? Draft { get; set; }
}
