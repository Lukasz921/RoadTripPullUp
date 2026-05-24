using FluentValidation;
using MessageService.DTOs;

namespace MessageService.Validation;

public class CreateConversationDtoValidator : AbstractValidator<CreateConversationDto>
{
    public CreateConversationDtoValidator()
    {
        RuleFor(x => x.Participants).NotNull().WithMessage("participants required").Must(p => p.Count >= 1).WithMessage("at least one participant required");
        RuleForEach(x => x.Participants).NotEmpty().WithMessage("participant id is required");
        RuleFor(x => x.Title).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Title));
    }
}

