using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Application.Exceptions;
using Users.Core;

namespace Users.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IComplaintRepository _complaintRepository;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IComplaintRepository complaintRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _complaintRepository = complaintRepository;
    }

    public async Task<UserResponseDTO> GetById(Guid id)
    {
        var user = await _userRepository.FindById(id) ;
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

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = _passwordHasher.Hash(dto.Password);
        }

        await _userRepository.Save(user);
    }

    public async Task UpdateUserRating(Guid userId, int rating)
    {
        var user = await _userRepository.FindById(userId);
        if (user == null) throw new NotFoundException("User not found.");

        double totalScore = (user.AvgRating * user.RatingsCount) + rating;
        user.RatingsCount++;
        user.AvgRating = totalScore / user.RatingsCount;

        await _userRepository.Save(user);
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

    public async Task FileComplaint(Guid complainerId, Guid tripId, FileComplaintDTO dto)
    {
        var complainedUser = await _userRepository.FindById(dto.ComplainedUserId);
        if (complainedUser == null) throw new NotFoundException("Complained user not found.");

        var complaint = new Complaint
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            ComplainerId = complainerId,
            ComplainedUserId = dto.ComplainedUserId,
            Reason = dto.Reason,
            CreatedAt = DateTime.UtcNow
        };

        await _complaintRepository.Save(complaint);
    }

    public async Task<PagedComplaintsDTO> GetAllComplaints(int page, int pageSize)
    {
        var (items, totalCount) = await _complaintRepository.GetAll(page, pageSize);

        return new PagedComplaintsDTO
        {
            Items = items.Select(c => new ComplaintResponseDTO
            {
                Id = c.Id,
                TripId = c.TripId,
                ComplainerId = c.ComplainerId,
                ComplainedUserId = c.ComplainedUserId,
                Reason = c.Reason,
                CreatedAt = c.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
