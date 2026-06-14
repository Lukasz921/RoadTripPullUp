using Users.Application.DTOs;
using Users.Core;

namespace Users.Application.Interfaces;

public interface IUserService
{
    Task<UserResponseDTO> GetById(Guid id);
    Task Update(Guid id, UpdateUserDTO dto);
    Task UpdateUserRating(Guid userId, int rating);
    Task Ban(Guid userId, BanUserDTO dto);
    Task Unban(Guid userId);
    Task ChangeRole(Guid userId, UserRole newRole);
    Task<UserIntegrationDTO> GetUserIntegrationData(Guid id);
    Task FileComplaint(Guid complainerId, Guid tripId, FileComplaintDTO dto);
    Task<ComplaintResponseDTO> GetComplaintById(Guid id);
}
