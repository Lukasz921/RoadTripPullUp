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
    public async Task GetById_ShouldReturnUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDto = new UserResponseDTO { Id = userId, Name = "Test" };
        _userServiceMock.Setup(s => s.GetById(userId)).ReturnsAsync(userDto);

        // Act
        var result = await _controller.GetById(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(userDto);
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
