using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Common.DataTable
{
    public class DataTableQuery
    {
        public int Draw { get; set; }
        public int Start { get; set; } = 0;
        public int Length { get; set; } = 25;

        public List<DataTableColumn> Columns { get; set; }
        public List<DataTableOrder> Order { get; set; }
        [JsonProperty("search")]
        public DataTableSearch Search { get; set; }
    }


    public class DataTableColumn
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTableSearch Search { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } // "asc" or "desc"
    }

    public class DataTableSearch
    {
        public string Value { get; set; }
        public bool Regex { get; set; }
    }
}