using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SyndiceoWeb.Areas.Identity.Data;
using Syndiceo.Data.Models;
using SyndiceoDBContext = Syndiceo.Data.Models.SyndiceoDBContext;

namespace SyndiceoWeb
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<SyndiceoDBContext>(options =>
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly("Syndiceo.Data")));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentity<SyndiceoWebUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<SyndiceoDBContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                try
                {
                    var context = services.GetRequiredService<SyndiceoDBContext>();
                    logger.LogInformation("Стартиране на миграции...");
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Миграциите приключиха успешно.");

                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = services.GetRequiredService<UserManager<SyndiceoWebUser>>();

                    await SeedRolesAndAdmin(roleManager, userManager, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "КРИТИЧНА ГРЕШКА ПРИ СТАРТ:");
                    if (app.Environment.IsDevelopment()) throw;
                }
            }
            await app.RunAsync();
        }

        private static async Task SeedRolesAndAdmin(
            RoleManager<IdentityRole> roleManager,
            UserManager<SyndiceoWebUser> userManager,
            ILogger logger)
        {
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    logger.LogInformation($"Създаване на роля: {role}");
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = "admin@syndiceo.net";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser != null)
            {
                if (!adminUser.IsApproved)
                {
                    try
                    {
                        adminUser.IsApproved = true;
                        await userManager.UpdateAsync(adminUser);
                        logger.LogInformation("Статусът на администратора е актуализиран на IsApproved = true.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Грешка при актуализиране на админа: {ex.Message}");
                    }
                }
                logger.LogInformation("Админът вече съществува в базата.");
            }
            else
            {
                logger.LogInformation("Админът не е намерен. Опит за създаване...");
                var newAdmin = new SyndiceoWebUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
/*                    LastName = "Системен администратор",
*/                    IsApproved = true
                };

                var result = await userManager.CreateAsync(newAdmin, "Admin123!");

                if (result.Succeeded)
                {
                    logger.LogInformation("Админът е създаден успешно. Приписване на роля...");
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogError($"ГРЕШКА ПРИ СЪЗДАВАНЕ НА АДМИН: {errors}");
                    throw new Exception($"Identity Error: {errors}");
                }
            }
        }
    }
}