using Users.Core;
using Google.Apis.Auth;
using Users.Application.DTOs;
using Users.Application.Interfaces;

namespace Users.Application.Services;

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
            PhoneNumber = dto.PhoneNumber,
            DateOfBirth = dto.DateOfBirth,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = UserRole.REGULAR_USER,
        };

        if (Enum.TryParse<Sex>(dto.Sex, true, out var sex))
        {
            user.Sex = sex;
        }
        else
        {
            throw new Exception("Invalid value for Sex.");
        }

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
                Name = user.Name,
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
                DateOfBirth = DateTime.UtcNow,
                Sex = Sex.OTHER
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
                Name = user.Name,
                Role = user.Role.ToString()
            }
        };
    }

    public Task ResetPassword(string email)
    {
        throw new NotImplementedException("Reset password logic is not yet implemented.");
    }
}
