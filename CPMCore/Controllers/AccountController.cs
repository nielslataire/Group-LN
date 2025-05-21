using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CPMCore.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography.X509Certificates;


namespace CPMCore.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }
        //GET : /Account/Login
        [AllowAnonymous]
        public async Task<ActionResult> Login(string returnUrl = null)
        {
            LoginViewModel model = new LoginViewModel();


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
    }
}
