// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingWebSite
{
    // For by tests for dynamic routing to pages/controllers
    public class StartupForDynamic
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddNewtonsoftJson()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddSingleton<Transformer>();

            // Used by some controllers defined in this project.
            services.Configure<RouteOptions>(options => options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                // Workaround for #8130
                //
                // You can't dynamically route to this unless it already has another route.
                endpoints.MapAreaControllerRoute("admin", "Admin", "Admin/{controller=Home}/{action=Index}/{id?}");

                endpoints.MapDynamicControllerRoute<Transformer>("dynamic/{**slug}");
                endpoints.MapDynamicPageRoute<Transformer>("dynamicpage/{**slug}");
            });

            app.Map("/afterrouting", b => b.Run(c =>
            {
                return c.Response.WriteAsync("Hello from middleware after routing");
            }));
        }

        private class Transformer : DynamicRouteValueTransformer
        {
            // Turns a format like `controller=Home,action=Index` into an RVD
            public override Task<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
            {
                var kvps = ((string)values["slug"]).Split(",");

                var results = new RouteValueDictionary();
                foreach (var kvp in kvps)
                {
                    var split = kvp.Split("=");
                    results[split[0]] = split[1];
                }

                return Task.FromResult(results);
            }
        }
    }
}
