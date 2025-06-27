using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────
//  Authentication & Authorization
// ─────────────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt key missing");

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────────────────────────────
//  Connection string via DI
// ─────────────────────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
              ?? throw new Exception("Missing connection string");
builder.Services.AddSingleton(new ConnectionStrings { Sql = connStr });

// ─────────────────────────────────────────────────────────────────────
//  MVC / Controllers
// ─────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─────────────────────────────────────────────────────────────────────
//  Swagger + JWT security definition
// ─────────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Basic doc info (optional)
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WorldBuilder API",
        Version = "v1"
    });

    // Add the Bearer scheme
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token like this: **Bearer &lt;your_token_here&gt;**"
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);

    // Make every operation require that scheme (unless [AllowAnonymous])
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
});

// ─────────────────────────────────────────────────────────────────────
//  Pipeline
// ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

public record ConnectionStrings
{
    public string Sql { get; init; } = string.Empty;
}
