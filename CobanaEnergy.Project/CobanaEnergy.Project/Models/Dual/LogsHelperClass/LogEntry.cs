using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Dual.LogsHelperClass
{
    public class LogEntry
    {
        public string EId { get; set; }
        public string Username { get; set; }
        public string ActionDate { get; set; } 
        public string PreSalesStatusType { get; set; }
        public string Message { get; set; }
    }
}