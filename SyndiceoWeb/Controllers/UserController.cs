using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Data.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Policy;

namespace SyndiceoWeb.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly SyndiceoDBContext _context;
        public UserController(SyndiceoDBContext context)
        {
            _context = context;
        }

        public IActionResult Residences()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var myApartments = _context.Apartments
                .Include(a => a.Entrance)
                    .ThenInclude(e => e.Block)
                        .ThenInclude(b => b.Address)
                .Where(a => a.UserId == userId)
                .ToList();

            ViewBag.AvailableApartments = _context.Apartments
           .Include(a => a.Entrance)
               .ThenInclude(e => e.Block)
                   .ThenInclude(b => b.Address)
           .Where(a => a.UserId == null)
           .Select(a => new {
               Id = a.ApartmentId,
               Text = $"ул. {a.Entrance.Block.Address.Street} № {a.Entrance.Block.BlockName}, вх. {a.Entrance.EntranceName}, ап. {a.ApartmentNumber}"
           }).ToList();

            return View(myApartments);
        }

        [HttpPost]
        public async Task<IActionResult> AddResidence(string apartmentNumber, int floor)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Residences));
        }
        [HttpPost]
        public async Task<IActionResult> RequestApartment(int apartmentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var apartment = _context.Apartments.FirstOrDefault(a => a.ApartmentId == apartmentId);

            if (apartment != null && string.IsNullOrEmpty(apartment.UserId))
            {
                apartment.UserId = userId;
                apartment.IsConfirmed = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Заявката е изпратена успешно!";
            }

            return RedirectToAction("Residences");
        }

        public async Task<IActionResult> ApartmentTaxes(int apartmentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var apartment = await _context.Apartments
                 .Include(a => a.Entrance)
                     .ThenInclude(e => e.Block)
                         .ThenInclude(b => b.Address) 
                 .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId && a.UserId == userId);

            if (apartment == null || !apartment.IsConfirmed)
            {
                return RedirectToAction("Residences"); 
            }

            var debts = await _context.Debts
                .Where(d => d.ApartmentId == apartmentId)
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            var transactions = await _context.ApartmentTransactions
                .Include(t => t.Category)
                .Where(t => t.ApartmentId == apartmentId)
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            var entranceTransactions = await _context.EntranceTransactions
          .Include(t => t.Category)
          .Where(t => t.EntranceId == apartment.EntranceId)
          .OrderByDescending(t => t.TransDate)
          .ToListAsync();

            decimal balance = 0;
            foreach (var t in entranceTransactions)
            {
                if (t.Category?.Kind == "Приход")
                    balance += t.Amount;
                else if (t.Category?.Kind == "Разход")
                    balance -= t.Amount;
            }

            ViewBag.EntranceBalance = balance;
            ViewBag.EntranceTransactions = entranceTransactions;
            ViewBag.Apartment = apartment;
            ViewBag.Transactions = transactions;

            return View(debts);
        }
        public async Task<IActionResult> ApartmentHistory(int apartmentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var apartment = await _context.Apartments
                 .Include(a => a.Entrance)
                     .ThenInclude(e => e.Block)
                         .ThenInclude(b => b.Address)
                 .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId && a.UserId == userId);

            if (apartment == null || !apartment.IsConfirmed)
            {
                return RedirectToAction("Residences");
            }

            var transactions = await _context.ApartmentTransactions
                .Include(t => t.Category)
                .Where(t => t.ApartmentId == apartmentId)
                .OrderByDescending(t => t.TransDate)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            ViewBag.Apartment = apartment;
            return View(transactions);
        }
        public IActionResult Reports()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userReports = _context.Reports
                .Include(r => r.Entrance)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            ViewBag.MyEntrances = _context.Apartments
                .Where(a => a.UserId == userId && a.IsConfirmed)
                .Select(a => a.Entrance)
                .Distinct()
                .ToList();

            return View(userReports);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReport(string title, string description, int entranceId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var report = new Report
            {
                UserId = userId,
                EntranceId = entranceId,
                Title = title,
                Description = description,
                CreatedAt = DateTime.Now,
                IsResolved = false
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Сигналът е изпратен успешно!";
            return RedirectToAction(nameof(Reports));
        }
        [HttpPost]
        public async Task<IActionResult> EditReport(int reportId, string title, string description, int entranceId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == userId);

            if (report == null || report.IsResolved)
            {
                return RedirectToAction(nameof(Reports));
            }

            report.Title = title;
            report.Description = description;
            report.EntranceId = entranceId;

            report.isEdited = true;
            report.CreatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Сигналът беше актуализиран!";
            return RedirectToAction(nameof(Reports));
        }

    }
}