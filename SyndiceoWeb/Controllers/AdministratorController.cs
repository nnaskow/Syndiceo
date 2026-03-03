using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syndiceo.Data.Models;
using SyndiceoWeb.Models;

namespace SyndiceoWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdministratorController : Controller
    {
        private readonly SyndiceoDBContext _context;

        public AdministratorController(SyndiceoDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> AdminDashboard()
        {
            var model = new AdminDashboardViewModel
            {
              /*  TotalEntries = await _context.Entries.CountAsync(), // assuming you have an Entries table
                ActiveUsers = await _context.Users.CountAsync(u => u.LockoutEnd == null), // example for active users
                NewReports = await _context.Reports.CountAsync(r => !r.Processed), // example Reports table
                PendingConfirmations = await _context.Residences.CountAsync(r => !r.IsConfirmed) // Residence requests*/
            };

            return View(model);
        }

        public IActionResult ReportsList()
        {
            var model = new SyndiceoWeb.Views.Administrator.ReportListModel();

            // Примерни данни
            model.Reports = new List<SyndiceoWeb.Views.Administrator.Report>
    {
        new SyndiceoWeb.Views.Administrator.Report
        {
            Id = 1,
            UserId = "ivan.ivanov@example.com",
            Title = "Шум през нощта",
            Description = "Съседите правят шум след 23ч.",
            CreatedAt = DateTime.Now.AddDays(-1),
            Processed = false
        },
        new SyndiceoWeb.Views.Administrator.Report
        {
            Id = 2,
            UserId = "maria.georgieva@example.com",
            Title = "Счупен тротоар",
            Description = "Тротоарът пред блока е счупен.",
            CreatedAt = DateTime.Now.AddDays(-2),
            Processed = true
        }
    };

            return View(model);
        }
        public IActionResult ResidenceConfirmation()
        {
            var model = new SyndiceoWeb.Views.Administrator.ResidenceConfirmationModel
            {
                PendingRequests = new List<SyndiceoWeb.Views.Administrator.ResidenceRequest>
        {
            new SyndiceoWeb.Views.Administrator.ResidenceRequest
            {
                Id = 1,
                UserId = "ivan.ivanov@example.com",
                Address = "ул. „България“ 123, София",
                CreatedAt = DateTime.Now.AddDays(-1),
                IsConfirmed = false
            },
            new SyndiceoWeb.Views.Administrator.ResidenceRequest
            {
                Id = 2,
                UserId = "maria.georgieva@example.com",
                Address = "ул. „Витоша“ 45, София",
                CreatedAt = DateTime.Now.AddDays(-2),
                IsConfirmed = false
            }
        }
            };

            return View(model);
        }
    }
}