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
            //base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<Contract>().Property(p => p.GovernorDeclaredEmergencyNumber).HasColumnName("GovernorDeclaredEmergencyNumber");

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

        public List<LineItem> GetDeepLineItems(int groupID)
        {
            List<LineItem> items = this.LineItems.AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.Fund)
                .Include(l => l.OCA)
                .Include(l => l.StateProgram)
                .OrderBy(l => l.LineNumber)
                .Where(l => l.LineItemGroupID == groupID)
                .ToList();
            return items;
        }

        public LineItem GetDeepLineItem(int lineItemID)
        {
            LineItem item = this.LineItems.AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.Fund)
                .Include(l => l.OCA)
                .Include(l => l.StateProgram)
                .OrderBy(l => l.LineNumber)
                .SingleOrDefault(l => l.LineItemID == lineItemID);
            return item;
        }


        public List<LineItemGroupStatus> GetDeepEncumbranceStatuses(int groupID)
        {
            List<LineItemGroupStatus> resultList = this.LineItemGroupStatuses
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.LineItemGroupID == groupID)
                .OrderBy(s => s.SubmittalDate)
                .ToList();

            return resultList;
        }

        public LineItemGroup GetDeepEncumbrance(int groupID)
        {
            LineItemGroup encumbrance = this.LineItemGroups.AsNoTracking()
                .Include(l => l.LastEditedUser)
                .Include(l => l.OriginatorUser)
                .Include(l => l.Contract)
                .Include(l => l.FileAttachments)
                .Include(l => l.LineItems).ThenInclude(li => li.OCA)
                .Include(l => l.LineItems).ThenInclude(li => li.Category)
                .Include(l => l.LineItems).ThenInclude(li => li.StateProgram)
                .Include(l => l.LineItems).ThenInclude(li => li.Fund)
                .Include(l => l.Statuses).ThenInclude(gst => gst.User)
                .SingleOrDefault(l => l.GroupID == groupID);
            return encumbrance;
        }

        public List<LineItemGroup> GetDeepEncumbrances(int contractID)
        {
            List<LineItemGroup> encumbrances = this.LineItemGroups.AsNoTracking()
                .Include(l => l.Contract)
                .Include(l => l.LastEditedUser)
                .Include(l => l.OriginatorUser)
                .Include(l => l.LineItems).ThenInclude(li => li.OCA)
                .Include(l => l.LineItems).ThenInclude(li => li.Category)
                .Include(l => l.LineItems).ThenInclude(li => li.StateProgram)
                .Include(l => l.LineItems).ThenInclude(li => li.Fund)
                .Include(l => l.Statuses).ThenInclude(gst => gst.User)
                .Where(l => l.ContractID == contractID)
                .ToList();
            return encumbrances;
        }

        public Contract GetDeepContract(int contractID)
        {
            Contract contract = this.Contracts.AsNoTracking()
                .Include(c => c.ContractFunding)
                .Include(c => c.MethodOfProcurement)
                .Include(c => c.Vendor)
                .Include(c => c.User)
                .Include(c => c.Recipient)
                .Include(c => c.ContractType)
                .SingleOrDefault(c => c.ContractID == contractID);
            return contract;
        }

        public decimal GetTotalAmountOfAllEncumbrances(int ContractID) //todo why not make this property of a contract?
        {
            List<LineItemGroup> encumbrances = this.LineItemGroups.AsNoTracking()
                .Include(l => l.LineItems)
                .Where(l => l.ContractID == ContractID).ToList();
            decimal totalAmount = 0.0m;
            foreach (LineItemGroup encumbrance in encumbrances)
            {
                if (encumbrance.LineItemType != ConstantStrings.Advertisement)
                {
                    foreach (LineItem lineitem in encumbrance.LineItems)
                    {
                        totalAmount += lineitem.Amount;
                    }
                }
            }
            return totalAmount;
        }

        public User GetUserByID(int userID)
        {
            return this.Users
            .Where(u => u.UserID == userID)
            .AsNoTracking()
            .SingleOrDefault();
        }



        public Dictionary<int, List<LineItemGroupStatus>> GetDeepContractEncumbranceStatusMap(int contractID)
        {
            Dictionary<int, List<LineItemGroupStatus>> resultMap = new Dictionary<int, List<LineItemGroupStatus>>();
            List<int> encumbranceIDs = this.LineItemGroups
                .AsNoTracking()
                .Where(e => e.ContractID == contractID)
                .Select(e => e.GroupID)
                .ToList();
            foreach (int groupID in encumbranceIDs)
            {
                resultMap.Add(groupID, this.GetDeepEncumbranceStatuses(groupID));
            }
            return resultMap;
        }

        public Contract GetContractByEncumbranceID(int encumbranceID)
        {
            int contractID = this.LineItemGroups
                                .AsNoTracking()
                                .Where(e => e.GroupID == encumbranceID)
                                .Select(e => e.ContractID)
                                .SingleOrDefault();
            return GetContractByID(contractID);
        }

        public Contract GetContractByID(int contractID)
        {
            // returns the Contract with the specified contractID
            if (contractID > 0)
            {
                Contract contract = this.Contracts
                    .Where(c => c.ContractID == contractID)
                    .AsNoTracking()
                    .SingleOrDefault();
                return contract;
            }
            return null;
        }

        public List<Contract> GetContractsByStatus(string status)
        {
            // returns Contracts with current status matching the specified status
            List<Contract> contracts = this.Contracts
                .Include(c => c.Vendor)
                .Include(c => c.ContractType)
                .Include(c => c.User)
                .Where(c => c.CurrentStatus.Contains(status))
                .AsNoTracking()
                .ToList();
            return (contracts);
        }

        public List<Contract> GetOriginatorOwnedContracts(int userID)
        {
            // returns Contracts created by the specified user
            List<Contract> contracts = this.Contracts
                .Include(c => c.Vendor)
                .Include(c => c.ContractType)
                .Include(c => c.User)
                .Where(c => c.UserID == userID)
                .AsNoTracking()
                .ToList();
            return (contracts);
        }
        public bool HasLineItems(Contract contract)
        {
            int itemCount = this.LineItems.Where(li => li.ContractID == contract.ContractID).Count();
            return (itemCount > 0);
        }
        public bool HasLineItems(LineItemGroup encumbrance)
        {
            int itemCount = this.LineItems.Where(li => li.LineItemGroupID == encumbrance.GroupID).Count();
            return (itemCount > 0);
        }
    }
}
