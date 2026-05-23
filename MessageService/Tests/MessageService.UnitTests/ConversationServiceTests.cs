using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MessageService.Repositories;
using MessageService.Services;
using Moq;
using Xunit;

namespace MessageService.UnitTests;

public class ConversationServiceTests
{
    [Fact]
    public async Task CreateConversation_Should_RequireParticipants()
    {
        var convRepo = new Mock<IConversationRepository>();
        var userRepo = new Mock<IUserRepository>();
        var svc = new ConversationService(convRepo.Object, userRepo.Object);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await svc.CreateConversationAsync(new DTOs.CreateConversationDto { Participants = new List<Guid>() }, Guid.NewGuid());
        });
    }

    [Fact]
    public async Task GetForUser_ShouldMapToDto()
    {
        var convRepo = new Mock<IConversationRepository>();
        var userRepo = new Mock<IUserRepository>();

        var userId = Guid.NewGuid();
        var conv = new MessageService.Models.Conversation
        {
            Id = Guid.NewGuid(),
            IsGroup = false,
            Title = "",
            Members = new List<MessageService.Models.ConversationMember>
            {
                new MessageService.Models.ConversationMember { UserId = userId }
            }
        };

        convRepo.Setup(r => r.GetForUserAsync(userId, 0, 20)).ReturnsAsync(new List<MessageService.Models.Conversation> { conv });

        var svc = new ConversationService(convRepo.Object, userRepo.Object);
        var list = await svc.GetForUserAsync(userId, 0, 20);
        list.Should().HaveCount(1);
        var dto = list.First();
        dto.Participants.Should().Contain(userId);
    }
}

