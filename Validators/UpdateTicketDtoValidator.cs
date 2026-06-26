using System;
using EGC_Ticketing_System.DTOs.Tickets;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Validation;
using FluentValidation;

namespace EGC_Ticketing_System.Validators
{
    public class UpdateTicketDtoValidator : BaseValidator<UpdateTicketDto>
    {
        public UpdateTicketDtoValidator()
        {
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

            RuleFor(x => x.Status)
                .Must(status => Enum.IsDefined(typeof(TicketStatus), status))
                .WithMessage("Status is invalid.");

            RuleFor(x => x.Priority)
                .Must(priority => Enum.IsDefined(typeof(TicketPriority), priority))
                .WithMessage("Priority is invalid.");
        }
    }
}
