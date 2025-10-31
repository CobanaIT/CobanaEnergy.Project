using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.Edit_Supplier
{
    public class SupplierUpliftViewModel
    {
        public long Id { get; set; }
        [Required]
        public string FuelType { get; set; }
        [Required]
        public string Uplift { get; set; }
        [Required]
        public string StartDate { get; set; }
        [Required]
        public string EndDate { get; set; }
    }
}