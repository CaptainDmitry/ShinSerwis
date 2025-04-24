using System.ComponentModel.DataAnnotations;

namespace ShinSerwis.Models
{
    public class Service
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        public int DurationMinutes { get; set; }
        
        public bool IsActive { get; set; } = true;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
} 