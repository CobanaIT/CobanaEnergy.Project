using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
using CobanaEnergy.Project.Models.Sector.SectorDBModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Electric.ElectricDBModels
{
    [Table("CE_ElectricContracts")]
    public class CE_ElectricContracts
    {
        public string EId { get; set; }
        public long Id { get; set; }
        public string Agent { get; set; }

        public string TopLine { get; set; }
        public string MPAN { get; set; }

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

        public string InputDate { get; set; }
        public string InitialStartDate { get; set; }
        public string Uplift { get; set; }
        public string Duration { get; set; }
        public string InputEAC { get; set; }

        public string DayRate { get; set; }
        public string NightRate { get; set; }
        public string EveWeekendRate { get; set; }
        public string OtherRate { get; set; }
        public string StandingCharge { get; set; }

       // public string SortCode { get; set; }
       // public string AccountNumber { get; set; }
        public string CurrentSupplier { get; set; }

        public long SupplierId { get; set; }
        public long ProductId { get; set; }

        public string EMProcessor { get; set; }
        public bool ContractChecked { get; set; }
        public bool ContractAudited { get; set; }
        public bool Terminated { get; set; }

        public string ContractNotes { get; set; }
        //public string LOGS { get; set; }

        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string Type { get; set; }
        public string Department { get; set; }
        public string Source { get; set; }
        public string SalesType { get; set; }
        public string SalesTypeStatus { get; set; }
        public string SupplierCommsType { get; set; }
        public string PreSalesStatus { get; set; }
        public DateTime? PreSalesFollowUpDate { get; set; }


        public int BrokerageId { get; set; }
        public string OfgemId { get; set; }

        // New dynamic fields based on Department
        public int? CloserId { get; set; }
        public int? ReferralPartnerId { get; set; }
        public int? SubReferralPartnerId { get; set; }
        public int? BrokerageStaffId { get; set; }
        public int? IntroducerId { get; set; }
        public int? SubIntroducerId { get; set; }
        public int? SubBrokerageId { get; set; }
        public string Collaboration { get; set; }
        public int? LeadGeneratorId { get; set; }

        // Navigation properties
        [ForeignKey("SupplierId")]
        public virtual CE_Supplier CE_Supplier { get; set; }

        [ForeignKey("ProductId")]
        public virtual CE_SupplierProducts CE_SupplierProducts { get; set; }

        // Only add FK for BrokerageId
        [ForeignKey("BrokerageId")]
        public virtual CE_Sector CE_Brokerage { get; set; }
    }
}