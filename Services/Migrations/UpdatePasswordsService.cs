using Microsoft.EntityFrameworkCore;
using ShinSerwis.Data;
using ShinSerwis.Models;

namespace ShinSerwis.Services.Migrations
{
    public class UpdatePasswordsService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UpdatePasswordsService> _logger;

        public UpdatePasswordsService(
            IServiceScopeFactory scopeFactory,
            ILogger<UpdatePasswordsService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task MigratePasswords()
        {
            _logger.LogInformation("Запуск миграции паролей");
            
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

                var users = await dbContext.Users.ToListAsync();
                _logger.LogInformation($"Найдено {users.Count} пользователей для обновления паролей");

                int updatedCount = 0;
                
                foreach (var user in users)
                {
                    if (!IsPasswordHashed(user.Password))
                    {
                        string plainPassword = user.Password;
                        user.Password = passwordHasher.HashPassword(plainPassword);
                        updatedCount++;
                    }
                }

                if (updatedCount > 0)
                {
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation($"Успешно обновлены пароли для {updatedCount} пользователей");
                }
                else
                {
                    _logger.LogInformation("Не найдено паролей для обновления");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при миграции паролей");
                throw;
            }
        }

        private bool IsPasswordHashed(string password)
        {
            return password.Contains(':') && password.Split(':').Length == 3;
        }
    }
} 