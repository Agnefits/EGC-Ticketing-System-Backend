using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EGC_Ticketing_System.DTOs.Profile;
using EGC_Ticketing_System.DTOs.Users;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Services.Interfaces;

namespace EGC_Ticketing_System.Controllers
{
    [Authorize]
    public class ProfileController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidationService _validationService;

        public ProfileController(IUnitOfWork unitOfWork, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || user.Status == UserStatus.Deleted)
            {
                return NotFound(new { message = "User not found." });
            }

            var response = new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                JobTitle = user.JobTitle,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt,
                SignatureUrl = user.SignatureUrl,
                CreatedById = user.CreatedById
            };

            return Ok(response);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || user.Status == UserStatus.Deleted)
            {
                return NotFound(new { message = "User not found." });
            }

            await _validationService.ValidateProfileUpdateAsync(userId, dto);

            var signaturePath = await SaveSignatureFileAsync(dto.Signature);

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.JobTitle = dto.JobTitle;
            
            if (signaturePath != null)
            {
                user.SignatureUrl = signaturePath;
            }

            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "UpdateProfile", "User", user.Id.ToString(), $"User {user.Username} updated their profile info.");

            return Ok(new { message = "Profile updated successfully." });
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || user.Status == UserStatus.Deleted)
            {
                return NotFound(new { message = "User not found." });
            }

            // Verify current password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.HashPassword);
            if (!isPasswordValid)
            {
                return BadRequest(new { message = "Incorrect current password." });
            }

            // Hash and update new password
            user.HashPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "ChangePassword", "User", user.Id.ToString(), $"User {user.Username} changed their password.");

            return Ok(new { message = "Password updated successfully." });
        }

        private async Task<string?> SaveSignatureFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "signatures");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/signatures/{uniqueFileName}";
        }
    }
}
