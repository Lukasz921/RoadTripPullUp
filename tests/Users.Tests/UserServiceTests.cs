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
    private readonly Mock<IRatingRepository> _ratingRepositoryMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _ratingRepositoryMock = new Mock<IRatingRepository>();
        _userService = new UserService(_userRepositoryMock.Object, _ratingRepositoryMock.Object);
    }

    [Fact]
    public async Task AddRating_ShouldUpdateAverageRatingCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var raterId = Guid.NewGuid();
        var user = new User { Id = userId, AvgRating = 4.0, RatingsCount = 1 };
        var rater = new User { Id = raterId, IsBanned = false };

        _userRepositoryMock.Setup(r => r.FindById(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.FindById(raterId)).ReturnsAsync(rater);

        var dto = new AddRatingDTO
        {
            UserId = userId,
            RaterId = raterId,
            Value = 5,
            Comment = "Great driver!"
        };

        // Act
        await _userService.AddRating(dto);

        // Assert
        user.RatingsCount.Should().Be(2);
        user.AvgRating.Should().Be(4.5); // (4*1 + 5) / 2 = 4.5
        _ratingRepositoryMock.Verify(r => r.Add(It.IsAny<Rating>()), Times.Once);
        _userRepositoryMock.Verify(r => r.Save(user), Times.Once);
    }

    [Fact]
    public async Task AddRating_ShouldThrow_WhenRaterIsBanned()
    {
        // Arrange
        var raterId = Guid.NewGuid();
        var rater = new User { Id = raterId, IsBanned = true, BannedUntil = DateTime.UtcNow.AddDays(1) };
        _userRepositoryMock.Setup(r => r.FindById(raterId)).ReturnsAsync(rater);

        var dto = new AddRatingDTO { RaterId = raterId, Value = 5 };

        // Act
        var act = () => _userService.AddRating(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Banned users cannot give ratings.");
    }

    [Fact]
    public async Task DeleteRating_ShouldRecalculateAverageCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var raterId = Guid.NewGuid();
        var ratingId = Guid.NewGuid();
        var user = new User { Id = userId, AvgRating = 4.5, RatingsCount = 2 };
        var rating = new Rating { Id = ratingId, UserId = userId, RaterId = raterId, Value = 5 };

        _ratingRepositoryMock.Setup(r => r.GetById(ratingId)).ReturnsAsync(rating);
        _userRepositoryMock.Setup(r => r.FindById(userId)).ReturnsAsync(user);

        // Act
        await _userService.DeleteRating(ratingId, raterId);

        // Assert
        user.RatingsCount.Should().Be(1);
        user.AvgRating.Should().Be(4.0); // (4.5*2 - 5) / 1 = 4.0
        _ratingRepositoryMock.Verify(r => r.Delete(rating), Times.Once);
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
