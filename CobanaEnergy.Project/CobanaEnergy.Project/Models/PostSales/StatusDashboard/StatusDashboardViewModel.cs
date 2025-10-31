using CobanaEnergy.Project.Models.Accounts.MasterDashboard.AccountMasterDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Models.PostSales.StatusDashboard
{
    public class StatusDashboardViewModel
    {
        public long? SupplierId { get; set; }
        public int ContractCount { get; set; }
        public List<SelectListItem> Suppliers { get; set; } = new List<SelectListItem>();
        public List<PostSalesRowViewModel> AccountsContracts { get; set; } = new List<PostSalesRowViewModel>();
    }

    public class UpdatePostSalesFieldDto
    {
        public string EId { get; set; }
        public string ContractType { get; set; }
        public string Email { get; set; }
        public string StartDate { get; set; }
        public string CED { get; set; }
        public string COTDate { get; set; }
        public string ReAppliedDate { get; set; }
        public int ReAppliedCount { get; set; }
        public string ContractStatus { get; set; }
        public string Duration { get; set; }
        public long ContractId { get; set; }
        public string ObjectionDate { get; set; }
        public string QueryType { get; set; }
    }

    public class PostSalesRowViewModel
    {
        public long ContractId { get; set; }
        public long SupplierId { get; set; }
        public string EId { get; set; }
        public string ContractType { get; set; }
        public string Agent { get; set; }
        public string AgentEmail { get; set; }
        public string Collaboration { get; set; }
        public string CobanaSalesType { get; set; }
        public string ContractNotes { get; set; }
        public string MPXN { get; set; }
        public string SupplierName { get; set; }
        public string BusinessName { get; set; }
        public string InputDate { get; set; }
        public string Duration { get; set; }
        public string Email { get; set; }
        public string StartDate { get; set; }
        public string CED { get; set; }
        public string PostCode { get; set; }
        public string COTDate { get; set; }
        public string ContractStatus { get; set; }
        public string ObjectionDate { get; set; }
        public int ObjectionCount { get; set; }
        public string QueryType { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
        public string Link { get; set; }
        public List<string> EmailList { get; set; }
    }

    public class ContractStatusSummary
    {
        public string ContractStatus { get; set; }
        public int Count { get; set; }
        public string ColorCode { get; set; }
    }
}