using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EPS3.DataContexts;
using EPS3.Models;
using System.Data.Entity;
using Microsoft.Extensions.Logging;
using System.IO;
using Serilog;

namespace EPS3
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
            string connectionString = Configuration.GetConnectionString("EPSContext");
            string smtpSetup = Configuration.GetSection("Smtp").ToString();
            Log.Information("Connection String: " + connectionString);
            Log.Information("Smtp Setup: " + smtpSetup);

            services.Configure<SmtpConfig>(Configuration.GetSection("Smtp"));
            services.AddDbContext<EPSContext>(options => options.UseSqlServer(connectionString));
            services.AddMvc(options =>
            {
            }).AddSessionStateTempDataProvider();
            
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, EPSContext context, ILoggerFactory loggerFactory)
        {
            app.UseDeveloperExceptionPage();
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseSession();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "duplicate",
                    template: "{controller=LineItems}/{action=Edit}/{id}/{duplicate?}");
                routes.MapRoute(
                    name: "newLineItemGroup",
                    template: "{controller=LineItems}/{action=Create}/{contractID}/{groupID}");
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            //DbInitializer.Initialize(context);
            loggerFactory.AddDebug();
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("eps2_log.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }
    }
}
