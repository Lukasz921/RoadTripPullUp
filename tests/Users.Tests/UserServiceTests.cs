using FluentAssertions;
using Moq;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Application.Services;
using Users.Core;
using Xunit;

namespace Users.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IComplaintRepository> _complaintRepositoryMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _complaintRepositoryMock = new Mock<IComplaintRepository>();
        _userService = new UserService(_userRepositoryMock.Object, _passwordHasherMock.Object, _complaintRepositoryMock.Object);
    }

    [Fact]
    public async Task FileComplaint_ShouldSaveComplaint_WhenDataIsValid()
    {
        // Arrange
        var complainerId = Guid.NewGuid();
        var complainedUserId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var dto = new FileComplaintDTO { ComplainedUserId = complainedUserId, Reason = "Bad behavior" };

        _userRepositoryMock.Setup(r => r.FindById(complainedUserId)).ReturnsAsync(new User { Id = complainedUserId });

        // Act
        await _userService.FileComplaint(complainerId, tripId, dto);

        // Assert
        _complaintRepositoryMock.Verify(r => r.Save(It.Is<Complaint>(c => 
            c.ComplainerId == complainerId && 
            c.ComplainedUserId == complainedUserId && 
            c.TripId == tripId && 
            c.Reason == "Bad behavior")), Times.Once);
    }

    [Fact]
    public async Task GetComplaintById_ShouldReturnComplaint_WhenExists()
    {
        // Arrange
        var complaintId = Guid.NewGuid();
        var complaint = new Complaint
        {
            Id = complaintId,
            TripId = Guid.NewGuid(),
            ComplainerId = Guid.NewGuid(),
            ComplainedUserId = Guid.NewGuid(),
            Reason = "Test",
            CreatedAt = DateTime.UtcNow
        };

        _complaintRepositoryMock.Setup(r => r.FindById(complaintId)).ReturnsAsync(complaint);

        // Act
        var result = await _userService.GetComplaintById(complaintId);

        // Assert
        result.Id.Should().Be(complaintId);
        result.Reason.Should().Be("Test");
    }

    [Fact]
    public async Task UpdateUserRating_ShouldUpdateAverageRatingCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, AvgRating = 4.0, RatingsCount = 1 };

        _userRepositoryMock.Setup(r => r.FindById(userId)).ReturnsAsync(user);

        // Act
        await _userService.UpdateUserRating(userId, 5);

        // Assert
        user.RatingsCount.Should().Be(2);
        user.AvgRating.Should().Be(4.5); // (4*1 + 5) / 2 = 4.5
        _userRepositoryMock.Verify(r => r.Save(user), Times.Once);
    }

    [Fact]
    public async Task Ban_ShouldUpdateUserStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, IsBanned = false };
        _userRepositoryMock.Setup(r => r.FindById(userId)).ReturnsAsync(user);

        var banDto = new BanUserDTO { Reason = "Spam", Until = DateTime.UtcNow.AddDays(7) };

        // Act
        await _userService.Ban(userId, banDto);

        // Assert
        user.IsBanned.Should().BeTrue();
        user.BanReason.Should().Be("Spam");
        user.BannedUntil.Should().Be(banDto.Until);
        _userRepositoryMock.Verify(r => r.Save(user), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldUpdateFieldsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User 
        { 
            Id = userId, 
            Name = "OldName", 
            Surname = "OldSurname",
            PhoneNumber = "123",
            DateOfBirth = new DateTime(1990, 1, 1),
            Sex = Sex.OTHER
        };
        _userRepositoryMock.Setup(r => r.FindById(userId)).ReturnsAsync(user);

        var dto = new UpdateUserDTO 
        { 
            Name = "NewName", 
            Surname = "NewSurname",
            PhoneNumber = "456",
            DateOfBirth = new DateTime(1991, 2, 2),
            Sex = "MALE"
        };

        // Act
        await _userService.Update(userId, dto);

        // Assert
        user.Name.Should().Be("NewName");
        user.Surname.Should().Be("NewSurname");
        user.PhoneNumber.Should().Be("456");
        user.DateOfBirth.Should().Be(new DateTime(1991, 2, 2));
        user.Sex.Should().Be(Sex.MALE);
        _userRepositoryMock.Verify(r => r.Save(user), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldUpdatePassword_WhenProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, PasswordHash = "OldHash" };
        _userRepositoryMock.Setup(r => r.FindById(userId)).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Hash("NewPassword")).Returns("NewHash");

        var dto = new UpdateUserDTO { Password = "NewPassword" };

        // Act
        await _userService.Update(userId, dto);

        // Assert
        user.PasswordHash.Should().Be("NewHash");
        _userRepositoryMock.Verify(r => r.Save(user), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldThrow_WhenUserIsBanned()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, IsBanned = true, BannedUntil = DateTime.UtcNow.AddDays(1) };
        _userRepositoryMock.Setup(r => r.FindById(userId)).ReturnsAsync(user);

        // Act
        var act = () => _userService.Update(userId, new UpdateUserDTO { Name = "New Name" });

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Banned users cannot update their profiles.");
    }

    [Fact]
    public async Task GetUserIntegrationData_ShouldBlockTripCreation_WhenBanned()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User 
        { 
            Id = userId, 
            IsBanned = true, 
            BannedUntil = DateTime.UtcNow.AddDays(1),
            DateOfBirth = DateTime.UtcNow.AddYears(-20),
            Name = "John",
            Surname = "Doe"
        };
        _userRepositoryMock.Setup(r => r.FindById(userId)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserIntegrationData(userId);

        // Assert
        result.Trip.CanCreateTrip.Should().BeFalse();
    }
    
    [Fact]
    public async Task GetUserIntegrationData_ShouldReturnIsAdultFalse_WhenUnderage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User 
        { 
            Id = userId, 
            IsBanned = false, 
            DateOfBirth = DateTime.UtcNow.AddYears(-17),
            Name = "Young",
            Surname = "User"
        };
        _userRepositoryMock.Setup(r => r.FindById(userId)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserIntegrationData(userId);

        // Assert
        result.Trip.IsAdult.Should().BeFalse();
        result.Trip.CanCreateTrip.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeRole_ShouldUpdateRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Role = UserRole.REGULAR_USER };
        _userRepositoryMock.Setup(r => r.FindById(userId)).ReturnsAsync(user);

        // Act
        await _userService.ChangeRole(userId, UserRole.ADMIN);

        // Assert
        user.Role.Should().Be(UserRole.ADMIN);
        _userRepositoryMock.Verify(r => r.Save(user), Times.Once);
    }
}
