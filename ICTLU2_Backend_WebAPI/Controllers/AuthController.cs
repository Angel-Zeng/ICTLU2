using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
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
    private readonly ConnectionStrings _ConString;
    private readonly IConfiguration _Config;
    public AuthController(ConnectionStrings cs, IConfiguration cfg) 
    {
        _ConString = cs;
        _Config = cfg;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] LoginDto dto)
    {
        if (!ValidatePassword(dto.Password)) return BadRequest("Password is not strong enough!");

        //Connection to SQLite database
        using var con = new SqliteConnection(_ConString.Sqlite);
        con.Open();

        // Checking if the username already exists or not
        using (var check = con.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @u";
            check.Parameters.AddWithValue("@u", dto.Username);
            long exists = (long)check.ExecuteScalar()!;
            if (exists > 0) return BadRequest("Username already taken!");
        }
        //Using BCrypt hasing the password and inserting the new user into the DB!
        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO Users (Username, PasswordHash) VALUES (@u, @p)";
            cmd.Parameters.AddWithValue("@u", dto.Username);
            cmd.Parameters.AddWithValue("@p", BCrypt.Net.BCrypt.HashPassword(dto.Password));
            cmd.ExecuteNonQuery();
        }
        return Ok("Registration complete!"); 
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        using var con = new SqliteConnection(_ConString.Sqlite);
        con.Open();
        using var cmd = con.CreateCommand();

        //Looking up the user in Users table (username) and returns id and the stored password hash
        cmd.CommandText = "SELECT Id, PasswordHash FROM Users WHERE Username = @u";
        cmd.Parameters.AddWithValue("@u", dto.Username);
        using var reader = cmd.ExecuteReader();

        // If no user is found, then no rows, then returns Unauthorized
        if (!reader.Read()) return Unauthorized();

        //If a user is found, then we check the password with the stored hashed password
        int id = reader.GetInt32(0);
        string hash = reader.GetString(1);

        // If the password does not match, then returns Unauthorized
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, hash)) return Unauthorized();

        // if the password matches, then a JWT token is being generated and returns that token
        var token = GenerateJwt(id);
        return Ok(new { token });
    }

    string GenerateJwt(int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_Config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddDays(1), signingCredentials: creds); //expires in 1 day
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
    // Set it so the password must have:
    // a length thats at least 10 characters
    // Contain at least one lowercase letter
    // An uppercase letter
    // A numer
    // A special character.
    static bool ValidatePassword(string p) =>
        p.Length >= 10 && p.Any(char.IsLower) && p.Any(char.IsUpper) && p.Any(char.IsDigit) && p.Any(ch => !char.IsLetterOrDigit(ch));
}