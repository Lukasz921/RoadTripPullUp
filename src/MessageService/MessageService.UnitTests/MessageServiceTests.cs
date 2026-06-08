using System.Text.Json.Nodes;
using FluentAssertions;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using MessageService.Core.Models;
using MessageService.Core.RepositoryInterfaces;
using Moq;

namespace MessageService.UnitTests;

public class MessageServiceTests
{
    [Fact]
    public async Task CreateMessage_ShouldPersist_And_Publish()
    {
        // Arrange
        var messageRepo = new Mock<IMessageRepository>();
        var convRepo = new Mock<IConversationRepository>();
        var notifier = new Mock<INotificationService>();
        var clock = new Mock<IClockService>();

        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();

        var conv = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Direct,
            Members = [new ConversationMember { ConversationId = conversationId, UserId = senderId }]
        };

        convRepo.Setup(r => r.GetByIdAsync(conversationId)).ReturnsAsync(conv);
        messageRepo.Setup(r => r.CreateAsync(It.IsAny<Message>())).ReturnsAsync((Message m) =>
        {
            m.Id = Guid.NewGuid();
            return m;
        });

        var svc = new Application.Services.MessageService(messageRepo.Object, convRepo.Object, notifier.Object, clock.Object);

        var dto = new CreateMessageDto
        {
            ConversationId = conversationId,
            Type = MessageType.Text,
            Payload = new JsonObject { ["text"] = "hello" }
        };

        // Act
        var id = await svc.CreateMessageAsync(dto, senderId);

        // Assert
        id.Should().NotBe(Guid.Empty);
        messageRepo.Verify(r => r.CreateAsync(It.Is<Message>(m => m.ConversationId == conversationId && m.SenderId == senderId && m.Type == MessageType.Text)), Times.Once);
        notifier.Verify(n => n.PublishMessageCreatedAsync(It.IsAny<Message>()), Times.Once);
    }

    [Fact]
    public async Task MarkMessagesRead_ShouldCallRepository_And_Publish()
    {
        // Arrange
        var messageRepo = new Mock<IMessageRepository>();
        var convRepo = new Mock<IConversationRepository>();
        var notifier = new Mock<INotificationService>();
        var clock = new Mock<IClockService>();

        var conversationId = Guid.NewGuid();
        var readerId = Guid.NewGuid();
        var msg1 = Guid.NewGuid();
        var msg2 = Guid.NewGuid();

        messageRepo.Setup(r => r.MarkMessagesReadAsync(conversationId, It.IsAny<IEnumerable<Guid>>(), readerId, It.IsAny<DateTime>())).Returns(Task.CompletedTask).Verifiable();

        var svc = new Application.Services.MessageService(messageRepo.Object, convRepo.Object, notifier.Object, clock.Object);

        // Act
        await svc.MarkMessagesReadAsync(conversationId, [msg1, msg2], readerId, DateTime.UtcNow);

        // Assert
        messageRepo.Verify();
        notifier.Verify(n => n.PublishMessagesReadAsync(conversationId, It.IsAny<IEnumerable<Guid>>(), readerId, It.IsAny<DateTime>()), Times.Once);
    }
}

