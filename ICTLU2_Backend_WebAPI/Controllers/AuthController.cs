using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ICTLU2_Backend_WebAPI.DTO;
using ICTLU2_Backend_WebAPI.Services;

namespace ICTLU2_Backend_WebAPI.Controllers;

// Account endpoints van registreren en inloggen en JWT token generatie 

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService, IConfiguration config) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly IConfiguration _config = config;

    [HttpPost("register")]
    public async Task<IActionResult> Register(LoginDto dto)
    {
        try
        {
            var result = await _authService.RegisterUserAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        try
        {
            var userId = await _authService.LoginUserAsync(dto);
            return Ok(new { token = GenerateJwt(userId) });
        }
        catch (Exception ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    private string GenerateJwt(string userId)
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var creds = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}