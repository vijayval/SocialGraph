namespace Stunsy.SocialGraph.Api.Models;

public class FollowResponse
{
    public string FollowerId { get; set; } = string.Empty;
    public string FolloweeId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public bool IsActive { get; set; }
}
