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

    private static string Vid(string profileId) => $"profile:{profileId}";

    private static string Esc(string s) => s.Replace("\\", "\\\\").Replace("'", "\\'");

    public async Task<FollowResponse> FollowUserAsync(string followerId, string followeeId)
    {
        try
        {
            var fId = Esc(followerId);
            var feId = Esc(followeeId);
            var fVid = Esc(Vid(followerId));
            var feVid = Esc(Vid(followeeId));

            // Create follower vertex if not exists with proper pk
            await _gremlinClient.SubmitAsync<dynamic>(
                $"g.V('{fVid}').fold().coalesce(unfold(), addV('profile').property('id', '{fVid}').property('pk', '{fId}').property('profileId', '{fId}'))"
            );

            // Create followee vertex if not exists with proper pk
            await _gremlinClient.SubmitAsync<dynamic>(
                $"g.V('{feVid}').fold().coalesce(unfold(), addV('profile').property('id', '{feVid}').property('pk', '{feId}').property('profileId', '{feId}'))"
            );

            // Check if edge already exists
            var existingEdgeQuery = $"g.V('{fVid}').outE('follows').where(inV().hasId('{feVid}'))";
            var existingEdges = await _gremlinClient.SubmitAsync<dynamic>(existingEdgeQuery);
            var edgeList = existingEdges.ToList();

            DateTime createdAtUtc;
            
            if (edgeList.Count > 0)
            {
                // Edge exists - reactivate if inactive (idempotent)
                var edge = edgeList[0];
                bool isActive = true;
                try
                {
                    isActive = edge["isActive"];
                }
                catch
                {
                    isActive = false;
                }
                
                if (!isActive)
                {
                    await _gremlinClient.SubmitAsync<dynamic>(
                        $"g.V('{fVid}').outE('follows').where(inV().hasId('{feVid}')).property('isActive', true)"
                    );
                }
                
                // Get createdAtUtc from existing edge
                try
                {
                    var createdAtValue = edge["createdAtUtc"];
                    createdAtUtc = DateTime.Parse(createdAtValue.ToString());
                }
                catch
                {
                    createdAtUtc = DateTime.UtcNow;
                }
            }
            else
            {
                // Create new edge with pk property
                createdAtUtc = DateTime.UtcNow;
                await _gremlinClient.SubmitAsync<dynamic>(
                    $"g.V('{fVid}').addE('follows').to(g.V('{feVid}')).property('pk', '{fId}').property('createdAtUtc', '{createdAtUtc:o}').property('createdBy', '{fId}')"
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
            var fVid = Esc(Vid(followerId));
            var feVid = Esc(Vid(followeeId));
            
            // Drop the edge (hard delete)
            await _gremlinClient.SubmitAsync<dynamic>(
                $"g.V('{fVid}').outE('follows').where(inV().hasId('{feVid}')).drop()"
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
            // Try with profile: prefix first
            var vid = $"profile:{userId}";
            var query = $"g.V('{vid}').inE('follows').outV().values('profileId')";
            var results = await _gremlinClient.SubmitAsync<string>(query);
            
            return results.Select(id => new UserResponse { UserId = id }).ToList();
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
            // Try with profile: prefix first
            var vid = $"profile:{userId}";
            var query = $"g.V('{vid}').outE('follows').inV().values('profileId')";
            _logger.LogInformation("Executing Gremlin query: {Query}", query);
            var results = await _gremlinClient.SubmitAsync<string>(query);
            
            return results.Select(id => new UserResponse { UserId = id }).ToList();
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
            var vid = $"profile:{userId}";
            var query = $"g.V('{vid}').inE('follows').count()";
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
            var vid = $"profile:{userId}";
            var query = $"g.V('{vid}').outE('follows').count()";
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
            var fVid = $"profile:{followerId}";
            var tVid = $"profile:{targetId}";
            var query = $"g.V('{fVid}').outE('follows').where(inV().hasId('{tVid}')).count()";
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
