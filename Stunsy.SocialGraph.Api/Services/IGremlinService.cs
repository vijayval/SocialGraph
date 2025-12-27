using Stunsy.SocialGraph.Api.Models;

namespace Stunsy.SocialGraph.Api.Services;

public interface IGremlinService
{
    Task<FollowResponse> FollowUserAsync(string followerId, string followeeId);
    Task UnfollowUserAsync(string followerId, string followeeId);
    Task<List<UserResponse>> GetFollowersAsync(string userId);
    Task<List<UserResponse>> GetFollowingAsync(string userId);
    Task<int> GetFollowersCountAsync(string userId);
    Task<int> GetFollowingCountAsync(string userId);
    Task<bool> IsFollowingAsync(string followerId, string targetId);
}
