using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace CombinedLocalization.Helpers
{
    public static class LocalizationHelpers
    {
        private static string enviroment;
        private static string domain;
        static LocalizationHelpers()
        {
            enviroment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            domain = Environment.GetEnvironmentVariable("DOMAIN");
        }
        public static IHtmlContent LocalizedLink(this IHtmlHelper html, string countryCode, string LanguageName)
        {
            var host = html.ViewContext.HttpContext.Request.Host;
            

            var request = html.ViewContext.HttpContext.Request;
            HostString newHost;

            if (host.Port.HasValue)
            {
                newHost = new HostString(countryCode + "." + domain, host.Port.Value);
            }
            else
            {
                newHost = new HostString(countryCode + "." + domain);
            }

            var newUri = UriHelper.BuildAbsolute(request.Scheme, newHost, request.PathBase, request.Path, request.QueryString);


            return new HtmlString("<a href=\"" + newUri + "\">" + LanguageName + "</a>");
        }

        public static IHtmlContent GetEnv(this IHtmlHelper html)
        {
            string returnHtml = "";
            var enumerator = Environment.GetEnvironmentVariables().GetEnumerator();
            while (enumerator.MoveNext())
            {
                returnHtml += "<h4>" + ($"{enumerator.Key,5}:{enumerator.Value,100}") + "</h4>";
            }
            return new HtmlString(returnHtml);
        }

    }
}
