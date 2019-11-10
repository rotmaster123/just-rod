using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;

namespace MyApp
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
            // Add any DbContext here
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "my_data";
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var user = Environment.GetEnvironmentVariable("DB_USERNAME");
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
            var connectionString = $"Host={host};Database={dbName};Username={user};Password={password}";
            services.AddEntityFrameworkNpgsql();
            services.AddDbContext<FruitsContext>(opt => opt.UseNpgsql(connectionString));

            services.AddHealthChecks()
                    .AddNpgSql(connectionString);

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHealthChecks("/health");

            // Optionally, initialize Db with data here
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<FruitsContext>();
                context.Database.EnsureCreated();

                // Seed database with initial data on first run
                var fruit = context.Fruits.FirstOrDefault();
                if (fruit == null)
                {
                    context.Fruits.Add(new Fruit {Name = "Apple" , Stock = 10 });
                    context.Fruits.Add(new Fruit {Name = "Orange", Stock = 10 });
                    context.Fruits.Add(new Fruit {Name = "Pear", Stock = 10 });
                }
                context.SaveChanges();
            }

            app.UseMvc();
        }
    }
}
