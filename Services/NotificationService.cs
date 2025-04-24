using Microsoft.EntityFrameworkCore;
using ShinSerwis.Data;
using ShinSerwis.Models;

namespace ShinSerwis.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendNewAppointmentNotificationAsync(Appointment appointment)
        {
            var employees = await _context.Users
                .Where(u => u.Role == UserRole.Employee || u.Role == UserRole.Admin)
                .ToListAsync();

            foreach (var employee in employees)
            {
                var notification = new Notification
                {
                    UserId = employee.Id,
                    Title = "Новая запись",
                    Message = $"Новая запись от клиента {appointment.User.FirstName} {appointment.User.LastName} на {appointment.AppointmentDate.ToString("dd.MM.yyyy HH:mm")}",
                    Type = NotificationType.NewAppointment,
                    AppointmentId = appointment.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendStatusChangeNotificationAsync(Appointment appointment, AppointmentStatus oldStatus)
        {
            var notification = new Notification
            {
                UserId = appointment.UserId,
                Title = "Статус записи изменен",
                Message = $"Статус вашей записи на {appointment.AppointmentDate.ToString("dd.MM.yyyy HH:mm")} изменен с \"{oldStatus}\" на \"{appointment.Status}\"",
                Type = NotificationType.StatusChanged,
                AppointmentId = appointment.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task SendFollowUpReminderAsync(int userId, int appointmentId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = "Напоминание о повторной записи",
                Message = message,
                Type = NotificationType.FollowUpReminder,
                AppointmentId = appointmentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
} 