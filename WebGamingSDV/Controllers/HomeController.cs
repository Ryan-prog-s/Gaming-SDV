using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Compute;
using Microsoft.AspNetCore.Mvc;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using System.Diagnostics;
using WebGamingSDV.Models;
using Azure.Core;
using Azure;
using Azure.ResourceManager.Compute.Models;

namespace WebGamingSDV.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
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

        public IActionResult Login()
        {
            string url = "/Identity/Account/Login";
            return Redirect(url);
        }
    }
}