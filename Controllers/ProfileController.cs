using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShinSerwis.Data;
using ShinSerwis.Models;
using ShinSerwis.ViewModels;
using System.Security.Claims;

namespace ShinSerwis.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var cars = await _context.Cars
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var appointments = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Car)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                User = user,
                Cars = cars,
                Appointments = appointments
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Edit()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var user = _context.Users.Find(userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = int.Parse(User.FindFirstValue("UserId"));
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound();
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult AddCar()
        {
            if (User.IsInRole("Employee"))
            {
                TempData["ErrorMessage"] = "Сотрудники не могут добавлять автомобили";
                return RedirectToAction(nameof(Index));
            }
            
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/Profile/AddCarDirect")]
        public async Task<IActionResult> AddCarDirect(string Brand, string Model, string LicensePlate, string Year, string VIN)
        {
            if (User.IsInRole("Employee"))
            {
                TempData["ErrorMessage"] = "Сотрудники не могут добавлять автомобили";
                return RedirectToAction(nameof(Index));
            }
                        
            var model = new AddCarViewModel
            {
                Brand = Brand,
                Model = Model,
                LicensePlate = LicensePlate,
                Year = Year,
                VIN = string.IsNullOrWhiteSpace(VIN) ? null : VIN
            };
            
            if (string.IsNullOrWhiteSpace(Brand))
                ModelState.AddModelError("Brand", "Марка автомобиля обязательна");
                
            if (string.IsNullOrWhiteSpace(Model))
                ModelState.AddModelError("Model", "Модель автомобиля обязательна");
                
            if (string.IsNullOrWhiteSpace(LicensePlate))
                ModelState.AddModelError("LicensePlate", "Гос. номер обязателен");
                
            if (string.IsNullOrWhiteSpace(Year))
                ModelState.AddModelError("Year", "Год выпуска обязателен");
                
            if (!string.IsNullOrWhiteSpace(VIN) && VIN.Length != 17)
                ModelState.AddModelError("VIN", "VIN-код должен состоять из 17 символов");
            
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = int.Parse(User.FindFirstValue("UserId"));
                    
                    var existingCar = await _context.Cars.FirstOrDefaultAsync(c => c.LicensePlate == model.LicensePlate);
                    if (existingCar != null)
                    {
                        ModelState.AddModelError("LicensePlate", "Автомобиль с таким гос. номером уже существует в системе");
                        return View("AddCar", model);
                    }
                    
                    var car = new Car
                    {
                        UserId = userId,
                        Brand = model.Brand,
                        Model = model.Model,
                        LicensePlate = model.LicensePlate,
                        Year = model.Year,
                        VIN = null
                    };
                    
                    if (!string.IsNullOrWhiteSpace(model.VIN))
                    {
                        string vinTrimmed = model.VIN.Trim();
                        if (vinTrimmed.Length == 17)
                        {
                            car.VIN = vinTrimmed;
                        }
                        else
                        {
                            ModelState.AddModelError("VIN", "VIN-код должен состоять из 17 символов");
                            return View("AddCar", model);
                        }
                    }

                _context.Cars.Add(car);
                await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Произошла ошибка при добавлении автомобиля");
                    return View("AddCar", model);
                }
            }
            
            return View("AddCar", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditCar(int id)
        {
            if (User.IsInRole("Employee"))
            {
                TempData["ErrorMessage"] = "Сотрудники не могут редактировать автомобили";
                return RedirectToAction(nameof(Index));
            }
            
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
            if (car == null)
            {
                return NotFound();
            }
            
            var model = new AddCarViewModel
            {
                Id = car.Id,
                Brand = car.Brand,
                Model = car.Model,
                Year = car.Year,
                LicensePlate = car.LicensePlate,
                VIN = car.VIN
            };
            
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCar(int id, AddCarViewModel model)
        {
            if (User.IsInRole("Employee"))
            {
                TempData["ErrorMessage"] = "Сотрудники не могут редактировать автомобили";
                return RedirectToAction(nameof(Index));
            }
            
            ViewBag.CarId = id;
            
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = int.Parse(User.FindFirstValue("UserId"));
                    var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
                    
                    if (car == null)
                    {
                        return NotFound();
                    }
                    
                    var existingCar = await _context.Cars.FirstOrDefaultAsync(c => c.LicensePlate == model.LicensePlate && c.Id != id);
                    if (existingCar != null)
                    {
                        return View(model);
                    }
                    
                    car.Brand = model.Brand;
                    car.Model = model.Model;
                    car.LicensePlate = model.LicensePlate;
                    car.Year = model.Year;
                    
                    if (string.IsNullOrWhiteSpace(model.VIN))
                    {
                        car.VIN = null;
                    }
                    else
                    {
                        string vinTrimmed = model.VIN.Trim();
                        if (vinTrimmed.Length == 17)
                        {
                            car.VIN = vinTrimmed;
                        }
                        else
                        {
                            ModelState.AddModelError("VIN", "VIN-код должен состоять из 17 символов");
                            return View(model);
                        }
                    }
                    
                    _context.Update(car);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Автомобиль успешно обновлен";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Произошла ошибка при обновлении автомобиля: {ex.Message}");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCar(int id)
        {
            if (User.IsInRole("Employee"))
            {
                TempData["ErrorMessage"] = "Сотрудники не могут удалять автомобили";
                return RedirectToAction(nameof(Index));
            }
            
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var car = await _context.Cars
                .Include(c => c.Appointments.Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.InProgress))
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
            if (car == null)
            {
                return NotFound();
            }
            
            if (car.Appointments.Any())
            {
                TempData["ErrorMessage"] = "Невозможно удалить автомобиль, так как он используется в активных записях на обслуживание";
                return RedirectToAction(nameof(Index));
            }
            
            try
            {
                _context.Cars.Remove(car);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Автомобиль успешно удален";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при удалении автомобиля: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 