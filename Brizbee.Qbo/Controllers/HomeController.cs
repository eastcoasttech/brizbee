using Brizbee.Qbo.Models;
using Microsoft.AspNetCore.Mvc;
using QuickBooksSharp;
using QuickBooksSharp.Entities;
using System.Diagnostics;

namespace Brizbee.Qbo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string _clientId = "ABadV9aKFYitPuja5pBltYuB5EnfxT8CFvpu27qxdIVTrGY8Ie";
        private readonly string _clientSecret = "pxsUULudiSY8zrAVqpcnDUxfP5c1r5yHnzkD5BZr";
        private readonly string _redirectUrl = "http://localhost:5145/Home/AuthResult";
        private string _accessToken = string.Empty;
        private string _code = string.Empty;
        private long _realmId;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Connect()
        {
            var authService = new AuthenticationService();
            var scopes = new[] { "com.intuit.quickbooks.accounting" };
            var state = Guid.NewGuid().ToString();
            var url = authService.GenerateAuthorizationPromptUrl(_clientId, scopes, _redirectUrl, state);
            // Redirect the user to redirectUrl so that they can approve the connection

            return Redirect(url);
        }

        [HttpGet]
        public async Task<IActionResult> AuthResult(string code, long realmId, string state)
        {
            //validate state parameter
            var authService = new AuthenticationService();
            var result = await authService.GetOAuthTokenAsync(_clientId, _clientSecret, code, _redirectUrl);
            //persist access token and refresh token
            _accessToken = result.access_token;
            _code = code;
            _realmId = realmId;

            return View();
        }
        
        [HttpGet]
        public async Task<IActionResult> SyncCustomers()
        {
            var dataService = new DataService(_accessToken, _realmId, useSandbox: true);

            var result = await dataService.PostAsync(new Customer
            {
                DisplayName = "Chandler Bing",
                Suffix = "Jr",
                Title = "Mr",
                MiddleName = "Muriel",
                FamilyName = "Bing",
                GivenName = "Chandler",
            });
            
            //result.Response is of type Customer
            var customer = result.Response;
            
            return View();
        }

        public IActionResult Index()
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
