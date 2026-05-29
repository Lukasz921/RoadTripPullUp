using Users.Application.DTOs;

namespace Users.Application.Interfaces;

public interface IAuthService
{
    Task Register(RegisterDTO dto);
    Task<AuthResponseDTO> Login(LoginDTO dto);
    Task ResetPassword(string email);
    Task<AuthResponseDTO> GoogleLogin(string idToken);
}
