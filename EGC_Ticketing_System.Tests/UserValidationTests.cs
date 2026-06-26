using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using EGC_Ticketing_System.DTOs.Users;
using EGC_Ticketing_System.DTOs.Profile;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Validators;
using EGC_Ticketing_System.Services.Classes;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Validation;

namespace EGC_Ticketing_System.Tests
{
    public class UserValidationTests
    {
        private readonly CreateUserDtoValidator _createUserValidator;
        private readonly UpdateUserDtoValidator _updateUserValidator;
        private readonly UpdateProfileDtoValidator _updateProfileValidator;

        public UserValidationTests()
        {
            _createUserValidator = new CreateUserDtoValidator();
            _updateUserValidator = new UpdateUserDtoValidator();
            _updateProfileValidator = new UpdateProfileDtoValidator();
        }

        [Theory]
        [InlineData("John Doe")]
        [InlineData("Mostafa")]
        [InlineData("محمد أحمد")] // Arabic Unicode Name
        public void FullName_Valid_Passes(string name)
        {
            var dto = new CreateUserDto { FullName = name };
            var result = _createUserValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.FullName));
            Assert.Null(error);
        }

        [Theory]
        [InlineData(" John")] // Leading space
        [InlineData("John ")] // Trailing space
        [InlineData("John123")] // Digits
        [InlineData("John_Doe")] // Special characters
        [InlineData("A")] // Minimum 2 chars
        [InlineData("")]
        [InlineData("   ")]
        public void FullName_Invalid_Fails(string name)
        {
            var dto = new CreateUserDto { FullName = name };
            var result = _createUserValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.FullName));
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData("mostafa")]
        [InlineData("mostafa123")]
        [InlineData("M12")]
        public void Username_Valid_Passes(string username)
        {
            var dto = new CreateUserDto { Username = username };
            var result = _createUserValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.Username));
            Assert.Null(error);
        }

        [Theory]
        [InlineData("A")] // Too short
        [InlineData(" Mostafa")] // Leading space
        [InlineData("Mostafa$")] // Special character
        [InlineData("01040224756")] // Numeric only
        [InlineData("")]
        [InlineData("   ")]
        public void Username_Invalid_Fails(string username)
        {
            var dto = new CreateUserDto { Username = username };
            var result = _createUserValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.Username));
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData("mostafa@gmail.com")]
        [InlineData("mostafa@gmail.edu.eg")]
        [InlineData("mostafa@aitu.aun.edu.eg")]
        [InlineData("mostafa@aitu.edu.eg")]
        public void Email_Valid_Passes(string email)
        {
            var dto = new CreateUserDto { Email = email };
            var result = _createUserValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.Email));
            Assert.Null(error);
        }

        [Theory]
        [InlineData(".Mostafa@gmail.com")] // Leading dot
        [InlineData("Mostafa@gmail.com.")] // Trailing dot
        [InlineData("Mostafa@gmail..com")] // Consecutive dots
        [InlineData("Mostafa@gmail")] // Missing TLD
        [InlineData("Mostafa@.com")] // Missing domain part
        [InlineData("%Mostafa@gmail.com")] // Special character
        [InlineData("Mostafa@gmail.MOSTAFA")] // Invalid TLD (length > 6)
        [InlineData("")]
        [InlineData("   ")]
        public void Email_Invalid_Fails(string email)
        {
            var dto = new CreateUserDto { Email = email };
            var result = _createUserValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.Email));
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData("01012345678")]
        [InlineData("01112345678")]
        [InlineData("01212345678")]
        [InlineData("01512345678")]
        public void PhoneNumber_Valid_Passes(string phone)
        {
            var dto = new CreateUserDto { PhoneNumber = phone };
            var result = _createUserValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.PhoneNumber));
            Assert.Null(error);
        }

        [Theory]
        [InlineData("01312345678")] // Invalid prefix
        [InlineData("0101234567")] // 10 digits
        [InlineData("010123456789")] // 12 digits
        [InlineData("01012345abc")] // Letters
        [InlineData("010-1234567")] // Special character
        [InlineData("")]
        [InlineData("   ")]
        public void PhoneNumber_Invalid_Fails(string phone)
        {
            var dto = new CreateUserDto { PhoneNumber = phone };
            var result = _createUserValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.PhoneNumber));
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData((UserRole)999)]
        [InlineData((UserRole)(-1))]
        public void Role_Invalid_Fails(UserRole role)
        {
            var dto = new CreateUserDto { Role = role };
            var result = _createUserValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.Role));
            Assert.NotNull(error);
        }

        [Fact]
        public async Task Duplicate_Username_Throws_ValidationException()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(u => u.Users.ExistsByUsernameAsync("duplicate")).ReturnsAsync(true);

            var validationService = new ValidationService(mockUnitOfWork.Object);
            var dto = new CreateUserDto { Username = "duplicate", Email = "unique@test.com", PhoneNumber = "01012345678" };

            var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => validationService.ValidateUserCreateAsync(dto));
            Assert.Contains("Username", ex.Errors.Keys);
        }

        [Fact]
        public async Task Duplicate_Email_Throws_ValidationException()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(u => u.Users.ExistsByEmailAsync("duplicate@test.com")).ReturnsAsync(true);

            var validationService = new ValidationService(mockUnitOfWork.Object);
            var dto = new CreateUserDto { Username = "unique", Email = "duplicate@test.com", PhoneNumber = "01012345678" };

            var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => validationService.ValidateUserCreateAsync(dto));
            Assert.Contains("Email", ex.Errors.Keys);
        }

        [Fact]
        public async Task Duplicate_Phone_Throws_ValidationException()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(u => u.Users.ExistsByPhoneAsync("01012345678")).ReturnsAsync(true);

            var validationService = new ValidationService(mockUnitOfWork.Object);
            var dto = new CreateUserDto { Username = "unique", Email = "unique@test.com", PhoneNumber = "01012345678" };

            var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => validationService.ValidateUserCreateAsync(dto));
            Assert.Contains("PhoneNumber", ex.Errors.Keys);
        }

        [Fact]
        public async Task UpdateProfile_Duplicate_Email_Throws()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(u => u.Users.ExistsByEmailExceptAsync(1, "duplicate@test.com")).ReturnsAsync(true);

            var validationService = new ValidationService(mockUnitOfWork.Object);
            var dto = new UpdateProfileDto { FullName = "Name", Email = "duplicate@test.com", PhoneNumber = "01012345678", JobTitle = "Dev" };

            var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => validationService.ValidateProfileUpdateAsync(1, dto));
            Assert.Contains("Email", ex.Errors.Keys);
        }
    }
}
