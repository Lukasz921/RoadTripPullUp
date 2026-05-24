using FluentValidation;
using MessageService.Application.DTOs;

namespace MessageService.Application.Validation;

public class CreateMessageDtoValidator : AbstractValidator<CreateMessageDto>
{
    public CreateMessageDtoValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage("type is required");
        RuleFor(x => x.Payload).NotNull().WithMessage("payload is required");
        RuleFor(x => x.Payload).Must(p => p != null).WithMessage("payload must be an object");
    }
}

