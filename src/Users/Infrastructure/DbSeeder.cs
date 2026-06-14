using Microsoft.Extensions.DependencyInjection;
using Users.Application.Interfaces;
using Users.Core;
using System.Linq;

namespace Users.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAdminUser(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<UsersDbContext>();
        var hasher = serviceProvider.GetRequiredService<IPasswordHasher>();

        if (!context.Users.Any(u => u.Role == UserRole.ADMIN))
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Name = "admin",
                Surname = "admin",
                Email = "admin@admin.com",
                PasswordHash = hasher.Hash("admin"),
                Role = UserRole.ADMIN,
                Sex = Sex.OTHER,
                DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                AvgRating = 0.0,
                RatingsCount = 0,
                IsBanned = false
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
