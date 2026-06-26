using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EGC_Ticketing_System.DTOs.Logs;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Middleware;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Controllers
{
    [Authorize]
    [AuthorizedRoles(UserRole.Admin)]
    public class LogsController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;

        public LogsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] LogFilterDto filter)
        {
            if (filter.Limit > 100)
            {
                filter.Limit = 100; // Cap search results size
            }

            var totalCount = await _unitOfWork.Logs.GetLogsCountAsync(
                filter.UserId,
                filter.Action,
                filter.EntityName,
                filter.EntityId,
                filter.StartDate,
                filter.EndDate);

            var logs = await _unitOfWork.Logs.GetLogsAsync(
                filter.UserId,
                filter.Action,
                filter.EntityName,
                filter.EntityId,
                filter.StartDate,
                filter.EndDate,
                filter.Skip,
                filter.Limit);

            var response = logs.Select(l => new LogResponseDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserFullName = l.User?.FullName,
                Action = l.Action,
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                CreatedAt = l.CreatedAt
            });

            return Ok(new
            {
                TotalCount = totalCount,
                Logs = response
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var log = await _unitOfWork.Logs.GetLogWithUserAsync(id);
            if (log == null)
            {
                return NotFound(new { message = "Log entry not found." });
            }

            var response = new LogDetailsResponseDto
            {
                Id = log.Id,
                UserId = log.UserId,
                Username = log.User?.Username,
                UserFullName = log.User?.FullName,
                Action = log.Action,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Details = log.Details,
                CreatedAt = log.CreatedAt
            };

            return Ok(response);
        }
    }
}
