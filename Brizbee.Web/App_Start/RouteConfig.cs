using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Brizbee
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "DefaultMvcRoute",
                url: "mvc/{controller}/{action}/{id}",
                defaults: new { action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                "ErrorRoute",
                "QuickAuth/Error/{errMsg}",
                new { controller = "QuickAuth", action = "Error", errMsg = UrlParameter.Optional }
            );
        }
    }
}

