using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EGC_Ticketing_System.DTOs.Users;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Middleware;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Services.Interfaces;

namespace EGC_Ticketing_System.Controllers
{
    [Authorize]
    [AuthorizedRoles(UserRole.Admin, UserRole.Manager)]
    public class UsersController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidationService _validationService;

        public UsersController(IUnitOfWork unitOfWork, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Status != UserStatus.Deleted);
            
            var response = users.Select(u => new UserResponseDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.Username,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                JobTitle = u.JobTitle,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt,
                SignatureUrl = u.SignatureUrl,
                CreatedById = u.CreatedById
            });

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var u = await _unitOfWork.Users.GetByIdAsync(id);
            if (u == null || u.Status == UserStatus.Deleted)
            {
                return NotFound(new { message = "User not found." });
            }

            var response = new UserResponseDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.Username,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                JobTitle = u.JobTitle,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt,
                SignatureUrl = u.SignatureUrl,
                CreatedById = u.CreatedById
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateUserDto dto)
        {
            var creatorId = GetCurrentUserId();
            var creatorRole = GetCurrentUserRole();

            // Manager can add members only
            if (creatorRole == UserRole.Manager && dto.Role != UserRole.Member)
            {
                return StatusCode(403, new { message = "Forbidden. Managers can only create Member users." });
            }

            await _validationService.ValidateUserCreateAsync(dto);

            var signaturePath = await SaveSignatureFileAsync(dto.Signature);

            var newUser = new User
            {
                FullName = dto.FullName,
                Username = dto.Username,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                HashPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                JobTitle = dto.JobTitle,
                Role = dto.Role,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                SignatureUrl = signaturePath,
                CreatedById = creatorId
            };

            await _unitOfWork.Users.AddAsync(newUser);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "CreateUser", "User", newUser.Id.ToString(), $"User {newUser.Username} created by {creatorRole} (ID: {creatorId})");

            var response = new UserResponseDto
            {
                Id = newUser.Id,
                FullName = newUser.FullName,
                Username = newUser.Username,
                Email = newUser.Email,
                PhoneNumber = newUser.PhoneNumber,
                JobTitle = newUser.JobTitle,
                Role = newUser.Role.ToString(),
                Status = newUser.Status.ToString(),
                CreatedAt = newUser.CreatedAt,
                SignatureUrl = newUser.SignatureUrl,
                CreatedById = newUser.CreatedById
            };

            return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            var userToUpdate = await _unitOfWork.Users.GetByIdAsync(id);
            if (userToUpdate == null || userToUpdate.Status == UserStatus.Deleted)
            {
                return NotFound(new { message = "User not found." });
            }

            var creatorId = GetCurrentUserId();
            var creatorRole = GetCurrentUserRole();

            // Manager can manage member role only
            if (creatorRole == UserRole.Manager)
            {
                // Manager cannot edit non-Members
                if (userToUpdate.Role != UserRole.Member)
                {
                    return StatusCode(403, new { message = "Forbidden. Managers can only update Member users." });
                }

                // Manager cannot promote a Member to Manager or Admin
                if (dto.Role != UserRole.Member)
                {
                    return StatusCode(403, new { message = "Forbidden. Managers cannot change roles of users to Manager or Admin." });
                }
            }

            await _validationService.ValidateUserUpdateAsync(id, dto);

            var signaturePath = await SaveSignatureFileAsync(dto.Signature);

            userToUpdate.FullName = dto.FullName;
            userToUpdate.Email = dto.Email;
            userToUpdate.PhoneNumber = dto.PhoneNumber;
            userToUpdate.JobTitle = dto.JobTitle;
            userToUpdate.Role = dto.Role;
            userToUpdate.Status = dto.Status;

            if (signaturePath != null)
            {
                userToUpdate.SignatureUrl = signaturePath;
            }

            _unitOfWork.Users.Update(userToUpdate);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "UpdateUser", "User", userToUpdate.Id.ToString(), $"User {userToUpdate.Username} updated by ID: {creatorId}");

            return Ok(new { message = "User updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userToDelete = await _unitOfWork.Users.GetByIdAsync(id);
            if (userToDelete == null || userToDelete.Status == UserStatus.Deleted)
            {
                return NotFound(new { message = "User not found." });
            }

            var creatorId = GetCurrentUserId();
            var creatorRole = GetCurrentUserRole();

            // Manager can delete/manage member role only
            if (creatorRole == UserRole.Manager && userToDelete.Role != UserRole.Member)
            {
                return StatusCode(403, new { message = "Forbidden. Managers can only delete Member users." });
            }

            // Prevent self-deletion
            if (userToDelete.Id == creatorId)
            {
                return BadRequest(new { message = "You cannot delete your own account." });
            }

            // Soft delete
            userToDelete.Status = UserStatus.Deleted;
            _unitOfWork.Users.Update(userToDelete);

            // Clean up TeamUser relationships
            var memberships = await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == id);
            foreach (var membership in memberships)
            {
                _unitOfWork.TeamMembers.Delete(membership);
            }

            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "DeleteUser", "User", userToDelete.Id.ToString(), $"User {userToDelete.Username} soft deleted by ID: {creatorId}");

            return Ok(new { message = "User deleted successfully." });
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
