using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier
{
    public class SupplierProductViewModel
    {
        public long Id { get; set; }
        [Required]
        public string ProductName { get; set; }
        [Required]
        public string StartDate { get; set; }
        [Required]
        public string EndDate { get; set; }
        [Required]
        public string Commission { get; set; }
        [Required]
        public string SupplierCommsType { get; set; }
    }

}