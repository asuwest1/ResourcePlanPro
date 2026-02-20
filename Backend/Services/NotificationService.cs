using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResourcePlanPro.API.Data;
using ResourcePlanPro.API.Models;
using ResourcePlanPro.API.Models.DTOs;

namespace ResourcePlanPro.API.Services
{
    public interface INotificationService
    {
        Task<List<NotificationLogDto>> GetNotificationHistoryAsync(int count = 50);
        Task<NotificationLogDto> SendConflictNotificationsAsync(SendNotificationRequest request);
        Task<int> CheckAndNotifyConflictsAsync(List<string> recipientEmails);
    }

    public class NotificationService : INotificationService
    {
        private readonly ResourcePlanProContext _context;
        private readonly IDashboardService _dashboardService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ResourcePlanProContext context,
            IDashboardService dashboardService,
            IConfiguration configuration,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _dashboardService = dashboardService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<NotificationLogDto>> GetNotificationHistoryAsync(int count = 50)
        {
            // Cap the count to prevent unbounded queries
            if (count < 1) count = 1;
            if (count > 500) count = 500;

            var logs = await _context.NotificationLogs
                .OrderByDescending(n => n.CreatedDate)
                .Take(count)
                .ToListAsync();

            return logs.Select(n => new NotificationLogDto
            {
                NotificationId = n.NotificationId,
                NotificationType = n.NotificationType,
                RecipientEmail = n.RecipientEmail,
                Subject = n.Subject,
                Status = n.Status,
                CreatedDate = n.CreatedDate,
                SentDate = n.SentDate
            }).ToList();
        }

        public async Task<NotificationLogDto> SendConflictNotificationsAsync(SendNotificationRequest request)
        {
            var conflicts = await _dashboardService.GetConflictsAsync();

            if (!request.IncludeOverallocations)
                conflicts = conflicts.Where(c => c.ConflictType != "OverallocatedEmployee").ToList();
            if (!request.IncludeUnderstaffing)
                conflicts = conflicts.Where(c => c.ConflictType != "UnderstaffedProject").ToList();

            if (!conflicts.Any())
            {
                return new NotificationLogDto
                {
                    NotificationType = "ConflictReport",
                    Subject = "No conflicts found",
                    Status = "NoAction",
                    CreatedDate = DateTime.UtcNow
                };
            }

            var subject = $"ResourcePlan Pro - {conflicts.Count} Resource Conflict(s) Detected";
            var body = BuildConflictEmailBody(conflicts);

            var recipientEmails = request.RecipientEmails.Any()
                ? request.RecipientEmails
                : new List<string> { "admin@resourceplanpro.com" };

            NotificationLog? lastLog = null;
            foreach (var email in recipientEmails)
            {
                var log = new NotificationLog
                {
                    NotificationType = "ConflictReport",
                    RecipientEmail = email,
                    Subject = subject,
                    Body = body,
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow
                };

                try
                {
                    await SendEmailAsync(email, subject, body);
                    log.Status = "Sent";
                    log.SentDate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    log.Status = "Queued";
                    log.ErrorMessage = $"Email delivery deferred: {ex.Message}. Notification logged for retry.";
                    _logger.LogWarning(ex, "Email delivery deferred for {Email}, notification queued", email);
                }

                _context.NotificationLogs.Add(log);
                lastLog = log;
            }

            await _context.SaveChangesAsync();

            return new NotificationLogDto
            {
                NotificationId = lastLog?.NotificationId ?? 0,
                NotificationType = "ConflictReport",
                RecipientEmail = string.Join(", ", recipientEmails),
                Subject = subject,
                Status = lastLog?.Status ?? "Queued",
                CreatedDate = DateTime.UtcNow,
                SentDate = lastLog?.SentDate
            };
        }

        public async Task<int> CheckAndNotifyConflictsAsync(List<string> recipientEmails)
        {
            var conflicts = await _dashboardService.GetConflictsAsync();
            if (!conflicts.Any()) return 0;

            var request = new SendNotificationRequest
            {
                RecipientEmails = recipientEmails,
                IncludeOverallocations = true,
                IncludeUnderstaffing = true
            };

            await SendConflictNotificationsAsync(request);
            return conflicts.Count;
        }

        private string BuildConflictEmailBody(List<ConflictSummaryDto> conflicts)
        {
            var overallocations = conflicts.Where(c => c.ConflictType == "OverallocatedEmployee").ToList();
            var understaffing = conflicts.Where(c => c.ConflictType == "UnderstaffedProject").ToList();

            var body = "<html><body style='font-family: Arial, sans-serif;'>";
            body += "<h2 style='color: #2c3e50;'>Resource Conflict Report</h2>";
            body += $"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>";

            if (overallocations.Any())
            {
                body += "<h3 style='color: #e74c3c;'>Over-allocated Employees</h3>";
                body += "<table style='border-collapse: collapse; width: 100%;'>";
                body += "<tr style='background: #f8f9fa;'><th style='padding: 8px; border: 1px solid #dee2e6;'>Employee</th>";
                body += "<th style='padding: 8px; border: 1px solid #dee2e6;'>Department</th>";
                body += "<th style='padding: 8px; border: 1px solid #dee2e6;'>Week</th>";
                body += "<th style='padding: 8px; border: 1px solid #dee2e6;'>Over-allocation</th>";
                body += "<th style='padding: 8px; border: 1px solid #dee2e6;'>Priority</th></tr>";

                foreach (var c in overallocations)
                {
                    var color = c.Priority == "High" ? "#e74c3c" : c.Priority == "Medium" ? "#f39c12" : "#27ae60";
                    body += $"<tr><td style='padding: 8px; border: 1px solid #dee2e6;'>{WebUtility.HtmlEncode(c.EntityName)}</td>";
                    body += $"<td style='padding: 8px; border: 1px solid #dee2e6;'>{WebUtility.HtmlEncode(c.DepartmentName)}</td>";
                    body += $"<td style='padding: 8px; border: 1px solid #dee2e6;'>{c.WeekStartDate:MMM dd, yyyy}</td>";
                    body += $"<td style='padding: 8px; border: 1px solid #dee2e6;'>{c.Variance:F1} hrs</td>";
                    body += $"<td style='padding: 8px; border: 1px solid #dee2e6; color: {color};'>{WebUtility.HtmlEncode(c.Priority)}</td></tr>";
                }
                body += "</table>";
            }

            if (understaffing.Any())
            {
                body += "<h3 style='color: #f39c12;'>Understaffed Projects</h3>";
                body += "<table style='border-collapse: collapse; width: 100%;'>";
                body += "<tr style='background: #f8f9fa;'><th style='padding: 8px; border: 1px solid #dee2e6;'>Project</th>";
                body += "<th style='padding: 8px; border: 1px solid #dee2e6;'>Department</th>";
                body += "<th style='padding: 8px; border: 1px solid #dee2e6;'>Week</th>";
                body += "<th style='padding: 8px; border: 1px solid #dee2e6;'>Hours Needed</th>";
                body += "<th style='padding: 8px; border: 1px solid #dee2e6;'>Priority</th></tr>";

                foreach (var c in understaffing)
                {
                    var color = c.Priority == "High" ? "#e74c3c" : c.Priority == "Medium" ? "#f39c12" : "#27ae60";
                    body += $"<tr><td style='padding: 8px; border: 1px solid #dee2e6;'>{WebUtility.HtmlEncode(c.EntityName)}</td>";
                    body += $"<td style='padding: 8px; border: 1px solid #dee2e6;'>{WebUtility.HtmlEncode(c.DepartmentName)}</td>";
                    body += $"<td style='padding: 8px; border: 1px solid #dee2e6;'>{c.WeekStartDate:MMM dd, yyyy}</td>";
                    body += $"<td style='padding: 8px; border: 1px solid #dee2e6;'>{c.Variance:F1} hrs</td>";
                    body += $"<td style='padding: 8px; border: 1px solid #dee2e6; color: {color};'>{WebUtility.HtmlEncode(c.Priority)}</td></tr>";
                }
                body += "</table>";
            }

            body += "<hr style='margin-top: 20px;'>";
            body += "<p style='color: #7f8c8d; font-size: 12px;'>This is an automated notification from ResourcePlan Pro.</p>";
            body += "</body></html>";

            return body;
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            // Basic email format validation
            if (string.IsNullOrWhiteSpace(to) || !to.Contains('@') || to.Contains('\n') || to.Contains('\r'))
            {
                throw new ArgumentException($"Invalid email address: {to}");
            }

            var smtpHost = _configuration["SmtpSettings:Host"];
            var smtpPort = int.TryParse(_configuration["SmtpSettings:Port"], out var port) ? port : 587;
            var smtpUser = _configuration["SmtpSettings:Username"];
            var smtpPass = _configuration["SmtpSettings:Password"];
            var fromEmail = _configuration["SmtpSettings:FromEmail"] ?? "noreply@resourceplanpro.com";

            if (string.IsNullOrEmpty(smtpHost))
            {
                _logger.LogInformation("SMTP not configured. Email to {To} queued: {Subject}", to, subject);
                throw new InvalidOperationException("SMTP server not configured. Notification has been logged and queued for delivery when SMTP is configured.");
            }

            using var client = new SmtpClient(smtpHost, smtpPort);
            if (!string.IsNullOrEmpty(smtpUser))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            }
            client.EnableSsl = true;

            using var message = new MailMessage(fromEmail, to, subject, body)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(message);
        }
    }
}
