using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Extensions.Options;
using Stunsy.SocialGraph.Api.Configuration;
using Stunsy.SocialGraph.Api.Models;

namespace Stunsy.SocialGraph.Api.Services;

public sealed class GremlinService : IGremlinService, IDisposable
{
    private readonly GremlinClient _gremlinClient;
    private readonly ILogger<GremlinService> _logger;

    private const string VertexLabel = "profile";
    private const string EdgeLabel = "follows";

    public GremlinService(IOptions<GremlinConfiguration> config, ILogger<GremlinService> logger)
    {
        _logger = logger;
        var c = config.Value;

        var gremlinServer = new GremlinServer(
            hostname: c.Hostname,
            port: c.Port,
            enableSsl: c.EnableSsl,
            username: $"/dbs/{c.Database}/colls/{c.Container}",
            password: c.AuthKey
        );

        // GraphSON2 is OK; GraphSON3 also works if you prefer.
        _gremlinClient = new GremlinClient(gremlinServer, new GraphSON2MessageSerializer());
    }

    private static string Vid(string profileId) => $"profile:{profileId}";

    // Basic escaping to avoid breaking Gremlin string literals.
    private static string Esc(string s) => s.Replace("\\", "\\\\").Replace("'", "\\'");

    private async Task EnsureProfileVertexAsync(string profileId)
    {
        var pid = Esc(profileId);
        var vid = Esc(Vid(profileId));

        // Graph partition key path is /pk, so every vertex must have pk
        var q =
            "g.V('" + vid + "').fold().coalesce(unfold()," +
            "addV('" + VertexLabel + "')" +
            ".property('id','" + vid + "')" +
            ".property('pk','" + pid + "')" +
            ".property('profileId','" + pid + "')" +
            ")";

        await _gremlinClient.SubmitAsync<dynamic>(q);
    }

    public async Task<FollowResponse> FollowUserAsync(string followerId, string followeeId)
    {
        if (string.IsNullOrWhiteSpace(followerId)) throw new ArgumentException(nameof(followerId));
        if (string.IsNullOrWhiteSpace(followeeId)) throw new ArgumentException(nameof(followeeId));
        if (followerId == followeeId) throw new InvalidOperationException("Cannot follow yourself.");

        try
        {
            await EnsureProfileVertexAsync(followerId);
            await EnsureProfileVertexAsync(followeeId);

            var fId = Esc(followerId);
            var feId = Esc(followeeId);
            var fVid = Esc(Vid(followerId));
            var feVid = Esc(Vid(followeeId));
            var now = DateTime.UtcNow;

            // ✅ Idempotent: create edge only if missing
            // ✅ Edge must have pk (we store followerId as pk for outgoing edges)
            var q =
                "g.V('" + fVid + "').coalesce(" +
                "outE('" + EdgeLabel + "').where(inV().hasId('" + feVid + "'))," +
                "addE('" + EdgeLabel + "').to(g.V('" + feVid + "'))" +
                ".property('pk','" + fId + "')" +
                ".property('createdAtUtc','" + now.ToString("o") + "')" +
                ".property('createdBy','" + fId + "')" +
                ")" ;

            await _gremlinClient.SubmitAsync<dynamic>(q);

            return new FollowResponse
            {
                FollowerId = followerId,
                FolloweeId = followeeId,
                CreatedAtUtc = now,
                IsActive = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error following {FollowerId} -> {FolloweeId}", followerId, followeeId);
            throw;
        }
    }

    public async Task UnfollowUserAsync(string followerId, string followeeId)
    {
        if (string.IsNullOrWhiteSpace(followerId)) throw new ArgumentException(nameof(followerId));
        if (string.IsNullOrWhiteSpace(followeeId)) throw new ArgumentException(nameof(followeeId));

        try
        {
            var fVid = Esc(Vid(followerId));
            var feVid = Esc(Vid(followeeId));

            // ✅ Hard delete edge (recommended)
            var q =
                "g.V('" + fVid + "').outE('" + EdgeLabel + "')" +
                ".where(inV().hasId('" + feVid + "')).drop()";

            await _gremlinClient.SubmitAsync<dynamic>(q);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unfollowing {FollowerId} -> {FolloweeId}", followerId, followeeId);
            throw;
        }
    }

    public async Task<List<UserResponse>> GetFollowersAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return [];

        try
        {
            var uVid = Esc(Vid(userId));

            // followers = in('follows')
            var q = "g.V('" + uVid + "').in('" + EdgeLabel + "').values('profileId')";
            var results = await _gremlinClient.SubmitAsync<string>(q);

            return results.Select(x => new UserResponse { UserId = x }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting followers for {UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserResponse>> GetFollowingAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return [];

        try
        {
            var uVid = Esc(Vid(userId));

            // following = out('follows')
            var q = "g.V('" + uVid + "').out('" + EdgeLabel + "').values('profileId')";
            var results = await _gremlinClient.SubmitAsync<string>(q);

            return results.Select(x => new UserResponse { UserId = x }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting following for {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetFollowersCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return 0;

        try
        {
            var uVid = Esc(Vid(userId));
            var q = "g.V('" + uVid + "').inE('" + EdgeLabel + "').count()";
            var results = await _gremlinClient.SubmitAsync<long>(q);
            return (int)results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting followers count for {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetFollowingCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return 0;

        try
        {
            var uVid = Esc(Vid(userId));
            var q = "g.V('" + uVid + "').outE('" + EdgeLabel + "').count()";
            var results = await _gremlinClient.SubmitAsync<long>(q);
            return (int)results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting following count for {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsFollowingAsync(string followerId, string targetId)
    {
        if (string.IsNullOrWhiteSpace(followerId) || string.IsNullOrWhiteSpace(targetId))
            return false;

        try
        {
            var fVid = Esc(Vid(followerId));
            var tVid = Esc(Vid(targetId));

            var q = "g.V('" + fVid + "').out('" + EdgeLabel + "').hasId('" + tVid + "').limit(1).count()";
            var results = await _gremlinClient.SubmitAsync<long>(q);

            return results.FirstOrDefault() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking IsFollowing {FollowerId} -> {TargetId}", followerId, targetId);
            throw;
        }
    }

    public void Dispose() => _gremlinClient.Dispose();
}
