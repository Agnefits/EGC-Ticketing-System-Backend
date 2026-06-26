using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using EGC_Ticketing_System.DTOs.Teams;
using EGC_Ticketing_System.DTOs.Tickets;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Validators;
using EGC_Ticketing_System.Controllers;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Repositories.Interfaces;
using EGC_Ticketing_System.Services.Interfaces;

namespace EGC_Ticketing_System.Tests
{
    public class TeamTicketValidationTests
    {
        private readonly CreateTeamDtoValidator _createTeamValidator;
        private readonly CreateTicketDtoValidator _createTicketValidator;
        private readonly UpdateTicketStatusDtoValidator _updateTicketStatusValidator;

        public TeamTicketValidationTests()
        {
            _createTeamValidator = new CreateTeamDtoValidator();
            _createTicketValidator = new CreateTicketDtoValidator();
            _updateTicketStatusValidator = new UpdateTicketStatusDtoValidator();
        }

        [Theory]
        [InlineData((TeamStatus)999)]
        [InlineData((TeamStatus)(-1))]
        public void TeamStatus_Invalid_Fails(TeamStatus status)
        {
            var dto = new CreateTeamDto { Name = "Team A", Description = "Desc", Status = status };
            var result = _createTeamValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.Status));
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData((TicketStatus)999)]
        [InlineData((TicketStatus)(-1))]
        public void TicketStatus_Invalid_Fails(TicketStatus status)
        {
            var dto = new ChangeTicketStatusDto { Status = status };
            var result = _updateTicketStatusValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.Status));
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData((TicketPriority)999)]
        [InlineData((TicketPriority)(-1))]
        public void TicketPriority_Invalid_Fails(TicketPriority priority)
        {
            var dto = new CreateTicketDto { TeamId = 1, Title = "Test Ticket", Priority = priority };
            var result = _createTicketValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.Priority));
            Assert.NotNull(error);
        }

        [Fact]
        public void TicketDeadline_PastDate_Fails()
        {
            var dto = new CreateTicketDto 
            { 
                TeamId = 1, 
                Title = "Test Ticket", 
                Deadline = DateTime.UtcNow.AddMinutes(-5) // Past date
            };
            var result = _createTicketValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.Deadline));
            Assert.NotNull(error);
        }

        [Fact]
        public void TicketDeadline_FutureDate_Passes()
        {
            var dto = new CreateTicketDto 
            { 
                TeamId = 1, 
                Title = "Test Ticket", 
                Deadline = DateTime.UtcNow.AddHours(2) // Future date
            };
            var result = _createTicketValidator.Validate(dto);
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(dto.Deadline));
            Assert.Null(error);
        }

        [Fact]
        public async Task DeleteUser_RemovesTeamMappings()
        {
            // Arrange
            var userId = 5;
            var creatorId = 1;

            var userToDelete = new User { Id = userId, Username = "todelete", Status = UserStatus.Active };
            
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId)).ReturnsAsync(userToDelete);

            var memberships = new List<TeamMember>
            {
                new TeamMember { TeamId = 10, MemberId = userId },
                new TeamMember { TeamId = 11, MemberId = userId }
            };
            mockUnitOfWork.Setup(u => u.TeamMembers.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TeamMember, bool>>>()))
                .ReturnsAsync(memberships);

            mockUnitOfWork.Setup(u => u.TeamMembers.Delete(It.IsAny<TeamMember>())).Verifiable();
            
            var mockLogRepository = new Mock<ILogRepository>();
            mockUnitOfWork.Setup(u => u.Logs).Returns(mockLogRepository.Object);
            mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            var mockValidationService = new Mock<IValidationService>();

            var controller = new UsersController(mockUnitOfWork.Object, mockValidationService.Object);

            // Mock HttpContext for User authorization claims (to pass self-deletion check and GetCurrentUserId/Role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, creatorId.ToString()),
                new Claim(ClaimTypes.Role, UserRole.Admin.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await controller.Delete(userId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            mockUnitOfWork.Verify(u => u.TeamMembers.Delete(It.IsAny<TeamMember>()), Times.Exactly(2));
            Assert.Equal(UserStatus.Deleted, userToDelete.Status);
        }
    }
}
