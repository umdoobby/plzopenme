using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using PlzOpenMe.Models;
using Serilog;

namespace PlzOpenMe
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // create the configuration service
            services.AddSingleton<IConfiguration>(Configuration);
            
            // create the database service
            services.AddDbContext<PlzOpenMeContext>(builder => builder
                .UseMySql(Configuration.GetConnectionString("MainDatabase"),
                    new MySqlServerVersion(new Version(Configuration.GetValue<string>("MariaDbVersion"))),
                    mySqlOptions => mySqlOptions
                        .CharSetBehavior(CharSetBehavior.NeverAppend))
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
            );
            
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            // enable serilog, log capture
            app.UseSerilogRequestLogging();

            // this app runs behind a reverse proxy and does not require ssl as that is handled by the proxy
            //app.UseHttpsRedirection();
            
            app.UseStaticFiles();

            app.UseRouting();
            
            // set up to respond to the forwarded requests correctly
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}