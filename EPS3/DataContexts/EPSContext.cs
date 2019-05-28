using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Threading.Tasks;
using EPS3.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using EPS3.Helpers;
using System.Data.Entity.Infrastructure;

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
        public DbSet<ContractType> ContractTypes { get; set; }
        public DbSet<Fund> Funds { get; set; }
        public DbSet<LineItem> LineItems { get; set; }
        public DbSet<LineItemGroup> LineItemGroups { get; set; }
        public DbSet<LineItemGroupStatus> LineItemGroupStatuses { get; set; }
        public DbSet<OCA> OCAs { get; set; }
        public DbSet<Procurement> Procurements { get; set; }
        public DbSet<Recipient> Recipients { get; set; }
        public DbSet<StateProgram> StatePrograms { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<WorkActivity> WorkActivities { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<FileAttachment> FileAttachments { get; internal set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageRecipient> MessageRecipients  { get; set; }
        public DbSet<EncumbranceLookup> EncumbranceLookups { get; set; }

        // Read only model from Views
        public DbSet<VEncumbrance> VEncumbrances { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.
            Entity<VEncumbrance>().ToTable("VEncumbrances")
            .Property(v => v.GroupID).HasColumnName("GroupID");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            try
            {
                var appSettingsJson = AppSettingsJson.GetAppSettings();
                var connectionStrings = appSettingsJson["ConnectionStrings"];
                var connectionString = appSettingsJson["EPSContext"];
                connectionString = "Data Source=USTLH1LT1506\\TurnpikeTest;Initial Catalog=EPSNew;Integrated Security=True";
            }
            catch (Exception e)
            {
                Console.Write(e.StackTrace.ToString());
            }
        }
    }
}
