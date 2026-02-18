using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using Stunsy.SocialGraph.Api.Auth;
using Stunsy.SocialGraph.Api.Configuration;
using Stunsy.SocialGraph.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add configuration
builder.Services.Configure<GremlinConfiguration>(
    builder.Configuration.GetSection("Gremlin"));

// Add services to the container
builder.Services.AddSingleton<IGremlinService, GremlinService>();
builder.Services.AddControllers();

// Add health checks
builder.Services.AddHealthChecks();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Use our sanitizing handler (TokenHandlers is the new pipeline in .NET 9)
        options.TokenHandlers.Clear();
        options.TokenHandlers.Add(new SanitizingJsonWebTokenHandler());

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("p9L!s7QkZ8wN4fS2xY3mV6rB1tU0hC5D")),
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authValues = context.Request.Headers.TryGetValue("Authorization", out var vals) ? vals : default;
                var bearer = authValues.Where(v => v != null).FirstOrDefault(v => v!.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(bearer))
                {
                    // No bearer header -> tell the middleware not to try anything
                    context.NoResult();
                    return Task.CompletedTask;
                }

                var token = bearer.Substring(7).Trim();
                // Stash and force; handler will sanitize again
                context.HttpContext.Items["raw_token"] = token;
                context.Token = token;
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[JWT] Auth Failed: {context.Exception.GetType().Name} - {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalAllow", policy =>
      policy.WithOrigins("http://localhost:3000") // add other origins if needed
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map health check endpoint
app.MapHealthChecks("/health");

// Map controllers
app.MapControllers();
app.UseCors("LocalAllow");

app.Run();
