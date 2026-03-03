using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syndiceo.Data.Models;
using System;
using System.Collections.Generic;

namespace SyndiceoWeb.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            var apartments = new List<Apartment>
    {
        new Apartment
        {
            Id = 1,
            Name = "Апартамент 1",
            Address = "ул. Пример 10",
            Entrances = new List<Entrance>
            {
                new Entrance { Id = 101, Name = "Вход А" },
                new Entrance { Id = 102, Name = "Вход Б" }
            }
        },
        new Apartment
        {
            Id = 2,
            Name = "Апартамент 2",
            Address = "ул. Пример 20",
            Entrances = new List<Entrance>
            {
                new Entrance { Id = 201, Name = "Вход А" }
            }
        }
    };

            return View(apartments);
        }

        public IActionResult ApartmentTaxes(int apartmentId)
        {
            var model = new ApartmentTaxesModel
            {
                ApartmentName = $"Апартамент {apartmentId}",
                EntranceTax = new ApartmentTaxesModel.TaxItem
                {
                    Description = "Такса за вход",
                    Amount = 50,
                    DueDate = DateTime.Today.AddDays(-10),
                    IsPaid = true
                },
                ApartmentTax = new ApartmentTaxesModel.TaxItem
                {
                    Description = "Такса за апартамент",
                    Amount = 30,
                    DueDate = DateTime.Today.AddDays(5),
                    IsPaid = false
                }
            };

            return View(model); // Views/User/ApartmentTaxes.cshtml
        }

        public IActionResult Reports()
        {
            var reports = new List<Report>
            {
                new Report { Id = 1, Date = DateTime.Today.AddDays(-3), Type = "Проблем", Description = "Нещо не работи", Status = "Отворен" },
                new Report { Id = 2, Date = DateTime.Today.AddDays(-1), Type = "Запитване", Description = "Въпрос относно таксите", Status = "Решен" }
            };

            return View(reports); // Views/User/Reports.cshtml
        }
        public IActionResult WhodApartments()
        {
            var whods = new List<EntranceWithApartments>
    {
        new EntranceWithApartments
        {
            Id = 1,
            Name = "Вход А",
            Apartments = new List<Apartment>
            {
                new Apartment { Id = 1, Name = "Апартамент 1", Address = "ул. Пример 10" },
                new Apartment { Id = 2, Name = "Апартамент 2", Address = "ул. Пример 12" }
            }
        },
        new EntranceWithApartments
        {
            Id = 2,
            Name = "Вход Б",
            Apartments = new List<Apartment>
            {
                new Apartment { Id = 3, Name = "Апартамент 3", Address = "ул. Пример 20" }
            }
        }
    };

            return View(whods); // Точно този тип view очаква
        }
        // --- Модели за MVC Views ---
        public class Apartment
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public List<Entrance> Entrances { get; set; } = new List<Entrance>();
        }
        public class Entrance
        {
            public int Id { get; set; }
            public string Name { get; set; } // Например "Вход А", "Вход Б"
        }

        public class ApartmentTaxesModel
        {
            public string ApartmentName { get; set; }
            public TaxItem EntranceTax { get; set; }
            public TaxItem ApartmentTax { get; set; }

            public class TaxItem
            {
                public string Description { get; set; }
                public decimal Amount { get; set; }
                public DateTime DueDate { get; set; }
                public bool IsPaid { get; set; }
            }
        }

        public class Report
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public string Type { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
        }
        public class EntranceWithApartments
        {
            public int Id { get; set; }
            public string Name { get; set; } // Името на входа
            public List<Apartment> Apartments { get; set; } = new List<Apartment>();
        }
    }
}