using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Business
{
    [Table("CE_BusinessContactInfo")]
    public class CE_BusinessContactInfo
    {
        [Key]
        public string EId { get; set; }
        public string Type { get; set; }

        [Index("IX_BusinessName")]
        [MaxLength(300)] 
        public string BusinessName { get; set; }

        [Index("IX_CustomerName")]
        [MaxLength(300)]
        public string CustomerName { get; set; }
        public string PhoneNumber1 { get; set; }
        public string PhoneNumber2 { get; set; }
        public string EmailAddress { get; set; }
    }
}