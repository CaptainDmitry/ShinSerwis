using Microsoft.EntityFrameworkCore;
using ShinSerwis.Models;
using ShinSerwis.Services;

namespace ShinSerwis.Data
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher _passwordHasher;

        public DataSeeder(ApplicationDbContext context, PasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedData()
        {
            if (await _context.Users.AnyAsync() || await _context.Services.AnyAsync())
            {
                return;
            }

            var admin = new User
            {
                Username = "admin",
                Password = _passwordHasher.HashPassword("Admin123!"),
                Email = "admin@shinserwis.ru",
                FirstName = "Администратор",
                LastName = "Системы",
                PhoneNumber = "+7 (999) 123-45-67",
                Role = UserRole.Admin
            };

            var employee = new User
            {
                Username = "employee",
                Password = _passwordHasher.HashPassword("Employee123!"),
                Email = "employee@shinserwis.ru",
                FirstName = "Иван",
                LastName = "Петров",
                PhoneNumber = "+7 (999) 765-43-21",
                Role = UserRole.Employee
            };

            var client = new User
            {
                Username = "client",
                Password = _passwordHasher.HashPassword("Client123!"),
                Email = "client@example.com",
                FirstName = "Алексей",
                LastName = "Сидоров",
                PhoneNumber = "+7 (999) 111-22-33",
                Role = UserRole.Client
            };

            await _context.Users.AddRangeAsync(admin, employee, client);

            var services = new List<Service>
            {
                new Service
                {
                    Name = "Шиномонтаж",
                    Description = "Профессиональная замена и балансировка колес с использованием современного оборудования",
                    Price = 2500,
                    DurationMinutes = 60,
                    IsActive = true
                },
                new Service
                {
                    Name = "Замена масла",
                    Description = "Быстрая и качественная замена масла и фильтров с использованием высококачественных материалов",
                    Price = 1800,
                    DurationMinutes = 30,
                    IsActive = true
                },
                new Service
                {
                    Name = "Диагностика",
                    Description = "Комплексная компьютерная диагностика всех систем автомобиля с выявлением неисправностей",
                    Price = 3000,
                    DurationMinutes = 90,
                    IsActive = true
                },
                new Service
                {
                    Name = "Ремонт тормозной системы",
                    Description = "Полный ремонт и обслуживание тормозной системы, включая замену колодок, дисков и цилиндров",
                    Price = 5000,
                    DurationMinutes = 120,
                    IsActive = true
                },
                new Service
                {
                    Name = "Кондиционер - заправка",
                    Description = "Диагностика, ремонт и заправка автомобильных кондиционеров",
                    Price = 3500,
                    DurationMinutes = 60,
                    IsActive = true
                }
            };

            await _context.Services.AddRangeAsync(services);
            
            var car = new Car
            {
                Brand = "Toyota",
                Model = "Camry",
                Year = "2020",
                LicensePlate = "А123ВС77",
                VIN = "4T1BF1FK5CU513879",
                User = client
            };

            await _context.Cars.AddAsync(car);

            await _context.SaveChangesAsync();
        }
    }
} 