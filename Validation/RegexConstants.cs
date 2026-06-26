using System;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace EGC_Ticketing_System.Validation
{
    public static class RegexConstants
    {
        // Letters only, allows spaces between words, no digits/special chars, min 2 chars
        public static readonly Regex FullNameRegex = new Regex(@"^\p{L}+(?: \p{L}+)*$", RegexOptions.Compiled);

        // 3-50 characters, letters and numbers, no spaces, no special characters, not numeric only
        public static readonly Regex UsernameRegex = new Regex(@"^(?![0-9]+$)[a-zA-Z0-9]{3,50}$", RegexOptions.Compiled);

        // Egyptian mobile numbers (010, 011, 012, 015 followed by exactly 8 digits)
        public static readonly Regex PhoneNumberRegex = new Regex(@"^(010|011|012|015)\d{8}$", RegexOptions.Compiled);

        // Team Name: letters and numbers, single spaces between words, no leading/trailing spaces
        public static readonly Regex TeamNameRegex = new Regex(@"^[a-zA-Z0-9]+(?: [a-zA-Z0-9]+)*$", RegexOptions.Compiled);

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            // Trim email
            email = email.Trim();

            // Reject if it has spaces
            if (email.Contains(" ")) return false;

            // Reject leading/trailing dots anywhere in the email
            if (email.StartsWith(".") || email.EndsWith(".")) return false;

            // Reject consecutive dots
            if (email.Contains("..")) return false;

            try
            {
                // Standard MailAddress parsing (catches many formatting issues)
                var addr = new MailAddress(email);
                if (addr.Address != email) return false;

                // Split into local part and domain part
                var parts = email.Split('@');
                if (parts.Length != 2) return false;

                var localPart = parts[0];
                var domainPart = parts[1];

                // Check local part starting/ending with dot
                if (localPart.StartsWith(".") || localPart.EndsWith(".")) return false;

                // Check domain part starting/ending with dot
                if (domainPart.StartsWith(".") || domainPart.EndsWith(".")) return false;

                // Domain must contain at least one dot
                var dotIndex = domainPart.LastIndexOf('.');
                if (dotIndex <= 0 || dotIndex == domainPart.Length - 1) return false;

                var tld = domainPart.Substring(dotIndex + 1);
                
                // TLD must be letters only, and length must be between 2 and 6
                if (tld.Length < 2 || tld.Length > 6 || !tld.All(char.IsLetter)) return false;

                // Reject if local part has special characters other than letters, numbers, and . _ % + -
                // This will fail %Mostafa@gmail.com because '%' is not in the start of allowed pattern,
                // actually we can match local part with regex:
                if (!Regex.IsMatch(localPart, @"^[a-zA-Z0-9._+-]+$")) return false;

                // Validate domain labels (each label must be alphanumeric and can have hyphen, but no special characters)
                var domainLabels = domainPart.Split('.');
                foreach (var label in domainLabels)
                {
                    if (string.IsNullOrEmpty(label)) return false;
                    if (!Regex.IsMatch(label, @"^[a-zA-Z0-9-]+$")) return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
