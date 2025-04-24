using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShinSerwis.Data;
using ShinSerwis.Models;
using ShinSerwis.Services;
using System.Security.Claims;
using System.Collections.Generic;

namespace ShinSerwis.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public AppointmentController(ApplicationDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // Список записей пользователя
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst("UserId");
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }
            
            var appointments = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Car)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        // Список всех записей для сотрудников с новыми записями сверху
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> AllAppointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Service)
                .Include(a => a.Car)
                .OrderBy(a => a.Status == AppointmentStatus.Pending ? 0 : 
                             (a.Status == AppointmentStatus.InProgress ? 1 : 
                             (a.Status == AppointmentStatus.Completed ? 2 : 3)))
                .ThenByDescending(a => a.Id)
                .ThenBy(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        // Отмена записи
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userIdClaim = User.FindFirst("UserId");
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }
            
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = AppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Просмотр деталей записи
        [HttpGet]
        public async Task<IActionResult> AppointmentDetails(int id)
        {
            int userId = int.Parse(User.FindFirstValue("UserId"));
            
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Car)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // Создание новой записи
        [HttpGet]
        public async Task<IActionResult> Create(int? serviceId = null)
        {
            
            try
            {
                int userId = int.Parse(User.FindFirstValue("UserId"));
                
                var services = await _context.Services
                    .Where(s => s.IsActive)
                    .ToListAsync();
                
                ViewBag.Services = services;
                
                var cars = await _context.Cars.Where(c => c.UserId == userId).ToListAsync();
                ViewBag.Cars = cars;
                
                var defaultDate = DateTime.Now.AddDays(1).Date.AddHours(10);
                defaultDate = DateTime.SpecifyKind(defaultDate, DateTimeKind.Utc);

                // Получаем информацию о занятости на ближайшие 14 дней
                var startDate = DateTime.UtcNow.Date;
                var endDate = startDate.AddDays(14);
                
                var appointmentsByDate = await _context.Appointments
                    .Where(a => a.AppointmentDate >= startDate && 
                                a.AppointmentDate < endDate &&
                                a.Status != AppointmentStatus.Cancelled)
                    .GroupBy(a => a.AppointmentDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();
                
                // Создаем словарь с количеством записей на каждый день
                var dailyAppointments = new Dictionary<DateTime, int>();
                foreach (var day in appointmentsByDate)
                {
                    dailyAppointments[day.Date] = day.Count;
                }
                
                // Максимальное количество записей на день
                ViewBag.MaxDailyAppointments = 10;
                ViewBag.DailyAppointments = dailyAppointments;
                
                var model = new Appointment { 
                    AppointmentDate = defaultDate
                };
                
                if (serviceId.HasValue)
                {
                    var service = services.FirstOrDefault(s => s.Id == serviceId.Value);
                    if (service != null)
                    {
                        model.ServiceId = service.Id;
                    }
                }
                
                return View(model);
            }
            catch (Exception ex)
            {
                return View(new Appointment { 
                    AppointmentDate = DateTime.SpecifyKind(DateTime.Now.AddDays(1).Date.AddHours(10), DateTimeKind.Utc)
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment Model)
        {
            try
            {
                int userId;
                if (!int.TryParse(User.FindFirstValue("UserId"), out userId))
                {
                    return RedirectToAction("Login", "Account");
                }
                
                int serviceId = 0;
                int carId = 0;
                DateTime appointmentDate = DateTime.Now;

                if (Request.Form.ContainsKey("ServiceId") && int.TryParse(Request.Form["ServiceId"], out serviceId))
                {
                    Console.WriteLine($"Успешно привязан ServiceId: {serviceId}");
                }
                else
                {
                    Console.WriteLine("Ошибка привязки ServiceId");
                    ModelState.AddModelError("ServiceId", "Выберите услугу");
                }

                if (Request.Form.ContainsKey("CarId") && int.TryParse(Request.Form["CarId"], out carId))
                {
                    Console.WriteLine($"Успешно привязан CarId: {carId}");
                }
                else
                {
                    Console.WriteLine("Ошибка привязки CarId");
                    ModelState.AddModelError("CarId", "Выберите автомобиль");
                }

                if (Request.Form.ContainsKey("AppointmentDate") && DateTime.TryParse(Request.Form["AppointmentDate"], out appointmentDate))
                {
                    // Преобразуем дату в UTC для PostgreSQL
                    if (appointmentDate.Kind != DateTimeKind.Utc)
                    {
                        appointmentDate = DateTime.SpecifyKind(appointmentDate, DateTimeKind.Utc);
                    }
                    Console.WriteLine($"Успешно привязана AppointmentDate: {appointmentDate} (Kind: {appointmentDate.Kind})");
                    
                    // Проверка на количество записей в выбранный день
                    var appointmentDate_Start = new DateTime(appointmentDate.Year, appointmentDate.Month, appointmentDate.Day, 0, 0, 0, DateTimeKind.Utc);
                    var appointmentDate_End = appointmentDate_Start.AddDays(1);
                    
                    var appointmentsCount = await _context.Appointments
                        .Where(a => a.AppointmentDate >= appointmentDate_Start && 
                                    a.AppointmentDate < appointmentDate_End &&
                                    a.Status != AppointmentStatus.Cancelled)
                        .CountAsync();
                    
                    if (appointmentsCount >= 10) // Максимум 10 записей в день
                    {
                        ModelState.AddModelError("AppointmentDate", "На эту дату уже нет свободных мест. Пожалуйста, выберите другую дату");
                        
                        ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
                        ViewBag.Cars = await _context.Cars.Where(c => c.UserId == userId).ToListAsync();
                        
                        return View(new Appointment 
                        { 
                            ServiceId = serviceId,
                            CarId = carId,
                            AppointmentDate = appointmentDate
                        });
                    }
                    
                    var selectedService = await _context.Services.FirstOrDefaultAsync(s => s.Id == serviceId);
                    if (selectedService != null)
                    {
                        int serviceDuration = selectedService.DurationMinutes;
                        
                        var appointmentTimeStart = appointmentDate;
                        var appointmentTimeEnd = appointmentDate.AddMinutes(serviceDuration);
                        
                        var appointmentsOnDay = await _context.Appointments
                            .Include(a => a.Service)
                            .Where(a => a.AppointmentDate >= appointmentDate_Start && 
                                        a.AppointmentDate < appointmentDate_End &&
                                        a.Status != AppointmentStatus.Cancelled)
                            .ToListAsync();
                        
                        foreach (var existingAppointment in appointmentsOnDay)
                        {
                            var existingStart = existingAppointment.AppointmentDate;
                            var existingEnd = existingAppointment.AppointmentDate.AddMinutes(existingAppointment.Service.DurationMinutes);
                            
                            if ((appointmentTimeStart >= existingStart && appointmentTimeStart < existingEnd) ||
                                (appointmentTimeEnd > existingStart && appointmentTimeEnd <= existingEnd) ||
                                (appointmentTimeStart <= existingStart && appointmentTimeEnd >= existingEnd))
                            {
                                ModelState.AddModelError("AppointmentDate", $"Выбранное время пересекается с другой записью. Интервал с {existingStart.ToString("HH:mm")} до {existingEnd.ToString("HH:mm")} уже занят.");
                                
                                ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
                                ViewBag.Cars = await _context.Cars.Where(c => c.UserId == userId).ToListAsync();
                                
                                return View(new Appointment 
                                { 
                                    ServiceId = serviceId,
                                    CarId = carId,
                                    AppointmentDate = appointmentDate
                                });
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Ошибка привязки AppointmentDate");
                    ModelState.AddModelError("AppointmentDate", "Укажите корректную дату");
                }

                if (serviceId <= 0 || carId <= 0 || appointmentDate <= DateTime.UtcNow)
                {
                    ModelState.AddModelError(string.Empty, "Все поля должны быть заполнены корректно");
                    
                    ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
                    ViewBag.Cars = await _context.Cars.Where(c => c.UserId == userId).ToListAsync();
                    
                    return View(new Appointment 
                    { 
                        ServiceId = serviceId,
                        CarId = carId,
                        AppointmentDate = appointmentDate
                    });
                }

                var service = await _context.Services
                    .FirstOrDefaultAsync(s => s.Id == serviceId);
                
                if (service == null || !service.IsActive)
                {
                    ModelState.AddModelError("ServiceId", "Выбранная услуга недоступна");
                    
                    ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
                    ViewBag.Cars = await _context.Cars.Where(c => c.UserId == userId).ToListAsync();
                    
                    return View(new Appointment 
                    { 
                        ServiceId = serviceId,
                        CarId = carId,
                        AppointmentDate = appointmentDate
                    });
                }
                
                var car = await _context.Cars
                    .FirstOrDefaultAsync(c => c.Id == carId && c.UserId == userId);
                
                if (car == null)
                {
                    ModelState.AddModelError("CarId", "Выбранный автомобиль не найден или не принадлежит вам");
                    
                    ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
                    ViewBag.Cars = await _context.Cars.Where(c => c.UserId == userId).ToListAsync();
                    
                    return View(new Appointment 
                    { 
                        ServiceId = serviceId,
                        CarId = carId,
                        AppointmentDate = appointmentDate
                    });
                }
                
                var newAppointment = new Appointment
                {
                    UserId = userId,
                    ServiceId = serviceId,
                    CarId = carId,
                    AppointmentDate = DateTime.SpecifyKind(appointmentDate, DateTimeKind.Utc),
                    Status = AppointmentStatus.Pending
                };

                _context.Appointments.Add(newAppointment);
                await _context.SaveChangesAsync();

                bool enableNotifications = true;

                if (enableNotifications)
                {
                    try
                    {
                        var completeAppointment = await _context.Appointments
                            .Include(a => a.User)
                            .Include(a => a.Car)
                            .Include(a => a.Service)
                            .FirstOrDefaultAsync(a => a.Id == newAppointment.Id);
                            
                        if (completeAppointment != null)
                        {
                            try {
                                
                                var employees = await _context.Users
                                    .Where(u => u.Role == UserRole.Employee || u.Role == UserRole.Admin)
                                    .ToListAsync();
                                    
                                
                                if (employees.Count > 0)
                                {
                                    var serviceInfo = await _context.Services
                                        .FirstOrDefaultAsync(s => s.Id == newAppointment.ServiceId);
                                        
                                    string serviceName = serviceInfo?.Name ?? "Неизвестная услуга";
                                    
                                    foreach (var employee in employees)
                                    {
                                        var notification = new Notification
                                        {
                                            UserId = employee.Id,
                                            Title = "Новая запись",
                                            Message = $"Новая запись на услугу {serviceName} на {newAppointment.AppointmentDate.ToString("dd.MM.yyyy HH:mm")}",
                                            Type = NotificationType.NewAppointment,
                                            AppointmentId = newAppointment.Id,
                                            CreatedAt = DateTime.UtcNow,
                                            IsRead = false
                                        };
                                        
                                        _context.Notifications.Add(notification);
                                    }
                                    
                                    await _context.SaveChangesAsync();
                                }
                            }
                            catch (Exception directEx) {
                            }
                        }
                    }
                    catch (Exception notifyEx)
                    {
                    }
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                
                ModelState.AddModelError(string.Empty, "Произошла ошибка: " + ex.Message);
                
                try
                {
                    int userId = int.Parse(User.FindFirstValue("UserId"));
                    ViewBag.Services = await _context.Services.Where(s => s.IsActive).ToListAsync();
                    ViewBag.Cars = await _context.Cars.Where(c => c.UserId == userId).ToListAsync();
                    
                    return View(new Appointment 
                    { 
                        AppointmentDate = DateTime.SpecifyKind(DateTime.Now.AddDays(1).Date.AddHours(10), DateTimeKind.Utc)
                    });
                }
                catch (Exception innerEx)
                {
                    return RedirectToAction("Index", "Home");
                }
            }
        }

        // Получение занятых слотов для выбранной даты и услуги
        [HttpGet]
        public async Task<JsonResult> GetTimeSlots(DateTime date, int serviceId)
        {
            try
            {
                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                var day_start = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
                var day_end = day_start.AddDays(1);
                
                var selectedService = await _context.Services.FirstOrDefaultAsync(s => s.Id == serviceId);
                if (selectedService == null)
                {
                    return Json(new { success = false, message = "Услуга не найдена" });
                }
                
                int serviceDuration = selectedService.DurationMinutes;
                
                var appointmentsOnDay = await _context.Appointments
                    .Include(a => a.Service)
                    .Where(a => a.AppointmentDate >= day_start && 
                                a.AppointmentDate < day_end &&
                                a.Status != AppointmentStatus.Cancelled)
                    .ToListAsync();
                
                var busyTimeSlots = new List<object>();
                foreach (var appointment in appointmentsOnDay)
                {
                    var start = appointment.AppointmentDate;
                    var end = start.AddMinutes(appointment.Service.DurationMinutes);
                    
                    busyTimeSlots.Add(new
                    {
                        start = start.ToString("HH:mm"),
                        end = end.ToString("HH:mm")
                    });
                }
                
                var workDayStart = new TimeSpan(9, 0, 0);
                var workDayEnd = new TimeSpan(20, 0, 0);
                var slotInterval = 30;
                
                var availableTimeSlots = new List<string>();
                for (var time = workDayStart; time <= workDayEnd; time = time.Add(TimeSpan.FromMinutes(slotInterval)))
                {
                    var slotStart = day_start.Add(time);
                    var slotEnd = slotStart.AddMinutes(serviceDuration);
                    
                    if (slotEnd.TimeOfDay > workDayEnd)
                    {
                        continue;
                    }
                    
                    bool isSlotAvailable = true;
                    foreach (var appointment in appointmentsOnDay)
                    {
                        var appointmentStart = appointment.AppointmentDate;
                        var appointmentEnd = appointmentStart.AddMinutes(appointment.Service.DurationMinutes);
                        
                        if ((slotStart >= appointmentStart && slotStart < appointmentEnd) ||
                            (slotEnd > appointmentStart && slotEnd <= appointmentEnd) ||
                            (slotStart <= appointmentStart && slotEnd >= appointmentEnd))
                        {
                            isSlotAvailable = false;
                            break;
                        }
                    }
                    
                    if (isSlotAvailable)
                    {
                        availableTimeSlots.Add(slotStart.ToString("HH:mm"));
                    }
                }
                
                return Json(new 
                { 
                    success = true, 
                    busySlots = busyTimeSlots,
                    availableSlots = availableTimeSlots,
                    serviceDuration = serviceDuration
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ошибка при получении временных слотов: " + ex.Message });
            }
        }
    }
} 