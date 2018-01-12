using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;

// add these
using ClientWebApp.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace IdentityServer4.AdoPersistence
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.SslPort = 5002;
                options.Filters.Add(new RequireHttpsAttribute());
            });

            services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "_af";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.HeaderName = "X-XSRF-TOKEN";
            });

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies")

            // default scheme from the earlier article
            // normal OIDC login flow (via Login button or [Authorize] attrib)
            .AddOpenIdConnect("oidc", options =>
            {
                options.SignInScheme = "Cookies";
                options.Authority = "https://localhost:5000";
                options.RequireHttpsMetadata = false;
                options.ClientId = "mv10blog.client";
                options.ClientSecret = "the_secret";
                options.ResponseType = "code id_token";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
            })

            // add this
            // attempt to re-establish persistent login for new session (see IndexModel.OnGet)
            .AddOpenIdConnect("persistent", options =>
             {
                 options.CallbackPath = "/signin-persistent";
                 options.Events = new OpenIdConnectEvents
                 {
                     OnRedirectToIdentityProvider = context =>
                     {
                         context.ProtocolMessage.Prompt = "none";
                         return Task.FromResult<object>(null);
                     },

                     OnMessageReceived = context => {
                         if(string.Equals(context.ProtocolMessage.Error, "login_required", StringComparison.Ordinal))
                         {
                             context.HandleResponse();
                             context.Response.Redirect("/");
                         }
                         return Task.FromResult<object>(null);
                     }
                 };

                 options.SignInScheme = "Cookies";
                 options.Authority = "https://localhost:5000";
                 options.RequireHttpsMetadata = false;
                 options.ClientId = "mv10blog.client";
                 options.ClientSecret = "the_secret";
                 options.ResponseType = "code id_token";
                 options.SaveTokens = true;
                 options.GetClaimsFromUserInfoEndpoint = true;
             });

            // add this
            services
                .AddScoped<IAccountService, AccountService>();  // same as ASP.NET Identity SignInService and UserManager
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if(env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();

                try
                {
                    var configuration = app.ApplicationServices.GetService<TelemetryConfiguration>();
                    configuration.DisableTelemetry = true;
                }
                catch { }
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseRewriter(new RewriteOptions().AddRedirectToHttps());
            app.UseAuthentication();
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });
        }
    }
}
