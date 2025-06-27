using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ICTLU2_Backend_WebAPI.DTO;
using ICTLU2_Backend_WebAPI.Models;

namespace ICTLU2_Backend_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ConnectionStrings _conString;
    private readonly IConfiguration _config;

    public AuthController(ConnectionStrings cs, IConfiguration cfg)
    {
        _conString = cs;
        _config = cfg;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] LoginDto dto)
    {
        if (!ValidatePassword(dto.Password))
            return BadRequest("Password is not strong enough!");

        using var con = new SqlConnection(_conString.Sql);
        con.Open();

        using (var check = con.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM dbo.Users WHERE Username = @u";
            check.Parameters.AddWithValue("@u", dto.Username);
            var exists = (int)check.ExecuteScalar()!;
            if (exists > 0) return BadRequest("Username already taken!");
        }

        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO dbo.Users (Username, PasswordHash)
                VALUES (@u, @p);
            """;
            cmd.Parameters.AddWithValue("@u", dto.Username);
            cmd.Parameters.AddWithValue("@p", BCrypt.Net.BCrypt.HashPassword(dto.Password));
            cmd.ExecuteNonQuery();
        }

        return Ok("Registration complete!");
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        using var con = new SqlConnection(_conString.Sql);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id, PasswordHash
            FROM dbo.Users
            WHERE Username = @u
        """;
        cmd.Parameters.AddWithValue("@u", dto.Username);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return Unauthorized();

        var id = reader.GetInt32(0);
        var hash = reader.GetString(1);

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, hash))
            return Unauthorized();

        var token = GenerateJwt(id);
        return Ok(new { token });
    }

    string GenerateJwt(int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    static bool ValidatePassword(string p) =>
        p.Length >= 10 &&
        p.Any(char.IsLower) &&
        p.Any(char.IsUpper) &&
        p.Any(char.IsDigit) &&
        p.Any(ch => !char.IsLetterOrDigit(ch));
}