using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels.ScottishPower
{
    public class ScottishPowerEacLogViewModel :BGBEacLogViewModel
    {
        public string SupplierCommsTypeElectric { get; set; }
        public string SupplierCommsTypeGas { get; set; }

        public string CommissionElectric { get; set; }
        public string UpliftElectric { get; set; }
        //Gas
        public string CommissionGas { get; set; }
        public string UpliftGas { get; set; }
        public string EDFSmeEac { get; set; }
        public string SupplierCommsType
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SupplierCommsTypeGas))
                    return SupplierCommsTypeGas;
                return SupplierCommsTypeElectric;
            }
        }

        public string Commission
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CommissionGas))
                    return CommissionGas;
                return CommissionElectric;
            }
        }

        public string Uplift
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(UpliftGas))
                    return UpliftGas;
                return UpliftElectric;
            }
        }
    }
}