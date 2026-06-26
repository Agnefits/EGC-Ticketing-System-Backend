using System;
using EGC_Ticketing_System.DTOs.Tickets;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Validation;
using FluentValidation;

namespace EGC_Ticketing_System.Validators
{
    public class CreateTicketDtoValidator : BaseValidator<CreateTicketDto>
    {
        public CreateTicketDtoValidator()
        {
            RuleFor(x => x.TeamId)
                .GreaterThan(0).WithMessage("TeamId must be greater than 0.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .Length(ValidationConstants.TicketTitleMinLength, ValidationConstants.TicketTitleMaxLength)
                .WithMessage($"Title must be between {ValidationConstants.TicketTitleMinLength} and {ValidationConstants.TicketTitleMaxLength} characters.")
                .Must(t => t == null || !string.IsNullOrWhiteSpace(t))
                .WithMessage("Title cannot be whitespace only.");

            RuleFor(x => x.Description)
                .MaximumLength(ValidationConstants.TicketDescriptionMaxLength)
                .WithMessage($"Description must not exceed {ValidationConstants.TicketDescriptionMaxLength} characters.");

            RuleFor(x => x.Deadline)
                .Must(d => !d.HasValue || d.Value >= DateTime.UtcNow)
                .WithMessage("Deadline must be UtcNow or later.");

            RuleFor(x => x.Priority)
                .Must(p => Enum.IsDefined(typeof(TicketPriority), p))
                .WithMessage("Priority is invalid.");
        }
    }
}
