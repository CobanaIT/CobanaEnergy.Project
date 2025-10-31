using CobanaEnergy.Project.Models.Common.DataTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace CobanaEnergy.Project.Service.ExtensionService
{
    public  static class DataTableExtensions
    {
        public static IQueryable<T> ApplyOrdering<T>(this IQueryable<T> query, DataTableQuery dtQuery)
        {
            if (dtQuery.Order != null && dtQuery.Order.Any())
            {
                foreach (var order in dtQuery.Order)
                {
                    var column = dtQuery.Columns[order.Column];
                    if (!string.IsNullOrEmpty(column.Data))
                    {
                        query = query.OrderByDynamic(column.Data, order.Dir == "asc");
                    }
                }
            }
            return query;
        }

        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, DataTableQuery dtQuery)
        {
            return query.Skip(dtQuery.Start).Take(dtQuery.Length);
        }

        // Helper for dynamic OrderBy
        public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> query, string propertyName, bool ascending)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.PropertyOrField(parameter, propertyName);
            var keySelector = Expression.Lambda(property, parameter);

            var methodName = ascending ? "OrderBy" : "OrderByDescending";
            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), property.Type);

            return (IQueryable<T>)method.Invoke(null, new object[] { query, keySelector });
        }
    }
}