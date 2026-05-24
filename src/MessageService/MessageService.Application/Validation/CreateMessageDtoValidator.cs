using FluentValidation;
using MessageService.Application.DTOs;

namespace MessageService.Application.Validation;

public class CreateMessageDtoValidator : AbstractValidator<CreateMessageDto>
{
    public CreateMessageDtoValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage("type is required");
        RuleFor(x => x.ConversationId).NotEmpty().WithMessage("conversationId is required");
    }
}
