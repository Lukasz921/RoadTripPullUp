using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Application.Exceptions;
using Users.Core;

namespace Users.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRatingRepository _ratingRepository;

    public UserService(IUserRepository userRepository, IRatingRepository ratingRepository)
    {
        _userRepository = userRepository;
        _ratingRepository = ratingRepository;
    }

    public async Task<UserResponseDTO> GetById(Guid id)
    {
        var user = await _userRepository.FindById(id);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        return new UserResponseDTO
        {
            Id = user.Id,
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            Sex = user.Sex.ToString(),
            AvgRating = user.AvgRating,
            RatingsCount = user.RatingsCount,
            IsBanned = user.IsBanned,
            BanReason = user.BanReason,
            BannedUntil = user.BannedUntil
        };
    }

    public async Task Update(Guid id, UpdateUserDTO dto)
    {
        var user = await _userRepository.FindById(id);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        if (user.IsBanned && (user.BannedUntil == null || user.BannedUntil > DateTime.UtcNow))
        {
            throw new Exception("Banned users cannot update their profiles.");
        }

        if (dto.Name != null) user.Name = dto.Name;
        if (dto.Surname != null) user.Surname = dto.Surname;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
        if (dto.DateOfBirth != null) user.DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc);
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

    public async Task AddRating(AddRatingDTO dto)
    {
        var rater = await _userRepository.FindById(dto.RaterId);
        if (rater != null && rater.IsBanned && (rater.BannedUntil == null || rater.BannedUntil > DateTime.UtcNow))
        {
            throw new Exception("Banned users cannot give ratings.");
        }

        if (dto.Value < 1 || dto.Value > 5)
        {
            throw new Exception("Rating must be between 1 and 5.");
        }

        var user = await _userRepository.FindById(dto.UserId);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var rating = new Rating
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            RaterId = dto.RaterId,
            Value = dto.Value,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        await _ratingRepository.Add(rating);

        // Update average rating
        double totalScore = (user.AvgRating * user.RatingsCount) + dto.Value;
        user.RatingsCount++;
        user.AvgRating = totalScore / user.RatingsCount;

        await _userRepository.Save(user);
    }

    public async Task<List<RatingResponseDTO>> GetUserRatings(Guid userId)
    {
        var ratings = await _ratingRepository.GetByUserId(userId);
        return ratings.Select(r => new RatingResponseDTO
        {
            Id = r.Id,
            RaterId = r.RaterId,
            RaterName = r.Rater != null ? $"{r.Rater.Name} {r.Rater.Surname}" : "Unknown",
            Value = r.Value,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<RatingResponseDTO> GetRating(Guid ratingId)
    {
        var r = await _ratingRepository.GetById(ratingId);
        if (r == null) throw new Exception("Rating not found.");

        return new RatingResponseDTO
        {
            Id = r.Id,
            RaterId = r.RaterId,
            RaterName = r.Rater != null ? $"{r.Rater.Name} {r.Rater.Surname}" : "Unknown",
            Value = r.Value,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        };
    }

    public async Task DeleteRating(Guid ratingId, Guid currentUserId)
    {
        var rating = await _ratingRepository.GetById(ratingId);
        if (rating == null) throw new Exception("Rating not found.");

        if (rating.RaterId != currentUserId)
        {
            throw new Exception("You can only delete your own ratings.");
        }

        var user = await _userRepository.FindById(rating.UserId);
        if (user != null)
        {
            // Recalculate average rating
            double totalScore = (user.AvgRating * user.RatingsCount) - rating.Value;
            user.RatingsCount--;
            user.AvgRating = user.RatingsCount > 0 ? totalScore / user.RatingsCount : 0;
            
            await _userRepository.Save(user);
        }

        await _ratingRepository.Delete(rating);
    }

    public async Task Ban(Guid userId, BanUserDTO dto)
    {
        var user = await _userRepository.FindById(userId);
        if (user == null) throw new NotFoundException("User not found.");

        user.IsBanned = true;
        user.BanReason = dto.Reason;
        user.BannedUntil = dto.Until.HasValue ? DateTime.SpecifyKind(dto.Until.Value, DateTimeKind.Utc) : null;

        await _userRepository.Save(user);
    }

    public async Task Unban(Guid userId)
    {
        var user = await _userRepository.FindById(userId);
        if (user == null) throw new NotFoundException("User not found.");

        user.IsBanned = false;
        user.BanReason = null;
        user.BannedUntil = null;

        await _userRepository.Save(user);
    }

    public async Task ChangeRole(Guid userId, UserRole newRole)
    {
        var user = await _userRepository.FindById(userId);
        if (user == null) throw new NotFoundException("User not found.");

        user.Role = newRole;
        await _userRepository.Save(user);
    }

    public async Task<UserIntegrationDTO> GetUserIntegrationData(Guid id)
    {
        var user = await _userRepository.FindById(id);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var isBanned = user.IsBanned && (user.BannedUntil == null || user.BannedUntil > DateTime.UtcNow);

        var today = DateTime.UtcNow.Date;
        var age = today.Year - user.DateOfBirth.Year;
        if (user.DateOfBirth.Date > today.AddYears(-age)) age--;

        var isAdult = age >= 18;

        return new UserIntegrationDTO
        {
            Id = user.Id,
            FullName = $"{user.Name} {user.Surname}",
            Email = user.Email,
            Trip = new TripIntegrationData
            {
                IsAdult = isAdult,
                CanCreateTrip = isAdult && !isBanned
            }
        };
    }
}
