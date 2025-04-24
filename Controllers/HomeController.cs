using Microsoft.AspNetCore.Mvc;
using ShinSerwis.Models;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ShinSerwis.Data;

namespace ShinSerwis.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var activeServices = await _context.Services
                .Where(s => s.IsActive)
                .Take(3)
                .ToListAsync();
                
            return View(activeServices);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
