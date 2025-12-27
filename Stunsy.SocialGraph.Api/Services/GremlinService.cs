using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure.IO.GraphSON;
using Stunsy.SocialGraph.Api.Configuration;
using Stunsy.SocialGraph.Api.Models;
using Microsoft.Extensions.Options;

namespace Stunsy.SocialGraph.Api.Services;

public class GremlinService : IGremlinService, IDisposable
{
    private readonly GremlinClient _gremlinClient;
    private readonly ILogger<GremlinService> _logger;

    public GremlinService(IOptions<GremlinConfiguration> config, ILogger<GremlinService> logger)
    {
        _logger = logger;
        var gremlinConfig = config.Value;

        var gremlinServer = new GremlinServer(
            gremlinConfig.Hostname,
            gremlinConfig.Port,
            enableSsl: gremlinConfig.EnableSsl,
            username: $"/dbs/{gremlinConfig.Database}/colls/{gremlinConfig.Container}",
            password: gremlinConfig.AuthKey
        );

        var messageSerializer = new GraphSON2MessageSerializer();
        _gremlinClient = new GremlinClient(gremlinServer, messageSerializer);
    }

    public async Task<FollowResponse> FollowUserAsync(string followerId, string followeeId)
    {
        try
        {
            // Create follower vertex if not exists
            await _gremlinClient.SubmitAsync<dynamic>(
                $"g.V('{followerId}').fold().coalesce(unfold(), addV('user').property('id', '{followerId}'))"
            );

            // Create followee vertex if not exists
            await _gremlinClient.SubmitAsync<dynamic>(
                $"g.V('{followeeId}').fold().coalesce(unfold(), addV('user').property('id', '{followeeId}'))"
            );

            // Check if edge already exists
            var existingEdgeQuery = $"g.V('{followerId}').outE('follows').where(inV().hasId('{followeeId}'))";
            var existingEdges = await _gremlinClient.SubmitAsync<dynamic>(existingEdgeQuery);
            var edgeList = existingEdges.ToList();

            DateTime createdAtUtc;
            
            if (edgeList.Count > 0)
            {
                // Edge exists - reactivate if inactive (idempotent)
                var edge = edgeList[0];
                var isActive = edge.GetValueOrDefault("isActive", false);
                
                if (!isActive)
                {
                    await _gremlinClient.SubmitAsync<dynamic>(
                        $"g.V('{followerId}').outE('follows').where(inV().hasId('{followeeId}')).property('isActive', true)"
                    );
                }
                
                // Get createdAtUtc from existing edge
                var createdAtValue = edge.GetValueOrDefault("createdAtUtc", DateTime.UtcNow.ToString("o"));
                createdAtUtc = DateTime.Parse(createdAtValue.ToString());
            }
            else
            {
                // Create new edge
                createdAtUtc = DateTime.UtcNow;
                await _gremlinClient.SubmitAsync<dynamic>(
                    $"g.V('{followerId}').addE('follows').to(g.V('{followeeId}')).property('createdAtUtc', '{createdAtUtc:o}').property('isActive', true)"
                );
            }

            return new FollowResponse
            {
                FollowerId = followerId,
                FolloweeId = followeeId,
                CreatedAtUtc = createdAtUtc,
                IsActive = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error following user {FollowerId} -> {FolloweeId}", followerId, followeeId);
            throw;
        }
    }

    public async Task UnfollowUserAsync(string followerId, string followeeId)
    {
        try
        {
            // Soft delete - set isActive to false
            await _gremlinClient.SubmitAsync<dynamic>(
                $"g.V('{followerId}').outE('follows').where(inV().hasId('{followeeId}')).property('isActive', false)"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unfollowing user {FollowerId} -> {FolloweeId}", followerId, followeeId);
            throw;
        }
    }

    public async Task<List<UserResponse>> GetFollowersAsync(string userId)
    {
        try
        {
            var query = $"g.V('{userId}').inE('follows').has('isActive', true).outV().id()";
            var results = await _gremlinClient.SubmitAsync<dynamic>(query);
            
            return results.Select(id => new UserResponse { UserId = id.ToString() }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting followers for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserResponse>> GetFollowingAsync(string userId)
    {
        try
        {
            var query = $"g.V('{userId}').outE('follows').has('isActive', true).inV().id()";
            var results = await _gremlinClient.SubmitAsync<dynamic>(query);
            
            return results.Select(id => new UserResponse { UserId = id.ToString() }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting following for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetFollowersCountAsync(string userId)
    {
        try
        {
            var query = $"g.V('{userId}').inE('follows').has('isActive', true).count()";
            var results = await _gremlinClient.SubmitAsync<long>(query);
            return (int)results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting followers count for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetFollowingCountAsync(string userId)
    {
        try
        {
            var query = $"g.V('{userId}').outE('follows').has('isActive', true).count()";
            var results = await _gremlinClient.SubmitAsync<long>(query);
            return (int)results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting following count for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsFollowingAsync(string followerId, string targetId)
    {
        try
        {
            var query = $"g.V('{followerId}').outE('follows').has('isActive', true).where(inV().hasId('{targetId}')).count()";
            var results = await _gremlinClient.SubmitAsync<long>(query);
            return results.FirstOrDefault() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {FollowerId} is following {TargetId}", followerId, targetId);
            throw;
        }
    }

    public void Dispose()
    {
        _gremlinClient?.Dispose();
    }
}
