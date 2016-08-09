using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace PayDesk
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                    name: "PayDesk",
                    routeTemplate: "{*.uri}",
                    defaults: new { controller = "Ui1", action = "PortalUiHtml" }
                );

            //config.Routes.MapHttpRoute(
            //    name: "DefaultApi",
            //    routeTemplate: "api/{controller}/{id}",
            //    defaults: new { id = RouteParameter.Optional }
            //);

            //var json = GlobalConfiguration.Configuration.Formatters.JsonFormatter;
            //json.UseDataContractJsonSerializer = true;
        }
    }
}

