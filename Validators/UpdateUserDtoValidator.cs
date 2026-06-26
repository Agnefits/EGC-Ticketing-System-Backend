using System;
using EGC_Ticketing_System.DTOs.Users;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Validation;
using FluentValidation;

namespace EGC_Ticketing_System.Validators
{
    public class UpdateUserDtoValidator : BaseValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.FullName).FullNameRule();
            RuleFor(x => x.Email).EmailRule();
            RuleFor(x => x.PhoneNumber).PhoneNumberRule();
            RuleFor(x => x.JobTitle).JobTitleRule();

            RuleFor(x => x.Role)
                .Must(role => Enum.IsDefined(typeof(UserRole), role))
                .WithMessage("Role is invalid.");

            RuleFor(x => x.Status)
                .Must(status => Enum.IsDefined(typeof(UserStatus), status))
                .WithMessage("Status is invalid.");

            RuleFor(x => x.Signature).SignatureFileRule();
        }
    }
}
