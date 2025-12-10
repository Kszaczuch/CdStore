using CdStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

                const string adminEmail = "admin@example.com";
                const string adminPassword = "Haslo123_";

                logger.LogInformation("Seeding admin user");
                var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

                if (existingAdmin != null)
                {
                    existingAdmin.FullName = "admin";
                    existingAdmin.Email = adminEmail;
                    existingAdmin.NormalizedEmail = adminEmail.ToUpper();
                    existingAdmin.UserName = adminEmail;
                    existingAdmin.NormalizedUserName = adminEmail.ToUpper();
                    existingAdmin.EmailConfirmed = true;
                    if (existingAdmin.DeliveryAddress == null)
                    {
                        existingAdmin.DeliveryAddress = string.Empty;
                    }

                    var updateResult = await userManager.UpdateAsync(existingAdmin);
                    if (updateResult.Succeeded)
                    {
                        logger.LogInformation("Updated existing admin user to {Email}", adminEmail);
                        if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
                        {
                            await userManager.AddToRoleAsync(existingAdmin, "Admin");
                        }
                    }
                    else
                    {
                        logger.LogError("Failed to update admin user {Errors}", string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    var adminUser = new Users
                    {
                        FullName = "admin",
                        Email = adminEmail,
                        NormalizedEmail = adminEmail.ToUpper(),
                        UserName = adminEmail,
                        NormalizedUserName = adminEmail.ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        DeliveryAddress = string.Empty
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Assigning Admin role to the admin user");
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                    else
                    {
                        logger.LogError("Failed to create admin user {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }

                const string userEmail = "user@example.com";
                logger.LogInformation("Seeding regular user");
                var existingUser = await userManager.FindByEmailAsync(userEmail);
                if (existingUser == null)
                {
                    var normalUser = new Users
                    {
                        FullName = "user",
                        Email = userEmail,
                        NormalizedEmail = userEmail.ToUpper(),
                        UserName = userEmail,
                        NormalizedUserName = userEmail.ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        DeliveryAddress = string.Empty
                    };

                    var userCreateResult = await userManager.CreateAsync(normalUser, adminPassword);
                    if (userCreateResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(normalUser, "User");
                        logger.LogInformation("Created regular user {Email}", userEmail);
                    }
                    else
                    {
                        logger.LogError("Failed to create regular user {Errors}", string.Join(", ", userCreateResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    if (existingUser.DeliveryAddress == null)
                    {
                        existingUser.DeliveryAddress = string.Empty;
                        var setAddrResult = await userManager.UpdateAsync(existingUser);
                        if (setAddrResult.Succeeded)
                        {
                            logger.LogInformation("Set empty DeliveryAddress for existing user {Email}", userEmail);
                        }
                        else
                        {
                            logger.LogError("Failed to set DeliveryAddress for {Email}: {Errors}", userEmail, string.Join(", ", setAddrResult.Errors.Select(e => e.Description)));
                        }
                    }

                    if (await userManager.IsInRoleAsync(existingUser, "Admin"))
                    {
                        var removed = await userManager.RemoveFromRoleAsync(existingUser, "Admin");
                        if (removed.Succeeded)
                        {
                            logger.LogInformation("Removed Admin role from existing user {Email}", userEmail);
                        }
                        else
                        {
                            logger.LogError("Failed to remove Admin role from {Email}: {Errors}", userEmail, string.Join(", ", removed.Errors.Select(e => e.Description)));
                        }
                    }

                    if (!await userManager.IsInRoleAsync(existingUser, "User"))
                    {
                        var addUserRole = await userManager.AddToRoleAsync(existingUser, "User");
                        if (addUserRole.Succeeded)
                        {
                            logger.LogInformation("Ensured User role for existing user {Email}", userEmail);
                        }
                        else
                        {
                            logger.LogError("Failed to add User role to {Email}: {Errors}", userEmail, string.Join(", ", addUserRole.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        logger.LogInformation("Regular user {Email} already exists and has correct roles", userEmail);
                    }
                }

                logger.LogInformation("Seeding music categories (genres)");
                var genres = new[]
                {
                    "Pop",
                    "Rock",
                    "Metal",
                    "Hip-Hop",
                    "Jazz",
                    "Blues",
                    "Electronic",
                    "Classical",
                    "Soul",
                    "Reggae"
                };

                var addedCount = 0;
                foreach (var g in genres)
                {
                    if (!context.Kategorie.Any(c => c.Nazwa == g))
                    {
                        context.Kategorie.Add(new Kategoria { Nazwa = g });
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    await context.SaveChangesAsync();
                    logger.LogInformation("Added {Count} music categories", addedCount);
                }
                else
                {
                    logger.LogInformation("Music categories already present; no changes made");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error has occured while seeding the database");
            }
        }

        private static async Task AddRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}