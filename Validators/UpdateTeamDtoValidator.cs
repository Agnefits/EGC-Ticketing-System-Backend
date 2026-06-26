using System;
using EGC_Ticketing_System.DTOs.Teams;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Validation;
using FluentValidation;

namespace EGC_Ticketing_System.Validators
{
    public class UpdateTeamDtoValidator : BaseValidator<UpdateTeamDto>
    {
        public UpdateTeamDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .Length(ValidationConstants.TeamNameMinLength, ValidationConstants.TeamNameMaxLength)
                .WithMessage($"Name must be between {ValidationConstants.TeamNameMinLength} and {ValidationConstants.TeamNameMaxLength} characters.")
                .Matches(RegexConstants.TeamNameRegex)
                .WithMessage("Team Name must contain only letters, numbers, and single spaces between words (no leading/trailing spaces or special characters).");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(ValidationConstants.TeamDescriptionMaxLength)
                .WithMessage($"Description must not exceed {ValidationConstants.TeamDescriptionMaxLength} characters.")
                .Must(d => d == null || !string.IsNullOrWhiteSpace(d))
                .WithMessage("Description cannot be whitespace only.");

            RuleFor(x => x.Status)
                .Must(status => Enum.IsDefined(typeof(TeamStatus), status))
                .WithMessage("Status is invalid.");
        }
    }
}
