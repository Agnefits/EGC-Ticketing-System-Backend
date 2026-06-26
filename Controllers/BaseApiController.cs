using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out var userId))
            {
                return userId;
            }
            throw new System.Security.Authentication.AuthenticationException("User is not authenticated.");
        }

        protected UserRole GetCurrentUserRole()
        {
            var claim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (Enum.TryParse<UserRole>(claim, out var role))
            {
                return role;
            }
            throw new System.Security.Authentication.AuthenticationException("User role is invalid or not found.");
        }

        protected async Task LogActivityAsync(IUnitOfWork unitOfWork, string action, string entityName, string? entityId, string details)
        {
            int? userId = null;
            try
            {
                userId = GetCurrentUserId();
            }
            catch
            {
                // Unauthenticated action (e.g. login failure or forgot password)
            }

            var log = new Log
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            await unitOfWork.Logs.AddAsync(log);
            await unitOfWork.CompleteAsync();
        }
    }
}
