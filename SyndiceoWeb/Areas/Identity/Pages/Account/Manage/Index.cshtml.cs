#nullable disable
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SyndiceoWeb.Areas.Identity.Data;

namespace SyndiceoWeb.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<SyndiceoWebUser> _userManager;
        private readonly SignInManager<SyndiceoWebUser> _signInManager;

        public IndexModel(
            UserManager<SyndiceoWebUser> userManager,
            SignInManager<SyndiceoWebUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Името е задължително")]
            [Display(Name = "Име")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Фамилията е задължителна")]
            [Display(Name = "Фамилия")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Имейлът е задължителен")]
            [EmailAddress(ErrorMessage = "Невалиден имейл формат")]
            [Display(Name = "Имейл")]
            public string Email { get; set; }
        }

        private async Task LoadAsync(SyndiceoWebUser user)
        {
            var userFromDb = await _userManager.FindByIdAsync(user.Id);

            Username = userFromDb.UserName;

            Input = new InputModel
            {
                FirstName = userFromDb.FirstName, 
                LastName = userFromDb.LastName,
                Email = userFromDb.Email
            };
        }
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Неуспешно зареждане на потребител.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Грешка при зареждане.");

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;

            if (Input.Email != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
                var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.Email);

                if (!setEmailResult.Succeeded || !setUserNameResult.Succeeded)
                {
                    StatusMessage = "Грешка при смяна на имейла.";
                    return RedirectToPage();
                }
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Профилът е обновен!";
            }
            else
            {
                StatusMessage = "Грешка при запис в базата.";
            }

            return RedirectToPage();
        }
    }
}