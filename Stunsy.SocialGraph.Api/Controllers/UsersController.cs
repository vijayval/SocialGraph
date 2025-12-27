using Microsoft.AspNetCore.Mvc;
using Stunsy.SocialGraph.Api.Models;
using Stunsy.SocialGraph.Api.Services;

namespace Stunsy.SocialGraph.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IGremlinService _gremlinService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IGremlinService gremlinService, ILogger<UsersController> logger)
    {
        _gremlinService = gremlinService;
        _logger = logger;
    }

    [HttpGet("{id}/followers")]
    public async Task<ActionResult<List<UserResponse>>> GetFollowers(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("User ID is required");
        }

        try
        {
            var result = await _gremlinService.GetFollowersAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFollowers endpoint");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}/following")]
    public async Task<ActionResult<List<UserResponse>>> GetFollowing(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("User ID is required");
        }

        try
        {
            var result = await _gremlinService.GetFollowingAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFollowing endpoint");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}/followers/count")]
    public async Task<ActionResult<CountResponse>> GetFollowersCount(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("User ID is required");
        }

        try
        {
            var count = await _gremlinService.GetFollowersCountAsync(id);
            return Ok(new CountResponse { Count = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFollowersCount endpoint");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}/following/count")]
    public async Task<ActionResult<CountResponse>> GetFollowingCount(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("User ID is required");
        }

        try
        {
            var count = await _gremlinService.GetFollowingCountAsync(id);
            return Ok(new CountResponse { Count = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFollowingCount endpoint");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}/is-following/{targetId}")]
    public async Task<ActionResult<IsFollowingResponse>> IsFollowing(string id, string targetId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("User ID is required");
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            return BadRequest("Target ID is required");
        }

        try
        {
            var isFollowing = await _gremlinService.IsFollowingAsync(id, targetId);
            return Ok(new IsFollowingResponse { IsFollowing = isFollowing });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in IsFollowing endpoint");
            return StatusCode(500, "Internal server error");
        }
    }
}
