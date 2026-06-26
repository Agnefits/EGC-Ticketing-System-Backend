using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EGC_Ticketing_System.DTOs.Auth;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Services;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Controllers
{
    public class AuthController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;

        // In-memory OTP store (Email -> (OtpCode, ExpirationTime))
        private static readonly Dictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();

        public AuthController(IUnitOfWork unitOfWork, IJwtService jwtService, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Find user by Email, Username, or PhoneNumber
            User? user = await _unitOfWork.Users.GetByEmailAsync(dto.Identifier);
            if (user == null)
            {
                user = await _unitOfWork.Users.GetByUsernameAsync(dto.Identifier);
            }
            if (user == null)
            {
                user = await _unitOfWork.Users.GetByPhoneNumberAsync(dto.Identifier);
            }

            if (user == null || user.Status == UserStatus.Deleted)
            {
                return Unauthorized(new { message = "Invalid username, email, phone number, or password." });
            }

            if (user.Status == UserStatus.Blocked)
            {
                return StatusCode(403, new { message = "Your account is blocked. Please contact support." });
            }

            // Verify Password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.HashPassword);
            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Invalid username, email, phone number, or password." });
            }

            // Generate JWT
            var token = _jwtService.GenerateToken(user);

            // Log login event
            await LogActivityAsync(_unitOfWork, "Login", "User", user.Id.ToString(), $"User {user.Username} successfully logged in.");

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role.ToString()
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
            if (user == null || user.Status == UserStatus.Deleted)
            {
                // Return success to prevent email enumeration attacks, but log internally
                return Ok(new { message = "If the email exists, an OTP has been sent." });
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            lock (_otpStore)
            {
                _otpStore[dto.Email.ToLower()] = (otp, expiry);
            }

            // Send Email
            string emailSubject = "Password Reset OTP - EGC Ticketing System";
            string emailBody = $@"
                <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05);"">
                    <div style=""background-color: #1a252f; padding: 24px; text-align: center; color: #ffffff;"">
                        <h1 style=""margin: 0; font-size: 24px; font-weight: 600; letter-spacing: 0.5px;"">EGC TICKETING SYSTEM</h1>
                    </div>
                    <div style=""padding: 32px; background-color: #ffffff; color: #333333; line-height: 1.6;"">
                        <h3 style=""color: #2c3e50; margin-top: 0; font-family: sans-serif;"">Password Reset Request</h3>
                        <p>You requested a password reset for your EGC Ticketing account. Please use the verification code below to complete the request:</p>
                        <div style=""background-color: #f1f2f6; padding: 18px; text-align: center; border-radius: 6px; margin: 24px 0;"">
                            <span style=""font-size: 32px; font-weight: bold; color: #e74c3c; letter-spacing: 6px; font-family: monospace;"">{otp}</span>
                        </div>
                        <p style=""font-size: 13px; color: #7f8c8d;"">This code is valid for 10 minutes. If you did not make this request, you can safely ignore this email.</p>
                    </div>
                    <div style=""background-color: #f8f9fa; padding: 16px; text-align: center; font-size: 12px; color: #7f8c8d; border-top: 1px solid #eeeeee;"">
                        <p style=""margin: 0;"">This is an automated system notification from EGC Ticketing System.</p>
                        <p style=""margin: 4px 0 0 0;"">&copy; 2026 EGC Ticketing System. All rights reserved.</p>
                    </div>
                </div>";

            await _emailService.SendEmailAsync(dto.Email, emailSubject, emailBody);

            await LogActivityAsync(_unitOfWork, "ForgotPasswordRequested", "User", user.Id.ToString(), $"OTP generated and emailed to {dto.Email}");

            return Ok(new { message = "If the email exists, an OTP has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var emailKey = dto.Email.ToLower();
            bool otpValid = false;

            lock (_otpStore)
            {
                if (_otpStore.TryGetValue(emailKey, out var val))
                {
                    if (val.Otp == dto.Otp && val.Expiry > DateTime.UtcNow)
                    {
                        otpValid = true;
                        _otpStore.Remove(emailKey);
                    }
                }
            }

            if (!otpValid)
            {
                return BadRequest(new { message = "Invalid or expired OTP." });
            }

            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
            if (user == null || user.Status == UserStatus.Deleted)
            {
                return NotFound(new { message = "User not found." });
            }

            // Reset password
            user.HashPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "PasswordResetSuccess", "User", user.Id.ToString(), $"Password reset successfully via OTP verification for {dto.Email}");

            return Ok(new { message = "Password reset successfully." });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Stateless logout log
            try
            {
                var userId = GetCurrentUserId();
                await LogActivityAsync(_unitOfWork, "Logout", "User", userId.ToString(), "User logged out.");
            }
            catch
            {
                // Already anonymous or expired token
            }

            return Ok(new { message = "Logged out successfully. Please discard the JWT token on client." });
        }
    }
}
