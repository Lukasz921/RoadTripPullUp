using FluentValidation;
using MessageService.Application.DTOs;

namespace MessageService.Application.Validation;

public class ReadMessagesRequestValidator : AbstractValidator<ReadMessagesRequest>
{
    public ReadMessagesRequestValidator()
    {
        RuleFor(x => x.MessageIds).NotNull().WithMessage("messageIds required");
        RuleFor(x => x.MessageIds).Must(list => list != null && list.Count > 0).WithMessage("at least one messageId required");
    }
}

