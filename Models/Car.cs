using System.ComponentModel.DataAnnotations;

namespace ShinSerwis.Models
{
    public class Car
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        public User User { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Brand { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Model { get; set; }
        
        [Required]
        [StringLength(20)]
        public string LicensePlate { get; set; }
        
        [Required]
        [StringLength(4)]
        public string Year { get; set; }
        
        [StringLength(17, MinimumLength = 17, ErrorMessage = "VIN-код должен состоять из 17 символов")]
        public string? VIN { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
} 