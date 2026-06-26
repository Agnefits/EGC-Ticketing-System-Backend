using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Services
{
    public class DeadlineNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeadlineNotificationService> _logger;
        
        // Tracks sent notifications in-memory to prevent duplicate emails during run: "warning-ticketId", "overdue-ticketId"
        private static readonly ConcurrentDictionary<string, bool> _sentNotifications = new();

        public DeadlineNotificationService(IServiceProvider serviceProvider, ILogger<DeadlineNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Deadline Notification Background Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDeadlinesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred processing ticket deadline notifications.");
                }

                // Check every 1 hour (or custom frequency)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessDeadlinesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;
            
            // Get all tickets that are active (not completed, not deleted)
            var tickets = await unitOfWork.Tickets.FindAsync(t => 
                t.Status != TicketStatus.Completed && 
                t.Status != TicketStatus.Deleted && 
                t.Deadline.HasValue);

            foreach (var ticket in tickets)
            {
                var deadline = ticket.Deadline!.Value;
                var timeRemaining = deadline - now;

                // Case 1: One day to deadline (between 0 and 24 hours remaining)
                if (timeRemaining.TotalHours > 0 && timeRemaining.TotalHours <= 24)
                {
                    string notificationKey = $"warning-{ticket.Id}";
                    if (_sentNotifications.TryAdd(notificationKey, true))
                    {
                        await SendDeadlineEmailAsync(unitOfWork, emailService, ticket, isOverdue: false);
                    }
                }
                // Case 2: One day past deadline (deadline was between 0 and 24 hours ago)
                else if (timeRemaining.TotalHours < 0 && Math.Abs(timeRemaining.TotalHours) <= 24)
                {
                    string notificationKey = $"overdue-{ticket.Id}";
                    if (_sentNotifications.TryAdd(notificationKey, true))
                    {
                        await SendDeadlineEmailAsync(unitOfWork, emailService, ticket, isOverdue: true);
                    }
                }
            }
        }

        private async Task SendDeadlineEmailAsync(IUnitOfWork unitOfWork, IEmailService emailService, Ticket ricket, bool isOverdue)
        {
            string? recipientEmail = null;
            string recipientName = "";

            // 1. Sent to member (if assigned and email exists)
            if (ricket.MemberId.HasValue)
            {
                var member = await unitOfWork.Users.GetByIdAsync(ricket.MemberId.Value);
                if (member != null && !string.IsNullOrEmpty(member.Email))
                {
                    recipientEmail = member.Email;
                    recipientName = member.FullName;
                }
            }

            // 2. If no member is assigned (or member has no email), send to team leader
            if (string.IsNullOrEmpty(recipientEmail))
            {
                var teamMembers = await unitOfWork.TeamMembers.GetByTeamIdAsync(ricket.TeamId);
                var leader = teamMembers.FirstOrDefault(tm => tm.IsTeamLeader);
                if (leader != null && leader.Member != null && !string.IsNullOrEmpty(leader.Member.Email))
                {
                    recipientEmail = leader.Member.Email;
                    recipientName = leader.Member.FullName;
                }
            }

            // Send only if email is found
            if (!string.IsNullOrEmpty(recipientEmail))
            {
                var team = await unitOfWork.Teams.GetByIdAsync(ricket.TeamId);
                string teamName = team?.Name ?? "Unknown Team";

                string subject = isOverdue 
                    ? $"[OVERDUE ALERT] Ticket \"{ricket.Title}\" is Past Deadline" 
                    : $"[URGENT] 24 Hours Left: Ticket \"{ricket.Title}\"";

                string contentHtml = isOverdue
                    ? $@"
                        <h3 style='color: #c0392b; margin-top: 0; font-family: sans-serif;'>Ticket Overdue Alert</h3>
                        <p>Hello <strong>{recipientName}</strong>,</p>
                        <p>This is an alert that the ticket <strong>""{ricket.Title}""</strong> assigned to team <strong>""{teamName}""</strong> has passed its deadline and remains incomplete.</p>
                        <div style='border-left: 4px solid #c0392b; padding-left: 16px; margin: 20px 0; background-color: #fdf2f2; padding: 12px; font-family: sans-serif;'>
                            <p style='margin: 4px 0;'><strong>Ticket Title:</strong> {ricket.Title}</p>
                            <p style='margin: 4px 0;'><strong>Deadline:</strong> {ricket.Deadline:yyyy-MM-dd HH:mm} UTC</p>
                            <p style='margin: 4px 0;'><strong>Status:</strong> {ricket.Status}</p>
                        </div>
                        <p style='color: #c0392b; font-weight: 600;'>Please review and complete this ticket as soon as possible.</p>"
                    : $@"
                        <h3 style='color: #d35400; margin-top: 0; font-family: sans-serif;'>Ticket Deadline Alert: 24 Hours Remaining</h3>
                        <p>Hello <strong>{recipientName}</strong>,</p>
                        <p>This is a friendly reminder that the ticket <strong>""{ricket.Title}""</strong> assigned to team <strong>""{teamName}""</strong> is due in less than 24 hours.</p>
                        <div style='border-left: 4px solid #d35400; padding-left: 16px; margin: 20px 0; background-color: #fff9f2; padding: 12px; font-family: sans-serif;'>
                            <p style='margin: 4px 0;'><strong>Ticket Title:</strong> {ricket.Title}</p>
                            <p style='margin: 4px 0;'><strong>Deadline:</strong> {ricket.Deadline:yyyy-MM-dd HH:mm} UTC</p>
                            <p style='margin: 4px 0;'><strong>Status:</strong> {ricket.Status}</p>
                        </div>
                        <p>Please update your progress and ensure all deliverables are completed in a timely manner.</p>";

                string emailBody = $@"
                    <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05);"">
                        <div style=""background-color: #1a252f; padding: 24px; text-align: center; color: #ffffff;"">
                            <h1 style=""margin: 0; font-size: 24px; font-weight: 600; letter-spacing: 0.5px;"">EGC TICKETING SYSTEM</h1>
                        </div>
                        <div style=""padding: 32px; background-color: #ffffff; color: #333333; line-height: 1.6;"">
                            {contentHtml}
                        </div>
                        <div style=""background-color: #f8f9fa; padding: 16px; text-align: center; font-size: 12px; color: #7f8c8d; border-top: 1px solid #eeeeee;"">
                            <p style=""margin: 0;"">This is an automated system notification from EGC Ticketing System.</p>
                            <p style=""margin: 4px 0 0 0;"">&copy; 2026 EGC Ticketing System. All rights reserved.</p>
                        </div>
                    </div>";

                await emailService.SendEmailAsync(recipientEmail, subject, emailBody);
                _logger.LogInformation($"Sent deadline notification ({ (isOverdue ? "overdue" : "warning") }) for ticket ID {ricket.Id} to {recipientEmail}");
            }
        }
    }
}
