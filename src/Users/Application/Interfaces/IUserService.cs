using Users.Application.DTOs;

namespace Users.Application.Interfaces;

public interface IUserService
{
    Task<UserResponseDTO> GetById(Guid id);
    Task Update(Guid id, UpdateUserDTO dto);
    Task AddRating(AddRatingDTO dto);
    Task<UserIntegrationDTO> GetUserIntegrationData(Guid id);
}
