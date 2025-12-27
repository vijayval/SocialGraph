# Stunsy.SocialGraph.Api - Implementation Details

## Architecture

### Project Structure
```
Stunsy.SocialGraph.Api/
├── Configuration/
│   └── GremlinConfiguration.cs       # Cosmos DB Gremlin settings
├── Controllers/
│   ├── FollowsController.cs          # Follow/Unfollow endpoints
│   └── UsersController.cs            # User query endpoints
├── Models/
│   ├── FollowRequest.cs              # Request model for follow/unfollow
│   ├── FollowResponse.cs             # Response model for follow operations
│   ├── UserResponse.cs               # User data response
│   ├── CountResponse.cs              # Count response
│   └── IsFollowingResponse.cs        # Is-following check response
├── Services/
│   ├── IGremlinService.cs            # Service interface
│   └── GremlinService.cs             # Gremlin operations implementation
├── Program.cs                         # Application startup and DI configuration
└── appsettings.json                   # Configuration including Gremlin settings
```

## Key Design Decisions

### 1. Idempotent Follow Operation
The follow operation checks if an edge already exists before creating a new one:
- If edge exists and is active: Returns existing follow information
- If edge exists but is inactive: Reactivates the edge (sets isActive = true)
- If edge doesn't exist: Creates new edge with createdAtUtc and isActive = true

### 2. Soft Delete for Unfollow
Instead of deleting the edge, the unfollow operation sets `isActive = false`:
- Preserves follow history
- Allows for analytics on past relationships
- Can be reactivated with follow operation

### 3. Dependency Injection
- `GremlinConfiguration` is injected via IOptions<T> pattern
- `IGremlinService` is registered as singleton for connection pooling
- Controllers receive service via constructor injection

### 4. Graph Queries

All queries filter by `isActive = true` to only return active relationships:

**Get Followers:**
```gremlin
g.V('{userId}').inE('follows').has('isActive', true).outV().id()
```

**Get Following:**
```gremlin
g.V('{userId}').outE('follows').has('isActive', true).inV().id()
```

**Get Followers Count:**
```gremlin
g.V('{userId}').inE('follows').has('isActive', true).count()
```

**Get Following Count:**
```gremlin
g.V('{userId}').outE('follows').has('isActive', true).count()
```

**Is Following:**
```gremlin
g.V('{followerId}').outE('follows').has('isActive', true).where(inV().hasId('{targetId}')).count()
```

### 5. Vertex Creation
Vertices are created automatically when a follow relationship is established:
```gremlin
g.V('{userId}').fold().coalesce(unfold(), addV('user').property('id', '{userId}'))
```

This ensures both follower and followee vertices exist before creating the edge.

## Configuration

The `GremlinConfiguration` class maps to the "Gremlin" section in appsettings.json:

```json
{
  "Gremlin": {
    "Hostname": "your-account.gremlin.cosmos.azure.com",
    "Port": 443,
    "Database": "socialGraphDb",
    "Container": "users",
    "AuthKey": "your-primary-key",
    "EnableSsl": true
  }
}
```

## Error Handling

All endpoints include try-catch blocks that:
1. Log errors with appropriate context
2. Return 500 Internal Server Error for unexpected exceptions
3. Return 400 Bad Request for validation errors

## Health Check

The `/health` endpoint is registered using ASP.NET Core's built-in health check system:
```csharp
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

## Testing Notes

Since this implementation requires a real Azure Cosmos DB instance with Gremlin API:
1. The API will build and start successfully without configuration
2. Actual operations will fail if Cosmos DB is not configured
3. For testing, configure a Cosmos DB account with Gremlin API enabled

## API Endpoint Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /follows/{followeeId} | Follow a user (idempotent) |
| DELETE | /follows/{followeeId} | Unfollow a user (soft delete) |
| GET | /users/{id}/followers | Get list of followers |
| GET | /users/{id}/following | Get list of users being followed |
| GET | /users/{id}/followers/count | Get count of followers |
| GET | /users/{id}/following/count | Get count of following |
| GET | /users/{id}/is-following/{targetId} | Check if following a user |
| GET | /health | Health check endpoint |

## Swagger UI

When running in Development mode, Swagger UI is available at:
- http://localhost:5195/swagger

This provides interactive API documentation and testing capabilities.
