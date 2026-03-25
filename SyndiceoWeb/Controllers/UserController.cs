using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                .Where(a => a.UserId == null)
                .Select(a => new {
                    Id = a.ApartmentId,
                    Text = $"Вход: {a.Entrance.EntranceName}, Ап. {a.ApartmentNumber}"
                }).ToList();

            return View(myApartments);
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
        [HttpPost]
        public async Task<IActionResult> AddResidence(string apartmentNumber, int floor)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Residences));
        }

        public async Task<IActionResult> ApartmentTaxes(int apartmentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var apartment = await _context.Apartments
                .Include(a => a.Entrance)
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

            ViewBag.Apartment = apartment;
            ViewBag.Transactions = transactions;

            return View(debts);
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

    }
}