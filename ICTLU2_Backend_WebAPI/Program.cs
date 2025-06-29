using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//Jwt sleutel die mij zo enorm veel pijn gaf in azure en user secrets jemig
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new("JWT sleutel ontbreekt in configuratie");

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

//Hetzelfde voor de connectiestring
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new("Database connectiestring ontbreekt");

builder.Services.AddSingleton(new ConnectionStrings
{
    Sql = connectionString
});

// registreren van controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Virtual Daycare API",
        Version = "v1"
    });

    // Dit toegevoegd zodat er met de token geauthentificeerd kon worden in Swagger UI
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header: Bearer <token>"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

//bouweeennnn
var app = builder.Build();

//configueren van hhtp request pipelines
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

//had ik in een andere file willen zetten maar iemand zei dat het niet werkt met azure
public record ConnectionStrings
{
    public string Sql { get; init; } = string.Empty;
};