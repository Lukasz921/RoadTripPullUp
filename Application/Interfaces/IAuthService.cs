using Application.DTOs;

namespace Application.Interfaces;

public interface IAuthService
{
    Task Register(UserDTO dto);
    Task<AuthResponseDTO> Login(LoginDTO dto);
    Task ResetPassword(string email);
    Task<AuthResponseDTO> GoogleLogin(string idToken);
}