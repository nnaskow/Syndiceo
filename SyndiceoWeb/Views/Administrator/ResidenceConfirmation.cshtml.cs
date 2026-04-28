using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SyndiceoWeb.Views.Administrator
{
    public class ResidenceRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsConfirmed { get; set; }
    }

    public class ResidenceConfirmationModel : PageModel
    {
        public IList<ResidenceRequest> PendingRequests { get; set; }

        public void OnGet()
        {
            PendingRequests = new List<ResidenceRequest>
            {
                new ResidenceRequest
                {
                    Id = 1,
                    UserId = "ivan.ivanov@example.com",
                    Address = "ул. „България“ 123, София",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    IsConfirmed = false
                },
                new ResidenceRequest
                {
                    Id = 2,
                    UserId = "maria.georgieva@example.com",
                    Address = "ул. „Витоша“ 45, София",
                    CreatedAt = DateTime.Now.AddDays(-2),
                    IsConfirmed = false
                }
            };
        }
    }
}