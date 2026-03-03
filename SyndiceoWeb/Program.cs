using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SyndiceoWeb.Areas.Identity.Data;
using SyndiceoWeb.Data;
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
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentity<SyndiceoWebUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
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
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<SyndiceoWebUser>>();

                await SeedRolesAndAdmin(roleManager, userManager);
            }
            app.Run();
        }
        private static async Task SeedRolesAndAdmin(
    RoleManager<IdentityRole> roleManager,
    UserManager<SyndiceoWebUser> userManager)
        {
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = "admin@syndiceo.net";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new SyndiceoWebUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newAdmin, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}
