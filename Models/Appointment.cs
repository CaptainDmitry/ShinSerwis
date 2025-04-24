using System.ComponentModel.DataAnnotations;

namespace ShinSerwis.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        public User User { get; set; }
        
        [Required]
        public int CarId { get; set; }
        public Car Car { get; set; }
        
        [Required]
        public int ServiceId { get; set; }
        public Service Service { get; set; }
        
        [Required]
        public DateTime AppointmentDate { get; set; }
        
        [Required]
        public AppointmentStatus Status { get; set; }
        
        public string? Notes { get; set; }
    }

    public enum AppointmentStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
} 