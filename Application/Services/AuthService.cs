using Application.DTOs;
using Application.Interfaces;
using Core.Entities;
using Core.Enums;
using Google.Apis.Auth;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;

    public AuthService(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher, 
        IJwtProvider jwtProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
    }

    public async Task Register(UserDTO dto)
    {
        var existingUser = await _userRepository.FindByEmail(dto.Email);
        if (existingUser != null)
        {
            throw new Exception("User with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Surname = dto.Surname,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = UserRole.REGULAR_USER,
        };

        await _userRepository.Save(user);
    }

    public async Task<AuthResponseDTO> Login(LoginDTO dto)
    {
        var user = await _userRepository.FindByEmail(dto.Email);
        
        if (user == null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            throw new Exception("Invalid email or password.");
        }

        var token = _jwtProvider.Generate(user);

        return new AuthResponseDTO
        {
            Token = token,
            ExpiresInSeconds = 7200,
            User = new AuthUserDTO
            {
                Id = user.Id,
                FirstName = user.Name,
                Role = user.Role.ToString()
            }
        };
    }

    public async Task<AuthResponseDTO> GoogleLogin(string idToken)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
        var user = await _userRepository.FindByEmail(payload.Email);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Name = payload.GivenName ?? "GoogleUser",
                Surname = payload.FamilyName ?? string.Empty,
                Email = payload.Email,
                PasswordHash = string.Empty,
                Role = UserRole.REGULAR_USER,
            };
            await _userRepository.Save(user);
        }

        var token = _jwtProvider.Generate(user);

        return new AuthResponseDTO
        {
            Token = token,
            ExpiresInSeconds = 7200,
            User = new AuthUserDTO
            {
                Id = user.Id,
                FirstName = user.Name,
                Role = user.Role.ToString()
            }
        };
    }

    public Task ResetPassword(string email)
    {
        throw new NotImplementedException("Reset password logic is not yet implemented.");
    }
}