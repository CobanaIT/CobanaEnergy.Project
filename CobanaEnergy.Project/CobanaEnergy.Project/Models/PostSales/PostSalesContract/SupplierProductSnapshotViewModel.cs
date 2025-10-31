using CobanaEnergy.Project.Models.Supplier.SupplierSnapshots_Gas;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.PostSales.PostSalesContract
{

    public enum FuelType { Gas = 1, Electric = 2 }

    public class PostSalesEditViewModel
    {
        // Common
        public long Id { get; set; }
        public string EId { get; set; }
        public FuelType ContractType { get; set; } // Gas/Electric

        // Generic "Supply Number" surface (MPAN/MPRN)
        public string SupplyNumberLabel { get; set; }      // "MPRN" or "MPAN"
        public string SupplyNumberPattern { get; set; }    // e.g. @"^\d{6,10}$" for MPRN, MPAN pattern for electric
        public string SupplyNumber { get; set; }           // actual value (MPRN/MPAN)
        public string SupplyNumberTitle { get; set; }      // title/tooltip

        // Business & contact
        public string AgentName { get; set; }
        public string AgentEmail { get; set; }
        public string Link { get; set; }
        public string CollaborationName { get; set; }
        public string BusinessName { get; set; }
        public string CustomerName { get; set; }
        public string BusinessDoorNumber { get; set; }
        public string BusinessHouseName { get; set; }
        public string BusinessStreet { get; set; }
        public string BusinessTown { get; set; }
        public string BusinessCounty { get; set; }
        public string PostCode { get; set; }
        public string PhoneNumber1 { get; set; }
        public string PhoneNumber2 { get; set; }
        public string EmailAddress { get; set; }

        // Pricing/contract
        public string InitialStartDate { get; set; }
        public string InputDate { get; set; }
        public string CED { get; set; }
        public string CEDCOT { get; set; }
        public string Uplift { get; set; }
        public string Duration { get; set; }
        public string InputEAC { get; set; }
        public string UnitRate { get; set; }
        public string OtherRate { get; set; }
        /// <summary>
        /// In Case of Smartest Energy
        /// </summary>
        public string ContractId { get; set; }
        public string StandingCharge { get; set; }

        // Bank
        public string SortCode { get; set; }
        public string AccountNumber { get; set; }
        public string CurrentSupplier { get; set; }

        // Supplier/Product
        public long SupplierId { get; set; }
        public long ProductId { get; set; }
        public string SupplierCommsType { get; set; }

        // Workflow
        public string EMProcessor { get; set; }
        public bool ContractChecked { get; set; }
        public bool ContractAudited { get; set; }
        public bool Terminated { get; set; }
        public string ContractNotes { get; set; }
        public string Department { get; set; }
        public string Source { get; set; }
        public string SalesType { get; set; }
        public string SalesTypeStatus { get; set; }
        public string TopLine { get; set; }
        public string PreSalesStatus { get; set; }
        public string CampaignName { get; set; }
        public string PreSalesFollowUpDate { get; set; }

        // Brokerage
        public int? BrokerageId { get; set; }
        public string OfgemId { get; set; }

        // Dynamic
        public int? CloserId { get; set; }
        public int? ReferralPartnerId { get; set; }
        public int? SubReferralPartnerId { get; set; }
        public int? BrokerageStaffId { get; set; }
        public int? IntroducerId { get; set; }
        public int? SubIntroducerId { get; set; }
        public int? SubBrokerageId { get; set; }
        public string Collaboration { get; set; }
        public int? LeadGeneratorId { get; set; }

        // Post-sales
        public string ContractStatus { get; set; }
        public string QueryType { get; set; }
        public int? ObjectionCount { get; set; }
        public string ObjectionDate { get; set; }
        public int? ReappliedCount { get; set; }
        public string ReappliedDate { get; set; }
        public string ProspectedSaleDate { get; set; }
        public string ProspectedSaleNotes { get; set; }
        public string FollowUpDate { get; set; }
        public List<string> EmailList { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }

        // Snapshot (generic)
        public SupplierSnapshotViewModel SupplierSnapshot { get; set; }
    }

    public class SupplierSnapshotViewModel
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string EId { get; set; }
        public string SupplierName { get; set; }
        public string Link { get; set; }
        public List<SupplierProductSnapshotViewModel> Products { get; set; }
        public List<SupplierUpliftSnapshotViewModel> Uplifts { get; set; }
        public List<SupplierContactSnapshotViewModel> Contacts { get; set; }
    }

    public class SupplierProductSnapshotViewModel
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string SupplierCommsType { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Commission { get; set; }
    }

    public class SupplierUpliftSnapshotViewModel
    {
        public long Id { get; set; }
        public string Uplift { get; set; }
        public string FuelType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class SupplierContactSnapshotViewModel
    {
        public long Id { get; set; }
        public string ContactName { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Notes { get; set; }
    }

}