using MessageService.Core.Exceptions;
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
        var clock = new Mock<IClockService>();
        var svc = new ConversationService(convRepo.Object, clock.Object);

        await Assert.ThrowsAsync<InvalidParametersException>(async () =>
        {
            await svc.CreateConversationAsync(new CreateConversationDto { Participants = [] }, Guid.NewGuid());
        });
    }

    [Fact]
    public async Task GetForUser_ShouldMapToDto()
    {
        var convRepo = new Mock<IConversationRepository>();
        var clock = new Mock<IClockService>();

        var userId = Guid.NewGuid();
        var conv = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = "direct",
            Title = "",
            Members = [new ConversationMember { UserId = userId }]
        };

        convRepo.Setup(r => r.GetForUserWithLastMessageAsync(userId, 0, 20))
            .ReturnsAsync([(conv, null)]);

        var svc = new ConversationService(convRepo.Object, clock.Object);
        var list = await svc.GetForUserAsync(userId, 0, 20);
        var conversationDtos = list.ToList();
        conversationDtos.Should().HaveCount(1);
        var dto = conversationDtos.First();
        dto.Participants.Should().Contain(userId);
    }
}


