using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CPMCore.Models;
using CPMCore.Service;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using System.Linq;


namespace CPMCore.Controllers
{
    
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailSender _emailSender;
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            IEmailSender emailSender) // toegevoegd

        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }
        public IActionResult Index()
        {
            return View();
        }
        //GET : /Account/Login
        [AllowAnonymous]
        public async Task<ActionResult> Login(string returnUrl = null)
        {
            var model = new LoginViewModel
            {
                Input = new LoginViewModel.InputModel
                {
                    Username = string.Empty,
                    Password = string.Empty
                },
                ReturnUrl = returnUrl ?? Url.Content("~/")
            };


            if (!string.IsNullOrEmpty(model.ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, model.ErrorMessage);
            }
            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            model.ReturnUrl = returnUrl;

            return View(model);
        }

        //Post : /Account/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            model.ReturnUrl = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Input.Username, model.Input.Password, model.Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }

            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //GET : /Account/Register
        public ActionResult Register(string returnUrl)
        {
            RegisterViewModel model = new RegisterViewModel();
            model.ReturnUrl = returnUrl;
            return View(model);
        }
        //Post : /Account/Register 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser()
                {
                    UserName = model.Input.Username,
                    Forename = model.Input.Forename,
                    Name = model.Input.Name,
                    JobFunction = model.Input.JobFunction,
                    Email = model.Input.Email,
                    Cellphone = model.Input.Cellphone
                };
                var result = await _userManager.CreateAsync(user, model.Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return LocalRedirect(model.ReturnUrl);

                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //Get : /Account/AccssDenied    
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GEt: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("index", "home");
        }

        //GET : /Account/UserProfile
        public ActionResult UserProfile(string userId)
        {
            ApplicationUser user = _userManager.FindByIdAsync(userId).Result;   
            UpdateViewModel model = new UpdateViewModel();
            model.User = user;
            return View(model);
        }
        //Post : /Account/UserProfile 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserProfile(UpdateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _userManager.FindByIdAsync(model.User.Id).Result;
                {
                    user.Email = model.User.Email;
                    user.Cellphone = model.User.Cellphone;
                    user.Forename = model.User.Forename;
                    user.Name = model.User.Name;
                    user.UserName = model.User.UserName;
                    user.JobFunction = model.User.JobFunction;
                }
                ;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User information updated");

                    return View(model);

                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await FindFirstUserByEmailAsync(model.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { email = model.Email, code = token }, Request.Scheme);

                // Hier moet je je eigen emailservice toevoegen of een SMTP mail sturen via bijv. SmtpClient
                try
                {
                    await _emailSender.SendEmailAsync(model.Email, "Reset je wachtwoord",
                        $"Klik <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>hier</a> om je wachtwoord opnieuw in te stellen.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fout bij verzenden van resetmail voor gebruiker {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Er liep iets mis bij het verzenden van de resetmail. Probeer het opnieuw of contacteer de administrator.");
                    return View(model);
                }
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult KeepAlive()
        {
            return NoContent();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string? email, string? code)
        {
            if (email == null || code == null)
                return BadRequest("Ongeldige resetlink.");

            var user = await FindFirstUserByEmailAsync(email);
            var validToken = user != null && await _userManager.VerifyUserTokenAsync(
                user,
                _userManager.Options.Tokens.PasswordResetTokenProvider,
                "ResetPassword",
                code);

            if (user == null || !validToken)
            {
                TempData["ResetError"] = "De resetlink is ongeldig of verlopen. Vraag een nieuwe link aan.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel { Email = email, Code = code, Password = "", ConfirmPassword = "" };
            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await FindFirstUserByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        private async Task<ApplicationUser?> FindFirstUserByEmailAsync(string email)
        {
            var normalizedEmail = _userManager.NormalizeEmail(email);
            var matches = await _userManager.Users
                .Where(u => u.NormalizedEmail == normalizedEmail)
                .ToListAsync();

            if (matches.Count > 1)
            {
                _logger.LogWarning("Multiple users share the email address {Email}. Using the first match for password reset.", email);
            }

            return matches.FirstOrDefault();
        }
    }
}
