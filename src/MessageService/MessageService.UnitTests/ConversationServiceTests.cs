using FluentAssertions;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using MessageService.Core.Models;
using MessageService.Core.RepositoryInterfaces;
using Moq;

namespace MessageService.UnitTests;

public class ConversationServiceTests
{
    [Fact]
    public async Task CreateConversation_Should_RequireParticipants()
    {
        var convRepo = new Mock<IConversationRepository>();
        var userRepo = new Mock<IUserRepository>();
        var clock = new Mock<IClockService>();
        var svc = new ConversationService(convRepo.Object, userRepo.Object, clock.Object);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await svc.CreateConversationAsync(new CreateConversationDto { Participants = [] }, Guid.NewGuid());
        });
    }

    [Fact]
    public async Task GetForUser_ShouldMapToDto()
    {
        var convRepo = new Mock<IConversationRepository>();
        var userRepo = new Mock<IUserRepository>();
        var clock = new Mock<IClockService>();

        var userId = Guid.NewGuid();
        var conv = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = ConversationType.Direct,
            Title = "",
            Members = [new ConversationMember { UserId = userId }]
        };

        convRepo.Setup(r => r.GetForUserAsync(userId, 0, 20)).ReturnsAsync([conv]);

        var svc = new ConversationService(convRepo.Object, userRepo.Object, clock.Object);
        var list = await svc.GetForUserAsync(userId, 0, 20);
        var conversationDtos = list.ToList();
        conversationDtos.Should().HaveCount(1);
        var dto = conversationDtos.First();
        dto.Participants.Should().Contain(userId);
    }
}

