using System;
using System.Globalization;
using System.Threading.Tasks;
using CombinedLocalization.Override;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CombinedLocalization
{
    public class Startup
    {
        static string domain; //domain without subdomain
        public Startup()
        {
            domain = Environment.GetEnvironmentVariable("DOMAIN");
        }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddMvc()
               .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
               .AddDataAnnotationsLocalization();


            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("tr-TR"),
                };

                options.DefaultRequestCulture = new RequestCulture(culture: "en-US", uiCulture: "en-US");

                options.SupportedCultures = supportedCultures;

                options.SupportedUICultures = supportedCultures;

                options.RequestCultureProviders = new[] { new Override.CombinedCultureProvider() };
            });

            services.Configure<RouteOptions>(options =>
            {
                options.ConstraintMap.Add("culture", typeof(SubdomainRouteConstraint));
            });


        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(locOptions.Value);

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();


            app.Use((context, next) =>
            {
                if (context.Request.Host.Host.Equals(domain, StringComparison.OrdinalIgnoreCase))
                {
                    var combinedCultureProvider = new CombinedCultureProvider();
                    context.Response.Redirect(combinedCultureProvider.DetermineRedirectURLDueToCulture(context));
                    return Task.FromResult(0);
                }
                return next();
            });


            app.UseMvc(routes =>
            {
                routes.MapRoute(
                        name: "About",
                        template: "about",
                        defaults: new { controller = "Home", action = "About" },
                        constraints: new { culture = new SubdomainRouteConstraint() }
                );
                routes.MapRoute(
                        name: "Contact",
                        template: "contact",
                        defaults: new { controller = "Home", action = "Contact" },
                        constraints: new { culture = new SubdomainRouteConstraint() }
                );
                routes.MapRoute(
                        name: "LocalizedDefault",
                        template: "{controller}/{action}/{id?}",
                        defaults: new { controller = "Home", action = "Index", culture = "en" },
                        constraints: new { culture = new SubdomainRouteConstraint() }
                );
                routes.MapRoute(
                      name: "default",
                      template: "{*catchall}",
                      defaults: new { controller = "Home", action = "RedirectToDefaultCulture", culture = "en" });
            });
        }


        public class SubdomainRouteConstraint : IRouteConstraint
        {
            public bool Match(HttpContext httpContext, IRouter route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
            {
                string url = httpContext.Request.Headers["HOST"];
                int index = url.IndexOf(".", StringComparison.Ordinal);

                if (index < 0)
                    return false;

                string sub = url.Split('.')[0];
                if (sub != "tr" && sub != "en")
                    return false;


                values["culture"] = sub;
                return true;
            }
        }
    }
}
