using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels.Crown
{
    public class CrownEacLogViewModel : BGBEacLogViewModel
    {
        public string SupplierCommsTypeElectric { get; set; }
        public string SupplierCommsTypeGas { get; set; }
        public string SupplierCommsType
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SupplierCommsTypeGas))
                    return SupplierCommsTypeGas;
                return SupplierCommsTypeElectric;
            }
        }
    }
}