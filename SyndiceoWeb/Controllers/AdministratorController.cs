using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public IActionResult AdminDashboard()
        {
            ViewBag.TotalEntrances = _context.Entrances.Count();
            ViewBag.ActiveUsers = _context.Users.Count(u => u.IsApproved);
            ViewBag.AwaitingApproval = _context.Users.Count(u => !u.IsApproved);
            ViewBag.NewSignals = _context.Reports.Count(r=> !r.IsResolved);

            return View();
        }
        public IActionResult ManageUsers(string searchTerm)
        {
            var users = _context.Users.Where(u => u.IsApproved).ToList();

            var confirmedUserIds = _context.Apartments
                .Where(a => a.IsConfirmed && a.UserId != null)
                .Select(a => a.UserId)
                .Distinct()
                .ToList();

            ViewBag.ConfirmedUserIds = confirmedUserIds; 
            ViewBag.TotalActive = users.Count;

            return View(users);
        }
        public IActionResult UserApproval()
        {
            var pendingUsers = _context.Users.Where(u => !u.IsApproved).ToList();
            ViewBag.Pendings= pendingUsers.Count;
            return View(pendingUsers);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUserAccount(string userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.IsApproved = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(UserApproval));
        }
        [HttpPost]
        public async Task<IActionResult> RejectUser(string userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(UserApproval));
        }
        public IActionResult ManageReports(string statusFilter)
        {
            var reportsQuery = _context.Reports
                .Include(r => r.User)
                .Include(r => r.Entrance)
                .AsQueryable();

            if (statusFilter == "resolved")
                reportsQuery = reportsQuery.Where(r => r.IsResolved);
            else if (statusFilter == "pending")
                reportsQuery = reportsQuery.Where(r => !r.IsResolved);

            var reports = reportsQuery.OrderByDescending(r => r.CreatedAt).ToList();

            ViewBag.CurrentFilter = statusFilter;
            return View(reports);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleReportStatus(int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report != null)
            {
                report.IsResolved = !report.IsResolved;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageReports));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReport(int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report != null)
            {
                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageReports));
        }

        public IActionResult ResidenceConfirmation()
        {
            var pendingRequests = _context.Apartments
                .Include(a => a.User) 
                .Include(a => a.Entrance)
                .Where(a => a.UserId != null && !a.IsConfirmed)
                .ToList();

            return View(pendingRequests);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmResidence(int apartmentId)
        {
            var apartment = _context.Apartments.FirstOrDefault(a => a.ApartmentId == apartmentId);
            if (apartment != null)
            {
                apartment.IsConfirmed = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ResidenceConfirmation));
        }
    }
}