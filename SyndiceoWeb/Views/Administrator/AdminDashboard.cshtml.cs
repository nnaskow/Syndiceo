using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Syndiceo.Data.Models;

namespace SyndiceoWeb.Views.Administrator
{
    public class AdminDashboardModel : PageModel
    {
        SyndiceoDBContext _context = new SyndiceoDBContext();
        public AdminDashboardModel(SyndiceoDBContext context)
        {
            _context = context;
        }
        public int AwaitingApproval { get; set; }
        public int NewSignals { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalEntrances { get; set; }
        public void OnGet()
        {
            AwaitingApproval = _context.Users.Count(u => !u.IsApproved);
            TotalEntrances = _context.Entrances.Count();
            NewSignals = _context.Reports.Count(r => !r.IsResolved);

        }
    }
}
