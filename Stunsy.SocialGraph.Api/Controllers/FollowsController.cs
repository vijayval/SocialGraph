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

    [HttpPost("{followeeId}")]
    public async Task<ActionResult<FollowResponse>> Follow(string followeeId, [FromBody] FollowRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FollowerId))
        {
            return BadRequest("FollowerId is required");
        }

        if (string.IsNullOrWhiteSpace(followeeId))
        {
            return BadRequest("FolloweeId is required");
        }

        if (request.FollowerId == followeeId)
        {
            return BadRequest("Cannot follow yourself");
        }

        try
        {
            var result = await _gremlinService.FollowUserAsync(request.FollowerId, followeeId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Follow endpoint");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{followeeId}")]
    public async Task<ActionResult> Unfollow(string followeeId, [FromBody] FollowRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FollowerId))
        {
            return BadRequest("FollowerId is required");
        }

        if (string.IsNullOrWhiteSpace(followeeId))
        {
            return BadRequest("FolloweeId is required");
        }

        try
        {
            await _gremlinService.UnfollowUserAsync(request.FollowerId, followeeId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Unfollow endpoint");
            return StatusCode(500, "Internal server error");
        }
    }
}
