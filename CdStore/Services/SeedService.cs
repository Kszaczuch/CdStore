using CdStore.Models;
using Microsoft.AspNetCore.Identity;

namespace CdStore.Services
{
    public class SeedService
    {
        public static async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();

            try
            {
                logger.LogInformation("Ensuring the database is created");
                await context.Database.EnsureCreatedAsync();

                logger.LogInformation("Seeding roles");
                await AddRoleAsync(roleManager, "Admin");
                await AddRoleAsync(roleManager, "User");

                logger.LogInformation("Seeding admin user");
                var adminEmail = "kszaczuch@gmail.com";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var adminUser = new Users
                    {
                        FullName = "Kszaczuch",
                        Email = adminEmail,
                        NormalizedEmail = adminEmail.ToUpper(),
                        UserName = adminEmail,
                        NormalizedUserName = adminEmail.ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                    };

                    var result = await userManager.CreateAsync(adminUser, "Haslo123_");
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Assigning Admin role to the admin user");
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    } else
                    {
                        logger.LogError("Failed to create admin user {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error has occured while seeding the database");
            }
        }

        private static async Task AddRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName)) {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
