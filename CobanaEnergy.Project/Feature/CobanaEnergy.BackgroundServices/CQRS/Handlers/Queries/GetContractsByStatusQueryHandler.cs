using CobanaEnergy.Project.Models;
using CobanaEnergy.BackgroundServices.CQRS.Queries;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace CobanaEnergy.BackgroundServices.CQRS.Handlers.Queries
{
    /// <summary>
    /// Handler for retrieving contracts filtered by status
    /// Optimized for read-only operations with AsNoTracking()
    /// </summary>
    public class GetContractsByStatusQueryHandler 
        : IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>>
    {
        private readonly ApplicationDBContext _db;

        public GetContractsByStatusQueryHandler(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<List<ContractBasicInfo>> HandleAsync(GetContractsByStatusQuery query)
        {
            var dbQuery = _db.CE_ContractStatuses
                .AsNoTracking()  // Read-only optimization
                .Where(cs => cs.ContractStatus == query.ContractStatus);

            // Apply optional filters
            if (query.ContractTypes != null && query.ContractTypes.Any())
            {
                dbQuery = dbQuery.Where(cs => query.ContractTypes.Contains(cs.Type));
            }

            if (query.FromDate.HasValue)
            {
                dbQuery = dbQuery.Where(cs => cs.ModifyDate >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                dbQuery = dbQuery.Where(cs => cs.ModifyDate <= query.ToDate.Value);
            }

            // Project to DTO to minimize data transfer
            return await dbQuery
                .Select(cs => new ContractBasicInfo
                {
                    EId = cs.EId,
                    Type = cs.Type,
                    ContractStatus = cs.ContractStatus,
                    ModifyDate = cs.ModifyDate,
                    PostSalesCreationDate = cs.PostSalesCreationDate
                })
                .ToListAsync();
        }
    }
}


