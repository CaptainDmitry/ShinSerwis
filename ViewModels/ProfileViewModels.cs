using System.ComponentModel.DataAnnotations;
using ShinSerwis.Models;

namespace ShinSerwis.ViewModels
{
    public class ProfileViewModel
    {
        public User User { get; set; }
        public List<Car> Cars { get; set; }
        public List<Appointment> Appointments { get; set; }
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [Display(Name = "Имя")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна")]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Номер телефона обязателен")]
        [Phone(ErrorMessage = "Некорректный формат телефона")]
        [Display(Name = "Номер телефона")]
        public string PhoneNumber { get; set; }
    }

    public class AddCarViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Марка автомобиля обязательна")]
        [Display(Name = "Марка")]
        [StringLength(50, ErrorMessage = "Марка не должна превышать 50 символов")]
        public string Brand { get; set; }

        [Required(ErrorMessage = "Модель автомобиля обязательна")]
        [Display(Name = "Модель")]
        [StringLength(50, ErrorMessage = "Модель не должна превышать 50 символов")]
        public string Model { get; set; }

        [Required(ErrorMessage = "Гос. номер обязателен")]
        [Display(Name = "Гос. номер")]
        [StringLength(20, ErrorMessage = "Гос. номер не должен превышать 20 символов")]
        [RegularExpression(@"^[A-Za-zА-Яа-я0-9\s-]+$", ErrorMessage = "Гос. номер содержит недопустимые символы")]
        public string LicensePlate { get; set; }

        [Required(ErrorMessage = "Год выпуска обязателен")]
        [Display(Name = "Год выпуска")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Год должен состоять из 4 цифр")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Год должен содержать только цифры")]
        public string Year { get; set; }

        [Display(Name = "VIN-код")]
        [VinValidation(ErrorMessage = "VIN-код должен состоять из 17 символов или быть пустым")]
        public string? VIN { get; set; }
    }

    public class VinValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            string vin = value.ToString().Trim();
            if (vin.Length == 17)
            {
                return ValidationResult.Success;
            }
            
            return new ValidationResult(ErrorMessage ?? "VIN-код должен состоять из 17 символов или быть пустым");
        }
    }
} 