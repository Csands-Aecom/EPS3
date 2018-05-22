using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Threading.Tasks;
using EPS3.Models;
using Microsoft.EntityFrameworkCore;

namespace EPS3.DataContexts
{
    public class EPSContext : DbContext
    {
        public EPSContext(DbContextOptions<EPSContext> options) : base(options)
        {

        }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Compensation> Compensations { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractStatus> ContractStatuses { get; set; }
        public DbSet<ContractType> ContractTypes { get; set; }
        public DbSet<Fund> Funds { get; set; }
        public DbSet<LineItem> LineItems { get; set; }
        public DbSet<LineItemStatus> LineItemStatuses { get; set; }
        public DbSet<OCA> OCAs { get; set; }
        public DbSet<Procurement> Procurements { get; set; }
        public DbSet<Recipient> Recipients { get; set; }
        public DbSet<StateProgram> StatePrograms { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<WorkActivity> WorkActivities { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlServer(@"Server=\\TLH1LT150\TURNPIKETEST;Datebase=NewEPS;Trusted_Connection=True;");
        }
    }
}
