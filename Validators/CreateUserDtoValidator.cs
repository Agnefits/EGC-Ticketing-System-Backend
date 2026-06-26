using System;
using EGC_Ticketing_System.DTOs.Users;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Validation;
using FluentValidation;

namespace EGC_Ticketing_System.Validators
{
    public class CreateUserDtoValidator : BaseValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.FullName).FullNameRule();
            RuleFor(x => x.Username).UsernameRule();
            RuleFor(x => x.Email).EmailRule();
            RuleFor(x => x.PhoneNumber).PhoneNumberRule();
            
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .Length(ValidationConstants.PasswordMinLength, ValidationConstants.PasswordMaxLength)
                .WithMessage($"Password must be between {ValidationConstants.PasswordMinLength} and {ValidationConstants.PasswordMaxLength} characters.");

            RuleFor(x => x.JobTitle).JobTitleRule();
            
            RuleFor(x => x.Role)
                .Must(role => Enum.IsDefined(typeof(UserRole), role))
                .WithMessage("Role is invalid.");

            RuleFor(x => x.Signature).SignatureFileRule();
        }
    }
}
