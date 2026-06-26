using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace EGC_Ticketing_System.Validation
{
    public static class ValidationExtensions
    {
        public static IRuleBuilderOptions<T, string> FullNameRule<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("FullName is required.")
                .Length(ValidationConstants.FullNameMinLength, ValidationConstants.FullNameMaxLength)
                .WithMessage($"FullName must be between {ValidationConstants.FullNameMinLength} and {ValidationConstants.FullNameMaxLength} characters.")
                .Matches(RegexConstants.FullNameRegex)
                .WithMessage("FullName must contain letters only, allow spaces between words, no digits, no special characters, and must not start or end with space.");
        }

        public static IRuleBuilderOptions<T, string> UsernameRule<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Username is required.")
                .Length(ValidationConstants.UsernameMinLength, ValidationConstants.UsernameMaxLength)
                .WithMessage($"Username must be between {ValidationConstants.UsernameMinLength} and {ValidationConstants.UsernameMaxLength} characters.")
                .Matches(RegexConstants.UsernameRegex)
                .WithMessage("Username must be 3-50 characters, contain only letters and numbers, and not be numeric only.");
        }

        public static IRuleBuilderOptions<T, string?> EmailRule<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Email is required.")
                .Length(ValidationConstants.EmailMinLength, ValidationConstants.EmailMaxLength)
                .WithMessage($"Email must be between {ValidationConstants.EmailMinLength} and {ValidationConstants.EmailMaxLength} characters.")
                .Must(e => e == null || RegexConstants.IsValidEmail(e))
                .WithMessage("Email is invalid.");
        }

        public static IRuleBuilderOptions<T, string?> PhoneNumberRule<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("PhoneNumber is required.")
                .Length(ValidationConstants.PhoneNumberLength)
                .WithMessage($"PhoneNumber must be exactly {ValidationConstants.PhoneNumberLength} digits.")
                .Matches(RegexConstants.PhoneNumberRegex)
                .WithMessage("PhoneNumber must be a valid Egyptian mobile number (010, 011, 012, 015 followed by 8 digits).");
        }

        public static IRuleBuilderOptions<T, string?> JobTitleRule<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("JobTitle is required.")
                .Length(ValidationConstants.JobTitleMinLength, ValidationConstants.JobTitleMaxLength)
                .WithMessage($"JobTitle must be between {ValidationConstants.JobTitleMinLength} and {ValidationConstants.JobTitleMaxLength} characters.");
        }

        public static IRuleBuilderOptions<T, string?> SignatureUrlRule<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder
                .MaximumLength(ValidationConstants.SignatureUrlMaxLength)
                .WithMessage($"SignatureUrl must not exceed {ValidationConstants.SignatureUrlMaxLength} characters.");
        }

        public static IRuleBuilderOptions<T, IFormFile?> SignatureFileRule<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder)
        {
            return ruleBuilder
                .Must(file => file == null || file.Length <= 5 * 1024 * 1024)
                .WithMessage("Signature file must not exceed 5MB.")
                .Must(file => file == null || IsValidImageExtension(file.FileName))
                .WithMessage("Signature file must be an image (.jpg, .jpeg, .png).");
        }

        private static bool IsValidImageExtension(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png";
        }
    }
}
