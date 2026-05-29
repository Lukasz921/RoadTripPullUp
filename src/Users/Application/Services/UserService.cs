using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Core;

namespace Users.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponseDTO> GetById(Guid id)
    {
        var user = await _userRepository.FindById(id);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        return new UserResponseDTO
        {
            Id = user.Id,
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            Sex = user.Sex.ToString()
        };
    }

    public async Task Update(Guid id, UpdateUserDTO dto)
    {
        var user = await _userRepository.FindById(id);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        if (dto.Name != null) user.Name = dto.Name;
        if (dto.Surname != null) user.Surname = dto.Surname;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
        if (dto.DateOfBirth != null) user.DateOfBirth = dto.DateOfBirth.Value;
        if (dto.Sex != null)
        {
            if (Enum.TryParse<Sex>(dto.Sex, true, out var sex))
            {
                user.Sex = sex;
            }
            else
            {
                throw new Exception("Invalid value for Sex.");
            }
        }

        await _userRepository.Save(user);
    }
}
