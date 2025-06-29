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
public class AuthController(ConnectionStrings conString, IConfiguration conFig) : ControllerBase
{
    private readonly ConnectionStrings _conString = conString;
    private readonly IConfiguration _conFig = conFig;

    
    //Endpoint voor registreren 
    [HttpPost("register")]
    public IActionResult Register(LoginDto dto)
    {
        //wachtwoorod check 
        if (!ValidatePassword(dto.Password))
            return BadRequest("Wachtwoord moet minimaal 10 tekens, 1 hoofdletters, 1 cijfers en 1 speciaal teken!");

        using var con = new SqlConnection(_conString.Sql);
        con.Open();

        //Controleren of de gebruiksnaam al bestaat 
        using var chk = new SqlCommand("SELECT COUNT(*) FROM dbo.Users WHERE Username=@username", con);
        chk.Parameters.AddWithValue("@username", dto.Username);
        if ((int)chk.ExecuteScalar()! > 0)
            return BadRequest("Gebruikersnaam is al in gebruik");

        // nieuwe gebruiker
        using var cmd = new SqlCommand(
            "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (@username, @password);",
            con);

        cmd.Parameters.AddWithValue("@username", dto.Username);
        cmd.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(dto.Password));
        cmd.ExecuteNonQuery();

        return Ok("Registratie succesvol");
    }

   
    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        using var con = new SqlConnection(_conString.Sql);
        con.Open();

        //zoeken in database
        using var cmd = new SqlCommand(
            "SELECT Id, PasswordHash FROM dbo.Users WHERE Username=@u",
            con);

        cmd.Parameters.AddWithValue("@u", dto.Username);
        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return Unauthorized("Ongeldige gebruikersnaam of wachtwoord");

        var id = r.GetInt32(0);
        var hash = r.GetString(1);

        //awachtwoord controlereren
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, hash))
            return Unauthorized("Ongeldige gebruikersnaam of wachtwoord");

        //Genereren van enorme token
        return Ok(new { token = GenerateJwt(id) });
    }

   //jwt token
    private string GenerateJwt(int userId)
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_conFig["Jwt:Key"]!));

        var creds = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    //valideren van wachtwoord
    private static bool ValidatePassword(string password) =>
        password.Length >= 10 &&
        password.Any(char.IsLower) &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsDigit) &&
        password.Any(ch => !char.IsLetterOrDigit(ch));
}