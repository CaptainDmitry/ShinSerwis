using System.ComponentModel.DataAnnotations;

namespace ShinSerwis.ViewModels
{
    public class AppointmentViewModel
    {
        [Required]
        public int ServiceId { get; set; }
        
        [Required]
        public int CarId { get; set; }
        
        [Required]
        [Display(Name = "Дата и время")]
        public DateTime AppointmentDate { get; set; }
        
        [Display(Name = "Примечания")]
        public string? Notes { get; set; }
    }
} 