using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers
{
    [Authorize(Roles="Admini")]
    public class AppRolesController : BaseController
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public AppRolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }   
        //list all the roles created by Users

        public IActionResult Index()
        {
            var roles = _roleManager.Roles;

            return View(roles);
        }

        //Create a new role 
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        //Create a new role 


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(IdentityRole model)
        {
            if (!_roleManager.RoleExistsAsync(model.Name).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(model.Name)).GetAwaiter().GetResult();
            }
            return RedirectToAction("Index");
        }
    }
}
