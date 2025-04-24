using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShinSerwis.Data;
using ShinSerwis.Models;
using ShinSerwis.Services;
using ShinSerwis.ViewModels;
using System.Security.Claims;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

namespace ShinSerwis.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher _passwordHasher;

        public AdminController(ApplicationDbContext context, PasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // Главная страница админ-панели
        public IActionResult Index()
        {
            return View();
        }

        #region Управление услугами

        // Список всех услуг (включая неактивные)
        public async Task<IActionResult> Services()
        {
            var services = await _context.Services.ToListAsync();
            return View(services);
        }

        // Метод для деактивации услуги
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            service.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Услуга успешно деактивирована";
            return RedirectToAction(nameof(Services));
        }

        // Метод для активации услуги
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            service.IsActive = true;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Услуга успешно активирована";
            return RedirectToAction(nameof(Services));
        }

        // Метод для полного удаления услуги
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            var hasAppointments = await _context.Appointments
                .AnyAsync(a => a.ServiceId == id);

            if (hasAppointments)
            {
                TempData["ErrorMessage"] = "Невозможно удалить услугу, так как с ней связаны записи на обслуживание";
                return RedirectToAction(nameof(Services));
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Услуга успешно удалена";
            return RedirectToAction(nameof(Services));
        }

        // Подтверждение удаления услуги
        public async Task<IActionResult> ConfirmDeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        #endregion

        #region Управление записями

        // Список всех записей
        public async Task<IActionResult> Appointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Car)
                .Include(a => a.Service)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        // Подробная информация о записи
        public async Task<IActionResult> AppointmentDetails(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Car)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // Изменение статуса записи
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAppointmentStatus(int id, AppointmentStatus status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(AppointmentDetails), new { id });
        }

        #endregion

        #region Управление сотрудниками

        // Список сотрудников
        public async Task<IActionResult> Employees()
        {
            var employees = await _context.Users
                .Where(u => u.Role == UserRole.Employee)
                .ToListAsync();

            return View(employees);
        }

        // Создание нового сотрудника
        public IActionResult CreateEmployee()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users
                    .AnyAsync(u => u.Username == model.Username || u.Email == model.Email);

                if (existingUser)
                {
                    ModelState.AddModelError("", "Пользователь с таким именем или email уже существует");
                    return View(model);
                }

                var employee = new User
                {
                    Username = model.Username,
                    Password = _passwordHasher.HashPassword(model.Password),
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    Role = UserRole.Employee
                };

                _context.Users.Add(employee);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Сотрудник успешно добавлен";
                return RedirectToAction(nameof(Employees));
            }

            return View(model);
        }

        // Редактирование сотрудника
        public async Task<IActionResult> EditEmployee(int id)
        {
            var employee = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Employee);

            if (employee == null)
            {
                return NotFound();
            }

            var model = new EditEmployeeViewModel
            {
                Id = employee.Id,
                Username = employee.Username,
                Email = employee.Email,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                PhoneNumber = employee.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(int id, EditEmployeeViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var employee = await _context.Users.FindAsync(id);
                    if (employee == null || employee.Role != UserRole.Employee)
                    {
                        return NotFound();
                    }

                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => (u.Username == model.Username || u.Email == model.Email) && u.Id != id);

                    if (existingUser != null)
                    {
                        ModelState.AddModelError("", "Пользователь с таким именем или email уже существует");
                        return View(model);
                    }

                    employee.Username = model.Username;
                    employee.Email = model.Email;
                    employee.FirstName = model.FirstName;
                    employee.LastName = model.LastName;
                    employee.PhoneNumber = model.PhoneNumber;

                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        employee.Password = _passwordHasher.HashPassword(model.NewPassword);
                    }

                    _context.Update(employee);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Данные сотрудника успешно обновлены";
                    return RedirectToAction(nameof(Employees));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Произошла ошибка при обновлении данных: {ex.Message}");
                }
            }

            return View(model);
        }

        // Удаление сотрудника
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Employee);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        [HttpPost, ActionName("DeleteEmployee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployeeConfirmed(int id)
        {
            var employee = await _context.Users.FindAsync(id);
            if (employee == null || employee.Role != UserRole.Employee)
            {
                return NotFound();
            }

            var hasAppointments = await _context.Appointments
                .AnyAsync(a => a.User.Id == id);

            if (hasAppointments)
            {
                TempData["ErrorMessage"] = "Невозможно удалить сотрудника, так как с ним связаны записи на обслуживание";
                return RedirectToAction(nameof(Employees));
            }

            _context.Users.Remove(employee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Сотрудник успешно удален";
            return RedirectToAction(nameof(Employees));
        }

        #endregion

        #region Управление клиентами

        // Список клиентов
        public async Task<IActionResult> Clients()
        {
            var clients = await _context.Users
                .Where(u => u.Role == UserRole.Client)
                .Include(u => u.Cars)
                .ToListAsync();

            return View(clients);
        }

        // Просмотр информации о клиенте
        public async Task<IActionResult> ClientDetails(int id)
        {
            var client = await _context.Users
                .Include(u => u.Cars)
                .Include(u => u.Appointments)
                    .ThenInclude(a => a.Service)
                .Include(u => u.Appointments)
                    .ThenInclude(a => a.Car)
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Client);

            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        #endregion

        #region Статистика

        // Статистика по системе
        public async Task<IActionResult> Statistics()
        {
            var statistics = new Dictionary<string, object>();
            
            statistics["TotalClients"] = await _context.Users
                .CountAsync(u => u.Role == UserRole.Client);
            
            statistics["TotalEmployees"] = await _context.Users
                .CountAsync(u => u.Role == UserRole.Employee);
            
            statistics["TotalCars"] = await _context.Cars.CountAsync();
            
            statistics["TotalServices"] = await _context.Services.CountAsync();
            statistics["ActiveServices"] = await _context.Services
                .CountAsync(s => s.IsActive);
            
            statistics["TotalAppointments"] = await _context.Appointments.CountAsync();
            statistics["PendingAppointments"] = await _context.Appointments
                .CountAsync(a => a.Status == AppointmentStatus.Pending);
            statistics["InProgressAppointments"] = await _context.Appointments
                .CountAsync(a => a.Status == AppointmentStatus.InProgress);
            statistics["CompletedAppointments"] = await _context.Appointments
                .CountAsync(a => a.Status == AppointmentStatus.Completed);
            statistics["CancelledAppointments"] = await _context.Appointments
                .CountAsync(a => a.Status == AppointmentStatus.Cancelled);
            
            var serviceStats = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed)
                .GroupBy(a => a.ServiceId)
                .Select(g => new
                {
                    ServiceId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
            
            var servicesWithStats = await _context.Services
                .Where(s => serviceStats.Select(ss => ss.ServiceId).Contains(s.Id))
                .ToListAsync();
            
            var serviceStatistics = servicesWithStats
                .Select(s => new
                {
                    ServiceName = s.Name,
                    Count = serviceStats.First(ss => ss.ServiceId == s.Id).Count
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();
            
            statistics["ServiceStatistics"] = serviceStatistics;
            
            var completedServicesByRevenue = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed)
                .Join(_context.Services, 
                    a => a.ServiceId, 
                    s => s.Id, 
                    (a, s) => new { Appointment = a, Service = s })
                .GroupBy(x => new { x.Service.Id, x.Service.Name, x.Service.Price })
                .Select(g => new
                {
                    ServiceId = g.Key.Id,
                    ServiceName = g.Key.Name,
                    Count = g.Count(),
                    Revenue = g.Count() * g.Key.Price
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();
            
            statistics["CompletedServices"] = completedServicesByRevenue;
            
            var appointments = await _context.Appointments.ToListAsync();
            
            var dayOfWeekStats = appointments
                .GroupBy(a => (int)a.AppointmentDate.DayOfWeek)
                .Select(g => new
                {
                    DayOfWeek = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.DayOfWeek)
                .ToList();
            
            statistics["DayOfWeekStatistics"] = dayOfWeekStats;
            
            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
            var monthlyRevenue = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed && a.AppointmentDate >= oneMonthAgo)
                .Join(_context.Services, a => a.ServiceId, s => s.Id, (a, s) => s.Price)
                .SumAsync();
            
            statistics["MonthlyRevenue"] = monthlyRevenue;
            
            var totalRevenue = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed)
                .Join(_context.Services, a => a.ServiceId, s => s.Id, (a, s) => s.Price)
                .SumAsync();
            
            statistics["TotalRevenue"] = totalRevenue;
            
            return View(statistics);
        }

        #endregion
    }
} 