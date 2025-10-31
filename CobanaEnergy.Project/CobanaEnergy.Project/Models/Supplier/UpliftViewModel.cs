using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier
{
    public class UpliftViewModel
    {
        public long Id { get; set; }
        [Required]
        public string FuelType { get; set; }
        [Required]
        public string Uplift { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
    }
}