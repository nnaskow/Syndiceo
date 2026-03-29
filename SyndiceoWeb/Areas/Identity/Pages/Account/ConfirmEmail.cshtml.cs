// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Syndiceo.Data;
using SyndiceoWeb.Areas.Identity.Data;

namespace SyndiceoWeb.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        // ПРОМЯНА: Смени IdentityUser със SyndiceoWebUser
        private readonly UserManager<SyndiceoWebUser> _userManager;

        public ConfirmEmailModel(UserManager<SyndiceoWebUser> userManager)
        {
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);

            StatusMessage = result.Succeeded ? "Благодарим ви за потвърждението на вашия имейл." : "Грешка при потвърждение на имейла.";

            return Page();
        }
    }
}