using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Load user‑secrets in dev automatically (SDK ≥ 6.0 does this when project has <UserSecretsId>)
// Nothing extra to code.

// JWT auth
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt key missing");

builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", opt =>
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

// Expose connection string via DI
var connStr = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=game.db"; // fallback for demo
builder.Services.AddSingleton(new ConnectionStrings { Sqlite = connStr });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token like this: Bearer <your_token_here>"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

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

// 🔧 On first run, create tables if they don’t exist
InitDatabase(connStr);

app.Run();

static void InitDatabase(string cs)
{
    using var con = new SqliteConnection(cs);
    con.Open();
    var cmd = con.CreateCommand();
    cmd.CommandText = @"
    CREATE TABLE IF NOT EXISTS Users (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Username TEXT NOT NULL UNIQUE,
        PasswordHash TEXT NOT NULL
    );
    CREATE TABLE IF NOT EXISTS Worlds (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL,
        Width INTEGER NOT NULL,
        Height INTEGER NOT NULL,
        UserId INTEGER NOT NULL,
        FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
    CREATE TABLE IF NOT EXISTS WorldObjects (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Type TEXT NOT NULL,
        X REAL NOT NULL,
        Y REAL NOT NULL,
        WorldId INTEGER NOT NULL,
        FOREIGN KEY(WorldId) REFERENCES Worlds(Id) ON DELETE CASCADE
    );";
    cmd.ExecuteNonQuery();
}

public record ConnectionStrings { public string Sqlite { get; init; } = string.Empty; }
