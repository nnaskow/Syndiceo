// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SyndiceoWeb.Areas.Identity.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace SyndiceoWeb.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<SyndiceoWebUser> _signInManager;
        private readonly UserManager<SyndiceoWebUser> _userManager;
        private readonly IUserStore<SyndiceoWebUser> _userStore;
        private readonly IUserEmailStore<SyndiceoWebUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<SyndiceoWebUser> userManager,
            IUserStore<SyndiceoWebUser> userStore,
            SignInManager<SyndiceoWebUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Имейл")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "Паролата {0} трябва да е  между {2} и {1} знака дълга.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Парола")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Потвърждаване на парола")]
            [Compare("Password", ErrorMessage = "Двете пароли не съвпадат.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Име")]
            public string FirstName { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Фамилия")]
            public string LastName { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.IsApproved = false;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    await _userManager.AddClaimAsync(user, new Claim("FirstName", Input.FirstName));
                    await _userManager.AddClaimAsync(user, new Claim("LastName", Input.LastName));

                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    string subject = "Потвърдете вашата регистрация в Syndiceo";

                    string htmlMessage = $@"
<div style='background-color: #f9fafb; padding: 40px 10px; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
    <div style='max-width: 600px; margin: auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05); border: 1px solid #e5e7eb;'>
        
        <div style='background: linear-gradient(135deg, #2C5282 0%, #1A365D 100%); padding: 30px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;'>Syndiceo</h1>
        </div>

        <div style='padding: 40px;'>
            <h2 style='color: #1a202c; margin-top: 0; font-size: 22px; font-weight: 700;'>Добре дошли, {Input.FirstName}!</h2>
            
            <p style='color: #4a5568; font-size: 16px; line-height: 1.6;'>
                Радваме се, че избрахте <strong>Syndiceo</strong> за управлението на вашата етажна собственост. За да активирате профила си, е необходимо да направите две малки стъпки:
            </p>

            <div style='margin: 30px 0; text-align: center;'>
                <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' 
                   style='background-color: #2b6cb0; color: #ffffff; padding: 14px 32px; text-decoration: none; border-radius: 12px; font-weight: 600; font-size: 16px; display: inline-block; box-shadow: 0 4px 10px rgba(43, 108, 176, 0.3);'>
                   Потвърди моя имейл
                </a>
            </div>

            <div style='background-color: #fffaf0; border-left: 4px solid #ed8936; padding: 20px; border-radius: 8px; margin-top: 30px;'>
                <p style='margin: 0; color: #7b341e; font-size: 14px; line-height: 1.5;'>
                    <strong><span style='font-size: 18px;'>⏳</span> Предстои одобрение:</strong><br>
                    След като потвърдите имейла си, вашият <strong>домоуправител</strong> ще получи известие, за да одобри достъпа ви. Това е мярка за сигурност, която гарантира, че само реални обитатели имат достъп до данните на входа.
                </p>
            </div>

            <p style='color: #718096; font-size: 13px; margin-top: 40px; text-align: center; border-top: 1px solid #edf2f7; padding-top: 20px;'>
                Ако бутонът не работи, копирайте този линк в браузъра си:<br>
                <span style='color: #2b6cb0;'>{callbackUrl}</span>
            </p>
        </div>

        <div style='background-color: #f7fafc; padding: 20px; text-align: center; border-top: 1px solid #edf2f7;'>
            <p style='color: #a0aec0; font-size: 12px; margin: 0;'>
                &copy; {DateTime.Now.Year} Syndiceo. Всички права запазени.
            </p>
        </div>
    </div>
</div>";
                    await _emailSender.SendEmailAsync(Input.Email, subject, htmlMessage);
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }
        private SyndiceoWebUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<SyndiceoWebUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(SyndiceoWebUser)}'. " +
                    $"Ensure that '{nameof(SyndiceoWebUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<SyndiceoWebUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<SyndiceoWebUser>)_userStore;
        }
    }
}
