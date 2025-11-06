using CobanaEnergy.Project.Models.Accounts;
using CobanaEnergy.Project.Models.Accounts.MainCampaign.DBModel;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using CobanaEnergy.Project.Models.AccountsDBModel;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels.snapshot;
using CobanaEnergy.Project.Models.EmailTemplateLookup;
using CobanaEnergy.Project.Models.Gas.GasDBModels;
using CobanaEnergy.Project.Models.InvoiceSupplierDashboard;
using CobanaEnergy.Project.Models.PostSales.Entities;
using CobanaEnergy.Project.Models.PostSales.RegInvoiceSupplierDashboard.DB_Model;
using CobanaEnergy.Project.Models.Sector.SectorDBModels;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot_Gas;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models
{
    public class ApplicationDBContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDBContext() : base("CobanaConnection") { }

        public static ApplicationDBContext Create()
        {
            return new ApplicationDBContext();
        }

        // Add other DbSets here (e.g., Suppliers, Contracts)
        // public DbSet<Supplier> Suppliers { get; set; }     
        public DbSet<CE_Supplier> CE_Supplier { get; set; }
        public DbSet<CE_SupplierContacts> CE_SupplierContacts { get; set; }
        public DbSet<CE_SupplierProducts> CE_SupplierProducts { get; set; }
        public DbSet<CE_ElectricContracts> CE_ElectricContracts { get; set; }
        public DbSet<CE_SupplierUplifts> CE_SupplierUplifts { get; set; }
        public DbSet<CE_ElectricContractLogs> CE_ElectricContractLogs { get; set; }
        public DbSet<CE_GasContracts> CE_GasContracts { get; set; }
        public DbSet<CE_GasContractLogs> CE_GasContractLogs { get; set; }
        public DbSet<CE_Accounts> CE_Accounts { get; set; }
        public DbSet<CE_ElectricSupplierSnapshots> CE_ElectricSupplierSnapshots { get; set; }
        public DbSet<CE_ElectricSupplierContactSnapshots> CE_ElectricSupplierContactSnapshots { get; set; }
        public DbSet<CE_ElectricSupplierProductSnapshots> CE_ElectricSupplierProductSnapshots { get; set; }
        public DbSet<CE_ElectricSupplierUpliftSnapshots> CE_ElectricSupplierUpliftSnapshots { get; set; }
        public DbSet<CE_GasSupplierSnapshots> CE_GasSupplierSnapshots { get; set; }
        public DbSet<CE_GasSupplierContactSnapshots> CE_GasSupplierContactSnapshots { get; set; }
        public DbSet<CE_GasSupplierProductSnapshots> CE_GasSupplierProductSnapshots { get; set; }
        public DbSet<CE_GasSupplierUpliftSnapshots> CE_GasSupplierUpliftSnapshots { get; set; }
        public DbSet<CE_InvoiceSupplierUploads> CE_InvoiceSupplierUploads { get; set; }
        public DbSet<CE_RegSupplierFileUploads> CE_RegSupplierFileUploads { get; set; }
        public DbSet<CE_EacLogs> CE_EacLogs { get; set; }
        public DbSet<CE_ContractStatuses> CE_ContractStatuses { get; set; }
        public DbSet<CE_CommissionAndReconciliation> CE_CommissionAndReconciliation { get; set; }
        public DbSet<CE_CommissionMetrics> CE_CommissionMetrics { get; set; }
        public DbSet<CE_PaymentAndNoteLogs> CE_PaymentAndNoteLogs { get; set; }
        public DbSet<CE_Campaign> CE_Campaigns { get; set; }
        public DbSet<CE_CampaignNotification> CE_CampaignNotifications { get; set; }
        public DbSet<CE_UserNotificationStatus> CE_UserNotificationStatus { get; set; }

        // Sector DbSets
        public DbSet<CE_Sector> CE_Sector { get; set; }
        public DbSet<CE_BankDetails> CE_BankDetails { get; set; }
        public DbSet<CE_CompanyTaxInfo> CE_CompanyTaxInfo { get; set; }
        public DbSet<CE_BrokerageCommissionAndPayment> CE_BrokerageCommissionAndPayment { get; set; }
        public DbSet<CE_CloserCommissionAndPayment> CE_CloserCommissionAndPayment { get; set; }
        public DbSet<CE_IntroducerCommissionAndPayment> CE_IntroducerCommissionAndPayment { get; set; }
        public DbSet<CE_ReferralPartnerCommissionAndPayment> CE_ReferralPartnerCommissionAndPayment { get; set; }
        public DbSet<CE_LeadGeneratorCommissionAndPayment> CE_LeadGeneratorCommissionAndPayment { get; set; }
        public DbSet<CE_BrokerageStaff> CE_BrokerageStaff { get; set; }
        public DbSet<CE_SubBrokerage> CE_SubBrokerage { get; set; }
        public DbSet<CE_SubBrokerageCommissionAndPayment> CE_SubBrokerageCommissionAndPayment { get; set; }
        public DbSet<CE_SubReferral> CE_SubReferral { get; set; }
        public DbSet<CE_SubReferralCommissionAndPayment> CE_SubReferralCommissionAndPayment { get; set; }
        public DbSet<CE_SubIntroducer> CE_SubIntroducer { get; set; }
        public DbSet<CE_SubIntroducerCommissionAndPayment> CE_SubIntroducerCommissionAndPayment { get; set; }
        public DbSet<CE_SectorSupplier> CE_SectorSupplier { get; set; }
        public DbSet<CE_PostSaleObjection> CE_PostSaleObjections { get; set; }
        public DbSet<CE_EmailTemplateLookup> CE_EmailTemplateLookups { get; set; }
        public DbSet<CE_PostSalesLogs> CE_PostSalesLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Note: Polymorphic relationships for CE_BankDetails and CE_CompanyTaxInfo
            // will be handled manually in the application logic using EntityType and EntityID
            // EF6 doesn't support complex polymorphic relationships with multiple foreign keys to the same column

            // The navigation properties are kept for convenience but won't be automatically populated by EF6
            // You'll need to manually populate them based on EntityType when querying
        }
    }
}