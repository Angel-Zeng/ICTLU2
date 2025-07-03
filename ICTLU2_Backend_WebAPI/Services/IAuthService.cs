using ICTLU2_Backend_WebAPI.DTO;
namespace ICTLU2_Backend_WebAPI.Services;

public interface IAuthService
{
    Task<string> RegisterUserAsync(LoginDto dto);
    Task<string> LoginUserAsync(LoginDto dto);
}