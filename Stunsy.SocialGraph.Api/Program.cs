using Stunsy.SocialGraph.Api.Configuration;
using Stunsy.SocialGraph.Api.Services;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();
