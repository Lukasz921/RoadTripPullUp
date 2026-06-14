using FluentAssertions;
using Moq;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Application.Services;
using Users.Application.Exceptions;
using Users.Core;
using Xunit;

namespace Users.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _authService = new AuthService(
            _userRepositoryMock.Object, 
            _passwordHasherMock.Object, 
            _jwtProviderMock.Object);
    }

    [Fact]
    public async Task ResetPassword_ShouldUpdatePassword_WhenUserExists()
    {
        // Arrange
        var email = "test@test.com";
        var user = new User { Email = email, PasswordHash = "OldHash" };
        _userRepositoryMock.Setup(r => r.FindByEmail(email)).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Hash("NewPassword")).Returns("NewHash");

        var dto = new ResetPasswordDTO { Email = email, NewPassword = "NewPassword" };

        // Act
        await _authService.ResetPassword(dto);

        // Assert
        user.PasswordHash.Should().Be("NewHash");
        _userRepositoryMock.Verify(r => r.Save(user), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ShouldThrowNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var email = "notfound@test.com";
        _userRepositoryMock.Setup(r => r.FindByEmail(email)).ReturnsAsync((User?)null);

        var dto = new ResetPasswordDTO { Email = email, NewPassword = "NewPassword" };

        // Act
        var act = () => _authService.ResetPassword(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
