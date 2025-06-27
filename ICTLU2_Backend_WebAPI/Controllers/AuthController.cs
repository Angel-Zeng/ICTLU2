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
    private readonly ConnectionStrings _cs;
    private readonly IConfiguration _cfg;
    public AuthController(ConnectionStrings cs, IConfiguration cfg) { _cs = cs; _cfg = cfg; }

    [HttpPost("register")]
    public IActionResult Register(LoginDto dto)
    {
        if (!ValidatePassword(dto.Password)) return BadRequest("Weak password");
        using var con = new SqlConnection(_cs.Sql); con.Open();

        using (var chk = con.CreateCommand())
        {
            chk.CommandText = "SELECT COUNT(*) FROM dbo.Users WHERE Username=@u";
            chk.Parameters.AddWithValue("@u", dto.Username);
            if ((int)chk.ExecuteScalar()! > 0) return BadRequest("Username taken");
        }

        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO dbo.Users (Username,PasswordHash) VALUES (@u,@p);";
            cmd.Parameters.AddWithValue("@u", dto.Username);
            cmd.Parameters.AddWithValue("@p", BCrypt.Net.BCrypt.HashPassword(dto.Password));
            cmd.ExecuteNonQuery();
        }
        return Ok("Registration Complete!");
    }

    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        using var con = new SqlConnection(_cs.Sql); con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id,PasswordHash FROM dbo.Users WHERE Username=@u";
        cmd.Parameters.AddWithValue("@u", dto.Username);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return Unauthorized();
        var id = r.GetInt32(0);
        var hash = r.GetString(1);
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, hash)) return Unauthorized();
        return Ok(new { token = GenerateJwt(id) });
    }

    string GenerateJwt(int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddDays(1), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    static bool ValidatePassword(string p) =>
        p.Length >= 10 && p.Any(char.IsLower) && p.Any(char.IsUpper) && p.Any(char.IsDigit) && p.Any(ch => !char.IsLetterOrDigit(ch));
}