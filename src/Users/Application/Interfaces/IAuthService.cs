using Users.Application.DTOs;

namespace Users.Application.Interfaces;

public interface IAuthService
{
    Task Register(RegisterDTO dto);
    Task<AuthResponseDTO> Login(LoginDTO dto);
    Task ResetPassword(ResetPasswordDTO dto);
    Task<AuthResponseDTO> GoogleLogin(string idToken);
}
