using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_Sector")]
    public class CE_Sector
    {
        public CE_Sector()
        {
            // Direct foreign key relationships
            this.CE_BrokerageStaff = new List<CE_BrokerageStaff>();
            this.CE_SubBrokerages = new List<CE_SubBrokerage>();
            this.CE_SubReferrals = new List<CE_SubReferral>();
            this.CE_SubIntroducers = new List<CE_SubIntroducer>();
            this.CE_BrokerageCommissionAndPayments = new List<CE_BrokerageCommissionAndPayment>();
            this.CE_CloserCommissionAndPayments = new List<CE_CloserCommissionAndPayment>();
            this.CE_IntroducerCommissionAndPayments = new List<CE_IntroducerCommissionAndPayment>();
            this.CE_ReferralPartnerCommissionAndPayments = new List<CE_ReferralPartnerCommissionAndPayment>();
            this.CE_LeadGeneratorCommissionAndPayments = new List<CE_LeadGeneratorCommissionAndPayment>();
            this.SectorSuppliers = new List<CE_SectorSupplier>();
        }

        [Key]
        public int SectorID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        public bool Active { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(200)]
        public string Email { get; set; }

        [StringLength(50)]
        public string Landline { get; set; }

        [StringLength(50)]
        public string Mobile { get; set; }

        [StringLength(100)]
        public string OfgemID { get; set; }

        [StringLength(100)]
        public string Department { get; set; }

        [Required]
        [StringLength(100)]
        public string SectorType { get; set; }

        // Navigation Properties
        // Direct foreign key relationships
        public virtual ICollection<CE_BrokerageStaff> CE_BrokerageStaff { get; set; }
        public virtual ICollection<CE_SubBrokerage> CE_SubBrokerages { get; set; }
        public virtual ICollection<CE_SubReferral> CE_SubReferrals { get; set; }
        public virtual ICollection<CE_SubIntroducer> CE_SubIntroducers { get; set; }
        public virtual ICollection<CE_BrokerageCommissionAndPayment> CE_BrokerageCommissionAndPayments { get; set; }
        public virtual ICollection<CE_CloserCommissionAndPayment> CE_CloserCommissionAndPayments { get; set; }
        public virtual ICollection<CE_IntroducerCommissionAndPayment> CE_IntroducerCommissionAndPayments { get; set; }
        public virtual ICollection<CE_ReferralPartnerCommissionAndPayment> CE_ReferralPartnerCommissionAndPayments { get; set; }
        public virtual ICollection<CE_LeadGeneratorCommissionAndPayment> CE_LeadGeneratorCommissionAndPayments { get; set; }
        public virtual ICollection<CE_SectorSupplier> SectorSuppliers { get; set; }
    }
}
