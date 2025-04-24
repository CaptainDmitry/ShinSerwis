using System.ComponentModel.DataAnnotations;
using ShinSerwis.Models;

namespace ShinSerwis.ViewModels
{
    public class CreateEmployeeViewModel
    {
        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [Display(Name = "Имя пользователя")]
        [StringLength(100, ErrorMessage = "Имя пользователя не должно превышать 100 символов")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [Display(Name = "Пароль")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать не менее 6 символов")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [Display(Name = "Подтверждение пароля")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Email")]
        [StringLength(100, ErrorMessage = "Email не должен превышать 100 символов")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        [Display(Name = "Имя")]
        [StringLength(100, ErrorMessage = "Имя не должно превышать 100 символов")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна")]
        [Display(Name = "Фамилия")]
        [StringLength(100, ErrorMessage = "Фамилия не должна превышать 100 символов")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Номер телефона обязателен")]
        [Phone(ErrorMessage = "Некорректный формат телефона")]
        [Display(Name = "Номер телефона")]
        [StringLength(20, ErrorMessage = "Номер телефона не должен превышать 20 символов")]
        public string PhoneNumber { get; set; }
    }

    public class EditEmployeeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [Display(Name = "Имя пользователя")]
        [StringLength(100, ErrorMessage = "Имя пользователя не должно превышать 100 символов")]
        public string Username { get; set; }

        [Display(Name = "Новый пароль")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать не менее 6 символов")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Display(Name = "Подтверждение пароля")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Email")]
        [StringLength(100, ErrorMessage = "Email не должен превышать 100 символов")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        [Display(Name = "Имя")]
        [StringLength(100, ErrorMessage = "Имя не должно превышать 100 символов")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна")]
        [Display(Name = "Фамилия")]
        [StringLength(100, ErrorMessage = "Фамилия не должна превышать 100 символов")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Номер телефона обязателен")]
        [Phone(ErrorMessage = "Некорректный формат телефона")]
        [Display(Name = "Номер телефона")]
        [StringLength(20, ErrorMessage = "Номер телефона не должен превышать 20 символов")]
        public string PhoneNumber { get; set; }
    }
} 