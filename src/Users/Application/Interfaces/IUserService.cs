using Users.Application.DTOs;

namespace Users.Application.Interfaces;

public interface IUserService
{
    Task<UserResponseDTO> GetById(Guid id);
    Task Update(Guid id, UpdateUserDTO dto);
    Task AddRating(AddRatingDTO dto);
    Task<RatingResponseDTO> GetRating(Guid ratingId);
    Task<List<RatingResponseDTO>> GetUserRatings(Guid userId);
    Task DeleteRating(Guid ratingId, Guid currentUserId);
    Task<UserIntegrationDTO> GetUserIntegrationData(Guid id);
}
