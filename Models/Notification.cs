using System;
using System.ComponentModel.DataAnnotations;

namespace ShinSerwis.Models
{
    public enum NotificationType
    {
        NewAppointment,
        StatusChanged,
        FollowUpReminder
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public int? AppointmentId { get; set; }
        public virtual Appointment Appointment { get; set; }

        public NotificationType Type { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
} 