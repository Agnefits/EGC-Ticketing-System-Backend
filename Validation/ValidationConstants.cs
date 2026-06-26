namespace EGC_Ticketing_System.Validation
{
    public static class ValidationConstants
    {
        public const int FullNameMinLength = 2;
        public const int FullNameMaxLength = 100;

        public const int UsernameMinLength = 3;
        public const int UsernameMaxLength = 50;

        public const int EmailMinLength = 5;
        public const int EmailMaxLength = 255;

        public const int PhoneNumberLength = 11;

        public const int JobTitleMinLength = 2;
        public const int JobTitleMaxLength = 100;

        public const int PasswordMinLength = 8;
        public const int PasswordMaxLength = 100;

        public const int SignatureUrlMaxLength = 500;

        public const int TeamNameMinLength = 2;
        public const int TeamNameMaxLength = 100;

        public const int TeamDescriptionMaxLength = 1000;

        public const int TicketTitleMinLength = 2;
        public const int TicketTitleMaxLength = 150;

        public const int TicketDescriptionMaxLength = 1000;
    }
}
