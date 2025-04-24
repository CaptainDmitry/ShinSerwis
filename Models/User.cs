using System.ComponentModel.DataAnnotations;

namespace ShinSerwis.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Username { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Password { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; }
        
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; }
        
        [Required]
        public UserRole Role { get; set; }

        public ICollection<Car> Cars { get; set; } = new List<Car>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }

    public enum UserRole
    {
        Client,
        Employee,
        Admin
    }
} 