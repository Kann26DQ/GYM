using Microsoft.AspNetCore.Mvc;

namespace GYM.Controllers
{
    public class SuperAdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
