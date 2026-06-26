using EGC_Ticketing_System.DTOs.Profile;
using EGC_Ticketing_System.Validation;
using FluentValidation;

namespace EGC_Ticketing_System.Validators
{
    public class UpdateProfileDtoValidator : BaseValidator<UpdateProfileDto>
    {
        public UpdateProfileDtoValidator()
        {
            RuleFor(x => x.FullName).FullNameRule();
            RuleFor(x => x.Email).EmailRule();
            RuleFor(x => x.PhoneNumber).PhoneNumberRule();
            RuleFor(x => x.JobTitle).JobTitleRule();
            RuleFor(x => x.Signature).SignatureFileRule();
        }
    }
}
