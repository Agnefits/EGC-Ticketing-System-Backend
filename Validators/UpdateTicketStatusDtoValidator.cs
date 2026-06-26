using System;
using EGC_Ticketing_System.DTOs.Tickets;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Validation;
using FluentValidation;

namespace EGC_Ticketing_System.Validators
{
    public class UpdateTicketStatusDtoValidator : BaseValidator<ChangeTicketStatusDto>
    {
        public UpdateTicketStatusDtoValidator()
        {
            RuleFor(x => x.Status)
                .Must(status => Enum.IsDefined(typeof(TicketStatus), status))
                .WithMessage("Status is invalid.");
        }
    }
}
