# SocialGraph

A .NET 8 Web API for managing social graph relationships using Azure Cosmos DB Gremlin API.

## Overview

This API allows you to manage follow/unfollow relationships between users in a social network. Each user is represented as a vertex in the graph, and follow relationships are represented as edges with properties.

## Features

- **Idempotent Follow**: Following a user multiple times is safe and won't create duplicate edges
- **Soft Unfollow**: Unfollowing marks the edge as inactive rather than deleting it
- **Dependency Injection**: Clean architecture with DI pattern
- **Health Checks**: Built-in health endpoint for monitoring
- **Azure Cosmos DB Gremlin**: Uses Azure Cosmos DB's Gremlin API for graph storage

## Graph Model

- **Vertex**: `user` with property `id` (userId)
- **Edge**: `follows` from follower → followee with properties:
  - `createdAtUtc`: DateTime when the follow was created
  - `isActive`: Boolean indicating if the follow is active

## API Endpoints

### Follow Management

#### Follow a User
```http
POST /follows/{followeeId}
Content-Type: application/json

{
  "followerId": "user1"
}
```

#### Unfollow a User
```http
DELETE /follows/{followeeId}
Content-Type: application/json

{
  "followerId": "user1"
}
```

### User Queries

#### Get Followers
```http
GET /users/{id}/followers
```
Returns list of user IDs who follow the specified user.

#### Get Following
```http
GET /users/{id}/following
```
Returns list of user IDs that the specified user follows.

#### Get Followers Count
```http
GET /users/{id}/followers/count
```
Returns the count of active followers.

#### Get Following Count
```http
GET /users/{id}/following/count
```
Returns the count of users being followed.

#### Check If Following
```http
GET /users/{id}/is-following/{targetId}
```
Returns whether the user is following the target user.

### Health Check
```http
GET /health
```

## Configuration

Configure your Azure Cosmos DB Gremlin connection in `appsettings.json`:

```json
{
  "Gremlin": {
    "Hostname": "your-cosmosdb-account.gremlin.cosmos.azure.com",
    "Port": 443,
    "Database": "socialGraphDb",
    "Container": "users",
    "AuthKey": "your-auth-key-here",
    "EnableSsl": true
  }
}
```

## Running the Application

1. Install .NET 8 SDK
2. Configure your Cosmos DB Gremlin connection in `appsettings.json`
3. Run the application:

```bash
cd Stunsy.SocialGraph.Api
dotnet run
```

4. Access Swagger UI at: `http://localhost:5195/swagger`

## Building the Project

```bash
cd Stunsy.SocialGraph.Api
dotnet build
```

## Dependencies

- .NET 8
- Gremlin.Net 3.8.0
- Swashbuckle.AspNetCore 6.6.2
- Microsoft.AspNetCore.OpenApi 8.0.22
