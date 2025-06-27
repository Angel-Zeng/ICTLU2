using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── JWT Auth ───────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new("Jwt key missing");

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

// ─── Connection string via DI ───────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
              ?? throw new("Missing connection string");
builder.Services.AddSingleton(new ConnectionStrings { Sql = connStr });

// ─── Controllers ────────────────────────────────────────────────
builder.Services.AddControllers();

// ─── Swagger + Bearer support ──────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "WorldBuilder API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste token: **Bearer &lt;token&gt;**"
    };
    o.AddSecurityDefinition("Bearer", jwtScheme);
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
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
app.Run();

public record ConnectionStrings { public string Sql { get; init; } = string.Empty; }