using LibraryAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace LibraryAPI.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<Member>>();

                // Roles banao
                string[] roles = { "Admin", "Member" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole(role));
                }

                // Default Admin banao
                var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
                var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

                if (adminEmail != null && adminPassword != null)
                {
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);
                    if (adminUser == null)
                    {
                        adminUser = new Member
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            FullName = "Super Admin",
                            EmailConfirmed = true,
                            JoinDate = DateTime.UtcNow
                        };

                        var result = await userManager.CreateAsync(adminUser, adminPassword);
                        if (result.Succeeded)
                            await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database seeding error: {ex.Message}");
            }
        }
    }
}