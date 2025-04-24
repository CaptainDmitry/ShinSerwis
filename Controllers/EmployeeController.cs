using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShinSerwis.Data;
using ShinSerwis.Models;
using ShinSerwis.Services;
using System.Security.Claims;

namespace ShinSerwis.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public EmployeeController(ApplicationDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            return RedirectToAction(nameof(Appointments));
        }

        public async Task<IActionResult> Appointments(AppointmentStatus? status = null)
        {
            IQueryable<Appointment> appointmentsQuery = _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Car)
                .Include(a => a.Service);

            if (status.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Status == status.Value);
            }
            
            appointmentsQuery = appointmentsQuery
                .OrderBy(a => a.Status == AppointmentStatus.Pending ? 0 : 
                       (a.Status == AppointmentStatus.InProgress ? 1 : 
                       (a.Status == AppointmentStatus.Completed ? 2 : 3)))
                .ThenByDescending(a => a.Id)
                .ThenBy(a => a.AppointmentDate);
            
            var appointments = await appointmentsQuery.ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(appointments);
        }

        // Просмотр деталей записи
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
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var oldStatus = appointment.Status;
            appointment.Status = status;
            
            int employeeId = int.Parse(User.FindFirstValue("UserId"));
            var employee = await _context.Users.FindAsync(employeeId);
            string employeeName = employee != null ? $"{employee.LastName} {employee.FirstName}" : $"ID={employeeId}";
            
            if (string.IsNullOrEmpty(appointment.Notes))
            {
                appointment.Notes = $"Статус изменен с {oldStatus} на {status} сотрудником {employeeName}";
            }
            else
            {
                appointment.Notes = $"Статус изменен с {oldStatus} на {status} сотрудником {employeeName}\n{appointment.Notes}";
            }

            await _context.SaveChangesAsync();
            
            await _notificationService.SendStatusChangeNotificationAsync(appointment, oldStatus);
            
            if (status == AppointmentStatus.Completed)
            {
                string followUpMessage = $"Ваша запись на {appointment.AppointmentDate.ToString("dd.MM.yyyy HH:mm")} была успешно завершена. Рекомендуем записаться на повторный осмотр через 6 месяцев.";
                await _notificationService.SendFollowUpReminderAsync(appointment.UserId, appointment.Id, followUpMessage);
            }
            
            TempData["SuccessMessage"] = "Статус записи успешно изменен";
            return RedirectToAction(nameof(AppointmentDetails), new { id });
        }

        // Фильтр записей по дате
        [HttpGet]
        public async Task<IActionResult> AppointmentsByDate(DateTime? date)
        {
            DateTime selectedDate;
            
            if (!date.HasValue)
            {
                selectedDate = DateTime.Today;
            }
            else
            {
                selectedDate = date.Value.Date;
            }
            
            DateTime startUtc = DateTime.SpecifyKind(selectedDate, DateTimeKind.Utc);
            DateTime endUtc = DateTime.SpecifyKind(selectedDate.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Car)
                .Include(a => a.Service)
                .Where(a => a.AppointmentDate >= startUtc && a.AppointmentDate <= endUtc)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            ViewBag.SelectedDate = selectedDate;
            return View(appointments);
        }
        
        // Список записей на сегодня
        [HttpGet]
        public async Task<IActionResult> TodayAppointments()
        {
            DateTime todayLocal = DateTime.Today;
            DateTime startUtc = DateTime.SpecifyKind(todayLocal, DateTimeKind.Utc);
            DateTime endUtc = DateTime.SpecifyKind(todayLocal.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            
            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Car)
                .Include(a => a.Service)
                .Where(a => a.AppointmentDate >= startUtc && a.AppointmentDate <= endUtc)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            ViewBag.SelectedDate = todayLocal;
            
            return View("AppointmentsByDate", appointments);
        }
        
        // Список клиентов с их записями
        public async Task<IActionResult> Clients(string searchTerm = null)
        {
            var clientsQuery = _context.Users
                .Where(u => u.Role == UserRole.Client)
                .Include(u => u.Cars)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                clientsQuery = clientsQuery.Where(u => 
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.PhoneNumber.Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.Cars.Any(c => c.LicensePlate.ToLower().Contains(searchTerm) ||
                                   c.Brand.ToLower().Contains(searchTerm) ||
                                   c.Model.ToLower().Contains(searchTerm)));
            }

            var clients = await clientsQuery
                .OrderBy(u => u.LastName)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            return View(clients);
        }
        
        // Детали клиента
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
    }
} 