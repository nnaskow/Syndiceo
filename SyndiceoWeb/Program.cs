using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SyndiceoWeb.Areas.Identity.Data;
using SyndiceoWeb.Data;

namespace SyndiceoWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // SyndiceoDBContext вече съдържа Identity таблиците
            builder.Services.AddDbContext<SyndiceoDBContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Регистрираш Identity за твоя User class и твоя DB контекст
            builder.Services.AddDefaultIdentity<SyndiceoWebUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false; // true, ако искаш потвърждение
            })
            .AddEntityFrameworkStores<SyndiceoDBContext>();

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

            app.UseAuthentication(); // <- задължително
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
