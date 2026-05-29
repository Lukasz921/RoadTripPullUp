using Users.Core;
using Google.Apis.Auth;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Application.Exceptions;

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

    public async Task Register(RegisterDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new UserValidationException("Email is required.");
        
        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new UserValidationException("Password is required.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new UserValidationException("Name is required.");

        if (string.IsNullOrWhiteSpace(dto.Surname))
            throw new UserValidationException("Surname is required.");

        var existingUser = await _userRepository.FindByEmail(dto.Email);
        if (existingUser != null)
        {
            throw new UserAlreadyExistsException(dto.Email);
        }

        if (!Enum.TryParse<Sex>(dto.Sex, true, out var sex))
        {
            sex = Sex.OTHER;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Surname = dto.Surname,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = UserRole.REGULAR_USER,
            PhoneNumber = dto.PhoneNumber,
            DateOfBirth = dto.DateOfBirth,
            Sex = sex
        };

        try
        {
            await _userRepository.Save(user);
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while creating the user account. Please try again later.", ex);
        }
    }

    public async Task<AuthResponseDTO> Login(LoginDTO dto)
    {
        var user = await _userRepository.FindByEmail(dto.Email);

        if (user == null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
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
                PhoneNumber = string.Empty,
                DateOfBirth = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Sex = Sex.OTHER
            };
            try
            {
                await _userRepository.Save(user);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while creating the user account via Google.", ex);
            }
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
