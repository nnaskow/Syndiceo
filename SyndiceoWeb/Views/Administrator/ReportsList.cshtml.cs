using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SyndiceoWeb.Views.Administrator
{
    public class Report
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Processed { get; set; }
    }

    public class ReportListModel : PageModel
    {
        public IList<Report> Reports { get; set; }

        public void OnGet()
        {
            // Примерни данни
            Reports = new List<Report>
            {
                new Report
                {
                    Id = 1,
                    UserId = "ivan.ivanov@example.com",
                    Title = "Шум през нощта",
                    Description = "Съседите правят шум след 23ч.",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    Processed = false
                },
                new Report
                {
                    Id = 2,
                    UserId = "maria.georgieva@example.com",
                    Title = "Счупен тротоар",
                    Description = "Тротоарът пред блока е счупен.",
                    CreatedAt = DateTime.Now.AddDays(-2),
                    Processed = true
                },
                new Report
                {
                    Id = 3,
                    UserId = "petar.petrov@example.com",
                    Title = "Липсва улично осветление",
                    Description = "На улица „Витоша“ няма осветление.",
                    CreatedAt = DateTime.Now.AddDays(-3),
                    Processed = false
                }
            };
        }
    }
}