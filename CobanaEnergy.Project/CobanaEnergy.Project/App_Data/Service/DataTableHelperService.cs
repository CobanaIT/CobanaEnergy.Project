using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Service
{
    /// <summary>
    /// Helper service for handling DataTable server-side processing parameters
    /// </summary>
    public static class DataTableHelperService
    {
        public class DataTableRequest
        {
            public int Start { get; set; }
            public int Length { get; set; }
            public string SearchValue { get; set; }
            public List<DataTableOrder> Order { get; set; }
            public int Draw { get; set; }
        }

        public class DataTableOrder
        {
            public int Column { get; set; }
            public string Dir { get; set; }
        }

        public class DataTableResponse<T>
        {
            public int draw { get; set; }
            public int recordsTotal { get; set; }
            public int recordsFiltered { get; set; }
            public List<T> data { get; set; }
        }

        /// <summary>
        /// Parse DataTable parameters from form collection
        /// </summary>
        public static DataTableRequest ParseDataTableRequest(FormCollection form)
        {
            return ParseDataTableRequest((NameValueCollection)form);
        }

        /// <summary>
        /// Parse DataTable parameters from NameValueCollection
        /// </summary>
        public static DataTableRequest ParseDataTableRequest(NameValueCollection form)
        {
            var request = new DataTableRequest
            {
                Start = Convert.ToInt32(form["start"] ?? "0"),
                Length = Convert.ToInt32(form["length"] ?? "10"),
                SearchValue = form["search[value]"] ?? "",
                Draw = Convert.ToInt32(form["draw"] ?? "1"),
                Order = ParseOrderParameters(form)
            };
            
            return request;
        }

        private static List<DataTableOrder> ParseOrderParameters(NameValueCollection form)
        {
            var orders = new List<DataTableOrder>();
            int orderIndex = 0;
            
            while (form[$"order[{orderIndex}][column]"] != null)
            {
                orders.Add(new DataTableOrder
                {
                    Column = Convert.ToInt32(form[$"order[{orderIndex}][column]"]),
                    Dir = form[$"order[{orderIndex}][dir]"] ?? "asc"
                });
                orderIndex++;
            }
            
            return orders;
        }
    }
}