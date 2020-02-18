using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WMTDashboard.Models;
using WMTServer;

namespace WMTDashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly WindmillFarm.WindmillFarmClient client;
        private readonly AccessTokenManager tokenManager;

        public HomeController(ILogger<HomeController> logger, WindmillFarm.WindmillFarmClient client, AccessTokenManager tokenManager)
        {
            this.logger = logger;
            this.client = client;
            this.tokenManager = tokenManager;
        }

        public async Task<IActionResult> Index()
        {
            var authHeader = new Metadata { {"Authorization", "Bearer " + (await tokenManager.GetApiTokenAsync()).AccessToken} };
            var response = await client.RequestListAsync(new WindmillListRequest(), authHeader);

            return View(response.Windmills.ToList());
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
    }
}