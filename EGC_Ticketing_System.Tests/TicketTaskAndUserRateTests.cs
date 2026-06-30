using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using EGC_Ticketing_System.DTOs.Tickets;
using EGC_Ticketing_System.DTOs.UserRates;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Repositories.Interfaces;
using EGC_Ticketing_System.Services.Classes;
using EGC_Ticketing_System.Validation;

namespace EGC_Ticketing_System.Tests
{
    public class TicketTaskAndUserRateTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITicketRepository> _mockTicketRepo;
        private readonly Mock<ITicketTaskRepository> _mockTaskRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<ITeamMemberRepository> _mockTeamMemberRepo;
        private readonly Mock<ITeamRepository> _mockTeamRepo;
        private readonly ValidationService _validationService;

        public TicketTaskAndUserRateTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTicketRepo = new Mock<ITicketRepository>();
            _mockTaskRepo = new Mock<ITicketTaskRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockTeamMemberRepo = new Mock<ITeamMemberRepository>();
            _mockTeamRepo = new Mock<ITeamRepository>();

            _mockUnitOfWork.Setup(u => u.Tickets).Returns(_mockTicketRepo.Object);
            _mockUnitOfWork.Setup(u => u.TicketTasks).Returns(_mockTaskRepo.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.TeamMembers).Returns(_mockTeamMemberRepo.Object);
            _mockUnitOfWork.Setup(u => u.Teams).Returns(_mockTeamRepo.Object);

            _validationService = new ValidationService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task ValidateTicketTaskCreate_TicketNotFound_ThrowsException()
        {
            _mockTicketRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Ticket)null);

            var dto = new CreateTicketTaskDto { Title = "Task 1" };

            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _validationService.ValidateTicketTaskCreateAsync(1, dto));
        }

        [Fact]
        public async Task ValidateUserRateCreate_RateSelf_ThrowsException()
        {
            var dto = new CreateUserRateDto { ToUserId = 1 };

            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _validationService.ValidateUserRateCreateAsync(1, dto));
        }

        [Fact]
        public async Task ValidateUserRateCreate_RaterNotFound_ThrowsException()
        {
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User)null);
            _mockUserRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new User { Id = 2, Role = UserRole.Member });

            var dto = new CreateUserRateDto { ToUserId = 2 };

            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                _validationService.ValidateUserRateCreateAsync(1, dto));
        }
    }
}
