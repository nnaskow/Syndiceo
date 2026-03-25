using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Data.Models;

namespace SyndiceoWeb.Areas.Identity.Pages
{
    public class ContactModel : PageModel
    {
        SyndiceoDBContext _context = new SyndiceoDBContext();
        public ContactModel(SyndiceoDBContext context)
        {
            _context = context;
        }

        public string ContactEmail { get; set; }
        public void OnGet()
        {
            var loginInfo = _context.Logins.FirstOrDefault();
            ContactEmail = loginInfo?.Email ?? "support@syndiceo.bg";
        }
    }
}
