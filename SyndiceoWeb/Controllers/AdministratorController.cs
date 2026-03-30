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
            ViewBag.TotalApartments = _context.Apartments.Count();
            ViewBag.OccupiedApartments = _context.Apartments.Count(a => a.UserId != null);
            ViewBag.FreeApartments = _context.Apartments.Count(a => a.UserId == null);
            ViewBag.ActiveUsers = _context.Users.Count(u => u.IsApproved);
            ViewBag.PendingUsers = _context.Users.Count(u => !u.IsApproved);
            ViewBag.PendingResidences = _context.Apartments.Count(a => a.UserId != null && !a.IsConfirmed);
            ViewBag.NewSignals = _context.Reports.Count(r => !r.IsResolved);
            ViewBag.ResolvedSignals = _context.Reports.Count(r => r.IsResolved);


            var pendingUsers = _context.Users
                    .Where(u => !u.IsApproved)
                    .OrderByDescending(u => u.Id)
                    .Take(3)
                    .ToList()
                    .Select(u => new ActivityLogViewModel
                    {
                        Title = "Нова регистрация",
                        Description = $"Потребител {u.Email} чака одобрение.",
                        Icon = "bi-person-plus-fill",
                        Color = "#805AD5",
                        BgColor = "#F3E8FF",
                        RawDate = DateTime.Now 
                    });

            var pendingResidences = _context.Apartments
                .Include(a => a.User)
                .Include(a => a.Entrance)
                .ThenInclude(b=>b.Block)
                .ThenInclude(e=>e.Address)
                .Where(a => a.UserId != null && !a.IsConfirmed)
                .OrderByDescending(a => a.ApartmentId)
                .Take(3)
                .ToList()
               .Select(a => {
                   var street = a.Entrance?.Block?.Address.Street;
                   var block = a.Entrance?.Block?.BlockName != null ? $"{a.Entrance.Block.BlockName}" : "";
                   var entrance = a.Entrance?.EntranceName != null ? $"вх. {a.Entrance.EntranceName}" : "";

                   return new ActivityLogViewModel
                   {
                       Title = "Заявка за адрес",
                       Description = $"ул. {street} № {block} {entrance}, ап. {a.ApartmentNumber}".Replace(" ,", "").Trim(new char[] { ' ', ',' }),
                       Icon = "bi-geo-alt-fill",
                       Color = "#DD6B20",
                       BgColor = "#FFFAF0",
                       RawDate = DateTime.Now
                   };
               });

            var recentReports = _context.Reports
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .ToList()
                .Select(r => new ActivityLogViewModel
                {
                    Title = "Нов сигнал",
                    Description = r.Title.Length > 40 ? r.Title.Substring(0, 40) + "..." : r.Title,
                    Icon = "bi-exclamation-triangle-fill",
                    Color = "#E53E3E", 
                    BgColor = "#FFF5F5",
                    RawDate = r.CreatedAt
                });

            var allActivities = pendingUsers
                .Concat(pendingResidences)
                .Concat(recentReports)
                .OrderByDescending(a => a.RawDate)
                .Take(6)
                .ToList();

            return View(allActivities);
        }
        public IActionResult ManageUsers(string searchTerm)
        {
            var usersQuery = _context.Users.Where(u => u.IsApproved).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                usersQuery = usersQuery.Where(u => u.Email.Contains(searchTerm));
            }

            var users = usersQuery.ToList();

            var confirmedUserIds = _context.Apartments
                .Where(a => a.IsConfirmed && a.UserId != null)
                .Select(a => a.UserId)
                .Distinct()
                .ToList();

            ViewBag.ConfirmedUserIds = confirmedUserIds;
            ViewBag.TotalActive = users.Count;
            ViewBag.CurrentSearch = searchTerm;

            return View(users);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            if (user.Email == "admin@syndiceo.net")
            {
                TempData["Error"] = "Системният администратор не може да бъде изтрит!";
                return RedirectToAction(nameof(ManageUsers));
            }

            var userApartments = await _context.Apartments
                .Where(a => a.UserId == userId)
                .ToListAsync();

            foreach (var apt in userApartments)
            {
                apt.UserId = null;
                apt.IsConfirmed = false;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Потребителят е изтрит успешно.";
            return RedirectToAction(nameof(ManageUsers));
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
                .ThenInclude(e => e.Block)
                .ThenInclude(b => b.Address)
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
        public IActionResult ManageResidences(string searchTerm)
        {
            var apartmentsQuery = _context.Apartments
                .Include(a => a.User)
                .Include(a => a.Entrance)
                    .ThenInclude(e => e.Block)
                        .ThenInclude(b => b.Address)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                apartmentsQuery = apartmentsQuery.Where(a =>
                    a.User.Email.Contains(searchTerm) ||
                    a.Entrance.Block.Address.Street.Contains(searchTerm) ||
                    a.ApartmentNumber.ToString() == searchTerm);
            }

            var residences = apartmentsQuery
                .OrderBy(a => a.Entrance.Block.Address.Street)
                .ThenBy(a => a.Entrance.Block.BlockName)
                .ThenBy(a => a.ApartmentNumber)
                .ToList();

            ViewBag.CurrentSearch = searchTerm;
            return View(residences);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveUserFromResidence(int apartmentId)
        {
            var apartment = await _context.Apartments.FindAsync(apartmentId);
            if (apartment != null)
            {
                apartment.UserId = null;
                apartment.IsConfirmed = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Потребителят е премахнат от адреса.";
            }
            return RedirectToAction(nameof(ManageResidences));
        }
    }
    public class ActivityLogViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Time { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public string BgColor { get; set; }
        public DateTime RawDate { get; set; }

        public string GetTimeAgo()
        {
            var diff = DateTime.Now - RawDate;

            if (diff.TotalSeconds < 60) return "сега";
            if (diff.TotalMinutes < 60) return $"преди {Math.Floor(diff.TotalMinutes)} мин.";
            if (diff.TotalHours < 24) return $"преди {Math.Floor(diff.TotalHours)} ч.";
            if (diff.TotalDays < 7) return $"преди {Math.Floor(diff.TotalDays)} дни";

            return RawDate.ToString("dd.MM.yyyy");
        }
    }
}