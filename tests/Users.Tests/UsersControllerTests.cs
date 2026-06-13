using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Users.API.Controllers;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Core;
using Xunit;

namespace Users.Tests;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UsersController(_userServiceMock.Object);
        
        // Mock User context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "ADMIN")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task RateUser_ShouldCallService()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        int value = 5;
        string comment = "Good";

        // Act
        var result = await _controller.RateUser(targetId, value, comment);

        // Assert
        result.Should().BeOfType<OkResult>();
        _userServiceMock.Verify(s => s.AddRating(It.Is<AddRatingDTO>(dto => 
            dto.UserId == targetId && dto.Value == value && dto.Comment == comment)), Times.Once);
    }

    [Fact]
    public async Task Ban_ShouldCallService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new BanUserDTO { Reason = "Test" };

        // Act
        var result = await _controller.Ban(userId, dto);

        // Assert
        result.Should().BeOfType<OkResult>();
        _userServiceMock.Verify(s => s.Ban(userId, dto), Times.Once);
    }

    [Fact]
    public async Task Unban_ShouldCallService()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _controller.Unban(userId);

        // Assert
        result.Should().BeOfType<OkResult>();
        _userServiceMock.Verify(s => s.Unban(userId), Times.Once);
    }

    [Fact]
    public async Task ChangeRole_ShouldCallService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var role = UserRole.ADMIN;

        // Act
        var result = await _controller.ChangeRole(userId, role);

        // Assert
        result.Should().BeOfType<OkResult>();
        _userServiceMock.Verify(s => s.ChangeRole(userId, role), Times.Once);
    }
}
