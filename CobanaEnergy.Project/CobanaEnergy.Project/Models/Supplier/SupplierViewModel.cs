using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier
{
    public class SupplierViewModel
    {
        [Required]
        public string Name { get; set; }
        public string Link { get; set; }
        public bool Status { get; set; } = true;
        public List<SupplierContactViewModel> Contacts { get; set; } = new List<SupplierContactViewModel>();
        public List<SupplierProductViewModel> Products { get; set; } = new List<SupplierProductViewModel>();
        public List<UpliftViewModel> Uplifts { get; set; } = new List<UpliftViewModel>();

    }

}