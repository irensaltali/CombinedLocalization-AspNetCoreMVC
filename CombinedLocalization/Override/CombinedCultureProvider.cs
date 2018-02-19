using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CombinedLocalization.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Localization;

namespace CombinedLocalization.Override
{
    public class CombinedCultureProvider : RequestCultureProvider
    {
        private static string enviroment;
        private static string domain;

        public CombinedCultureProvider()
        {
            enviroment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            domain = Environment.GetEnvironmentVariable("DOMAIN");
        }

        public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));

            string culture = null;
            var host = httpContext.Request.Host.Host.Split('.');


            if ((IsDevelopment() && host.Length != 2) ||
                (!IsDevelopment() && host.Length != 3))
            {
                var cookieRequestCultureProvider = new CookieRequestCultureProvider();
                return cookieRequestCultureProvider.DetermineProviderCultureResult(httpContext);
            }


            var twoLetterCultureName = host[0]?.ToString();

            if (twoLetterCultureName == "tr")
                culture = "tr-TR";
            else if (twoLetterCultureName == "en")
                culture = "en-US";

            if (culture == null)
                return NullProviderCultureResult;

            //Set Culture Cookie
            httpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    Domain = "."+domain
                }
            );

            var providerResultCulture = new ProviderCultureResult(culture, culture);

            return Task.FromResult(providerResultCulture);
        }

        public string DetermineRedirectURLDueToCulture(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var host = request.Host;
            var subdomain = "";

            HostString newHost;

            //First Cookie
            httpContext.Request.Cookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out string cultureCookie);
            var currentCulture = CookieRequestCultureProvider.ParseCookieValue(cultureCookie);
            if (currentCulture != null && currentCulture.Cultures.Count > 0)
            {
                if (currentCulture.Cultures.First().Value == "tr-TR")
                    subdomain = "tr";
                else
                    subdomain = "en";
            }
            else
            {
                //Second GeoLocation
                var country = DetermineCountryDueToIpAddress(httpContext);
                if (!string.IsNullOrEmpty(country))
                {
                    if (country == "TR")
                        subdomain = "tr";
                    else
                        subdomain = "en";

                }
                // Last to default
                else
                    subdomain = "en";

            }

            if (host.Port.HasValue)
            {
                newHost = new HostString(subdomain + "." + domain, host.Port.Value);
            }
            else
            {
                newHost = new HostString(subdomain + "." + domain);
            }

            return UriHelper.BuildAbsolute(request.Scheme, newHost, request.PathBase, request.Path, request.QueryString);
        }

        private string DetermineCountryDueToIpAddress(HttpContext httpContext)
        {
            var json = new WebClient().DownloadString("http://www.geoplugin.net/json.gp?ip="+ httpContext.Connection.RemoteIpAddress.ToString());
            var geoData = Newtonsoft.Json.JsonConvert.DeserializeObject<GeoPluginModel>(json);

            return geoData.geoplugin_countryCode;
        }

        private static bool IsDevelopment()
        {
            if (enviroment != null && enviroment == "Development")
                return true;
            else
                return false;
        }
    }
}
