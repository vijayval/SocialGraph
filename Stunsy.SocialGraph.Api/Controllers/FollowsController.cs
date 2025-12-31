using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stunsy.SocialGraph.Api.Models;
using Stunsy.SocialGraph.Api.Services;

namespace Stunsy.SocialGraph.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class FollowsController : ControllerBase
{
    private readonly IGremlinService _gremlinService;
    private readonly ILogger<FollowsController> _logger;

    public FollowsController(IGremlinService gremlinService, ILogger<FollowsController> logger)
    {
        _gremlinService = gremlinService;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("{followeeId}")]
public async Task<ActionResult<FollowResponse>> Follow(string followeeId)
{
    if (string.IsNullOrWhiteSpace(followeeId))
        return BadRequest("FolloweeId is required");

    var followerId =
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        User.FindFirst("sub")?.Value ??
        User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    if (string.IsNullOrWhiteSpace(followerId))
        return Unauthorized(); // better than BadRequest

    if (followerId == followeeId)
        return BadRequest("Cannot follow yourself");

    try
    {
        var result = await _gremlinService.FollowUserAsync(followerId, followeeId);
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in Follow endpoint");
        return StatusCode(500, "Internal server error");
    }
}


[Authorize]
[HttpDelete("{followeeId}")]
public async Task<ActionResult> Unfollow(string followeeId)
{
     var followerId =
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        User.FindFirst("sub")?.Value ??
        User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    if (string.IsNullOrWhiteSpace(followerId))
        return BadRequest("FollowerId is required");

    if (string.IsNullOrWhiteSpace(followeeId))
        return BadRequest("FolloweeId is required");

    try
    {
        await _gremlinService.UnfollowUserAsync(followerId, followeeId);
        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in Unfollow endpoint");
        return StatusCode(500, "Internal server error");
    }
}

}
