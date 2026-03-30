using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Data.Models;
using SyndiceoWeb.Areas.Identity.Data;
using SyndiceoWeb.Models;
using System.Diagnostics;

namespace SyndiceoWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SyndiceoDBContext _context; 
        private readonly UserManager<SyndiceoWebUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            SyndiceoDBContext context,
            UserManager<SyndiceoWebUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    ViewBag.NewDiscussionsCount = await _context.Discussions
                        .CountAsync(d => d.CreatedAt > user.LastDiscussionsView);
                }
            }
            else
            {
                ViewBag.NewDiscussionsCount = 0;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult Capabilities()
        {
            return View();
        }
    }
}