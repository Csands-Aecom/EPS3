using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPS3.LegacyModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EPS3.DataContexts
{
    public class LegacyContext : DbContext
    {
        public LegacyContext(DbContextOptions<LegacyContext> options) : base(options)
        {
        }

        public DbSet<LegacyContract> LegacyContracts { get; set; }
        public DbSet<LegacyFinancial> LegacyFinancials { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // No connection string needed since it is set in Startup, read from appsettings.json
            //optionsBuilder.UseSqlServer(@"Server=USTLH1LT1506\TURNPIKETEST;Database=EPS;Trusted_Connection=True;"); //Local
            optionsBuilder.UseSqlServer(@"Server=DOTSTPSQL;Database=EPS;user id=ursWeb;password=ursweb"); //Test & Prod
            // log SQL
            var lf = new LoggerFactory();
            lf.AddConsole();
            optionsBuilder.UseLoggerFactory(lf);
        }
    }
}
