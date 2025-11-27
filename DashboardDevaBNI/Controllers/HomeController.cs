using DashboardDevaBNI.Component;
using DashboardDevaBNI.Models;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DashboardDevaBNI.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbDashboardDevaBniContext _context;
        private readonly IConfiguration _configuration;
        private readonly IConverter _converter;
        private readonly LastSessionLog lastSession;
        private readonly AccessSecurity accessSecurity;
        public HomeController(IConfiguration config, DbDashboardDevaBniContext context, IHttpContextAccessor accessor)
        {
            _context = context;
            _configuration = config;
            lastSession = new LastSessionLog(accessor, context, config);
            accessSecurity = new AccessSecurity(accessor, context, config);
        }

        public IActionResult Index()
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Path = location.AbsolutePath;
            if (!accessSecurity.IsGetAccess(".." + Path))
            {
                return RedirectToAction("NotAccess", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Error");
            }

            ViewBag.CurrentPath = Path;
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
